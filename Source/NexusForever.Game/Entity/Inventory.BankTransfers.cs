using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        public bool SellRealmBankItem(ItemLocation location, IItem item, uint quantity,
            IReadOnlyList<(CurrencyType CurrencyTypeId, ulong CurrencyAmount)> payout, IBuybackManager buyback)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated || IsRealmBankPersistenceBlocked
                    || location?.Location != InventoryLocation.RealmBank || item?.Location != InventoryLocation.RealmBank
                    || item.CharacterId != characterId || !ReferenceEquals(GetItem(location), item)
                    || quantity == 0 || quantity > item.StackCount || buyback == null) return false;
                if (player.CurrencyManager is not CurrencyManager currencies) return false;
                using var batch = BeginRealmBankNotifications(true);
                if (batch == null) return false;
                try
                {
                    var captured = payout.ToArray();
                    var credit = currencies.CaptureBankCurrency(player, captured.Select(line => new CurrencyManager.BankCurrencyChange(line.CurrencyTypeId, line.CurrencyAmount)), batch.CurrentActor, batch.After);
                    batch.Attach(credit);
                    IItem sold = ItemDelete(location, quantity, ItemUpdateReason.Vendor);
                    if (sold == null) return false;
                    batch.Add(credit.Publish);
                    batch.Add(() => buyback.AddItem(player, sold, quantity, captured.ToList()));
                    batch.Complete(); return true;
                }
                catch (Exception error)
                {
                    log.Error(error, "Bank vendor sale preparation failed.");
                    player.Session.EnqueueMessageEncrypted(new ServerItemError { ErrorCode = GenericError.DbFailure });
                    return false;
                }
            }
        }
    }
}
