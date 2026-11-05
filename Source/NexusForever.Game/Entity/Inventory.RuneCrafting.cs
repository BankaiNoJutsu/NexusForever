using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Network.Message;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        public void SettleRuneCrafting(Player owner, IItem item, Func<TradeskillResult> change,
            Func<TradeskillResult, IWritable[]> replies, Func<bool> currentActor)
        {
            if (item is not Item || !currentActor()) return;
            string originalRunes = ItemRuneSlotsCodec.Serialize(item.RuneSlots);
            var location = item.Location;
            uint slot = item.BagIndex;
            owner.Save(Settle);

            void Settle()
            {
                lock (MutationGate)
                {
                    if (!TryAcquireMutationLease(out var lease)) return;
                    using var held = lease;
                    if (!currentActor() || item.CharacterId != characterId || !ReferenceEquals(GetItem(item.Guid), item)
                        || item.Location != location || item.BagIndex != slot
                        || ItemRuneSlotsCodec.Serialize(item.RuneSlots) != originalRunes) return;
                    try
                    {
                        TradeskillResult result;
                        using (var batch = BeginRealmBankNotifications(true, retryVendorNotices: true))
                        {
                            if (batch == null) return;
                            batch.RequireActor(currentActor);
                            batch.OnFailure(replies(TradeskillResult.UnknownError));
                            batch.Stage(item);
                            result = change();
                            if (result == TradeskillResult.Success)
                            {
                                foreach (var packet in replies(result)) batch.Add(packet);
                                batch.Complete();
                            }
                        }
                        // Validation failure has no accepted mutation. The batch
                        // restored preparation before publishing its failure.
                        if (result != TradeskillResult.Success)
                            foreach (var packet in replies(result))
                            {
                                if (!currentActor()) break;
                                owner.Session.EnqueueMessageEncrypted(packet);
                            }
                    }
                    catch (Exception error)
                    {
                        log.Error(error, "Rune crafting preparation failed.");
                    }
                }
            }
        }
    }
}
