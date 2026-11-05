using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        public void SettleVendorRepair(Player owner, IReadOnlyList<(IItem Item, float Durability)> quote,
            ulong cost, Func<bool> currentActor)
        {
            var lines = quote.ToArray();
            if (cost == 0 || lines.Length == 0 || !currentActor())
                return;

            // Settle earlier item and wallet intent before capturing this payment.
            owner.Save(Settle);

            void Settle()
            {
                lock (MutationGate)
                {
                    if (!TryAcquireMutationLease(out var lease))
                        return;
                    using var held = lease;
                    if (!currentActor() || owner.CurrencyManager is not CurrencyManager wallet
                        || lines.Any(line => line.Item is not Item || line.Item.CharacterId != characterId
                            || !ReferenceEquals(GetItem(line.Item.Guid), line.Item)
                            || !line.Item.Durability.Equals(line.Durability))
                        || lines.Select(line => line.Item.Guid).Distinct().Count() != lines.Length)
                        return;
                    if (!wallet.CanAfford(CurrencyType.Credits, cost))
                    {
                        owner.SendGenericError(GenericError.VendorNotEnoughCash);
                        return;
                    }

                    try
                    {
                        using var batch = BeginRealmBankNotifications(true, retryVendorNotices: true);
                        if (batch == null)
                            return;
                        batch.RequireActor(currentActor);
                        var payment = wallet.CaptureBankCurrency(owner,
                            [new(CurrencyType.Credits, cost, Subtract: true)], batch.CurrentActor, batch.After);
                        batch.Attach(payment);
                        foreach (var line in lines)
                        {
                            batch.Stage(line.Item);
                            line.Item.Durability = 1f;
                        }
                        batch.Add(() => payment.Publish(batch.Send));
                        batch.Complete();
                    }
                    catch (Exception error)
                    {
                        log.Error(error, "Vendor repair preparation failed.");
                    }
                }
            }
        }
    }
}
