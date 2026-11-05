using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private void SettleSatchelDeposit(Player owner, IItem item, uint amount)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (amount == 0 || amount > item.StackCount)
                throw new InvalidPacketValueException();

            if (item is not Item || owner.SupplySatchelManager is not SupplySatchelManager satchel || realmBankManager == null
                || owner.CurrencyManager is not CurrencyManager wallet)
                return;

            var session = owner.Session;
            var account = owner.Account;
            var events = session?.Events;
            ulong? epoch = account?.WorldSessionEpoch;
            var location = item.Location;
            uint slot = item.BagIndex;
            uint stack = item.StackCount;
            ulong guid = item.Guid;
            bool shared = location == InventoryLocation.RealmBank;

            if (!OwnsSource() || owner.IsNormalSaveInProgress || IsSaveBlocked || satchel.IsFull(item))
                return;

            // Settle earlier item and material intent before reserving the deposit.
            owner.Save(Settle);

            bool OwnsActor() => ReferenceEquals(owner.Session, session) && ReferenceEquals(session?.Events, events)
                && session is IPlayerSession bound && ReferenceEquals(bound.Player, owner)
                && ReferenceEquals(owner.Account, account) && ReferenceEquals(account?.Session, session) && account?.WorldSessionEpoch == epoch
                && ReferenceEquals(owner.Inventory, this) && ReferenceEquals(owner.SupplySatchelManager, satchel)
                && ReferenceEquals(owner.CurrencyManager, wallet) && owner.CharacterId == characterId
                && session is not NetworkSession { IsDisconnecting: true }
                && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            bool OwnsSource() => OwnsActor() && (!shared || realmBankManager.HasUnlock(owner))
                && ReferenceEquals(GetItem(guid), item) && item.Location == location && item.BagIndex == slot
                && item.StackCount == stack && !item.PendingDelete;

            void Settle()
            {
                lock (MutationGate)
                {
                    if (!OwnsSource() || !TryAcquireMutationLease(out var lease)) return;
                    using var held = lease;
                    try
                    {
                        string database = wallet.GetAuctionBidDatabase(owner);
                        using var batch = BeginRealmBankNotifications(true, retryVendorNotices: true);
                        if (batch == null) return;
                        batch.RequireActor(OwnsActor);
                        var material = satchel.ReserveManualDeposit(item, amount, database);
                        if (material == null) return;
                        var settlement = new BankMaterialSettlement(characterId, database, "satchel-deposit", material, allowItemless: false);
                        batch.Attach(settlement);
                        batch.Add(() => settlement.Publish(batch.CurrentActor, batch.Send));
                        if (settlement.MaterialCount == stack)
                        {
                            if (CanRemoveItem(item) is { } error)
                            {
                                session.EnqueueMessageEncrypted(new ServerItemError { ItemGuid = guid, ErrorCode = error });
                                return;
                            }
                            if (ItemDelete(GetBag(location), item, ItemUpdateReason.MaterialBagConversion) == null) return;
                        }
                        else
                            ItemStackCountUpdate(item, stack - settlement.MaterialCount);
                        batch.Complete();
                    }
                    catch (Exception error)
                    {
                        log.Error(error, "Satchel deposit preparation failed.");
                    }
                }
            }

        }
    }
}
