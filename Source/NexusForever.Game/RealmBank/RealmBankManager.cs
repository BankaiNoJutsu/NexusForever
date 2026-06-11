using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using ItemModel = NexusForever.Database.Character.Model.ItemModel;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.RealmBank
{
    public sealed class RealmBankManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        const uint BaseSlotCount = 16u;
        const uint SlotsPerEntitlementStack = 8u;

        readonly HashSet<(uint AccountId, ushort RealmId)> loaded = new();
        readonly IDatabaseManager databaseManager;
        readonly IItemManager itemManager;

        public RealmBankManager()
        {
        }

        public RealmBankManager(
            IDatabaseManager databaseManager,
            IItemManager itemManager = null)
        {
            this.databaseManager = databaseManager;
            this.itemManager     = itemManager;
        }

        public bool HasUnlock(IPlayer player)
        {
            return (player.Account.EntitlementManager.GetEntitlement(EntitlementType.SharedRealmBankUnlock)?.Amount ?? 0u) > 0u;
        }

        public uint GetSlotCapacity(IPlayer player)
        {
            if (!HasUnlock(player))
                return 0u;

            uint extraStacks = player.Account.EntitlementManager.GetEntitlement(EntitlementType.SharedRealmBankSlots)?.Amount ?? 0u;
            return BaseSlotCount + extraStacks * SlotsPerEntitlementStack;
        }

        public void OpenRealmBank(IPlayer player)
        {
            if (!HasUnlock(player))
                return;

            EnsureLoaded(player);
            SyncRealmBankToClient(player);
        }

        public void SyncRealmBankToClient(IPlayer player)
        {
            if (player?.Session == null || !HasUnlock(player))
                return;

            foreach (IBag bag in player.Inventory)
            {
                if (bag.Location != InventoryLocation.RealmBank)
                    continue;

                uint capacity = GetSlotCapacity(player);
                if (bag.Slots < capacity)
                    bag.Resize((int)(capacity - bag.Slots));

                foreach (IItem item in bag)
                {
                    player.Session.EnqueueMessageEncrypted(new ServerItemAdd
                    {
                        InventoryItem = new InventoryItem
                        {
                            Item   = item.Build(),
                            Reason = ItemUpdateReason.NoReason
                        }
                    });
                }

                break;
            }
        }

        public void EnsureLoaded(IPlayer player)
        {
            var key = (player.Account.Id, player.Identity.RealmId);
            CharacterDatabase database = GetDatabase();
            if (database == null)
                return;

            if (!loaded.Add(key))
                return;

            foreach (RealmBankItemModel model in database.GetRealmBankItems(player.Account.Id, player.Identity.RealmId))
            {
                IItemInfo info = itemManager.GetItemInfo(model.ItemId);
                if (info == null)
                    continue;

                var itemModel = new ItemModel
                {
                    Id                 = model.Id,
                    OwnerId            = player.CharacterId,
                    ItemId             = model.ItemId,
                    Location           = (ushort)InventoryLocation.Inventory,
                    BagIndex           = 0,
                    StackCount         = model.StackCount,
                    Charges            = model.Charges,
                    Durability         = model.Durability,
                    ExpirationTimeLeft = model.ExpirationTimeLeft,
                    Soulbound          = model.Soulbound
                };

                var item = new Entity.Item(itemModel);
                player.Inventory.LoadItem(item, InventoryLocation.RealmBank, model.BagIndex);
            }
        }

        public void SaveItem(IItem item, IPlayer player)
        {
            if (item.Location != InventoryLocation.RealmBank)
                return;

            CharacterDatabase database = GetDatabase();
            if (database == null)
                return;

            var model = new RealmBankItemModel
            {
                Id                 = item.Guid,
                AccountId          = player.Account.Id,
                RealmId            = player.Identity.RealmId,
                ItemId             = item.Id,
                BagIndex           = item.BagIndex,
                StackCount         = item.StackCount,
                Charges            = item.Charges,
                Durability         = item.Durability,
                ExpirationTimeLeft = item.ExpirationTimeLeft,
                Soulbound          = item.Soulbound
            };

            ulong itemGuid = item.Guid;
            database.Save(context =>
            {
                RealmBankItemModel existing = context.RealmBankItem.SingleOrDefault(i => i.Id == model.Id);
                if (existing == null)
                    context.RealmBankItem.Add(model);
                else
                {
                    existing.AccountId          = model.AccountId;
                    existing.RealmId            = model.RealmId;
                    existing.ItemId             = model.ItemId;
                    existing.BagIndex           = model.BagIndex;
                    existing.StackCount         = model.StackCount;
                    existing.Charges            = model.Charges;
                    existing.Durability         = model.Durability;
                    existing.ExpirationTimeLeft = model.ExpirationTimeLeft;
                    existing.Soulbound          = model.Soulbound;
                }

                ItemModel characterItem = context.Item.SingleOrDefault(i => i.Id == itemGuid);
                if (characterItem != null)
                    context.Item.Remove(characterItem);
            }).FireAndForgetAsync(ex => log.Error(ex, "Failed to save realm bank item {0}.", itemGuid));
        }

        public void SaveCharacterItem(IItem item, IPlayer player)
        {
            if (item.Location is InventoryLocation.RealmBank or InventoryLocation.None)
                return;

            CharacterDatabase database = GetDatabase();
            if (database == null)
                return;

            var model = new ItemModel
            {
                Id                 = item.Guid,
                OwnerId            = player.CharacterId,
                ItemId             = item.Id,
                Location           = (ushort)item.Location,
                BagIndex           = item.BagIndex,
                StackCount         = item.StackCount,
                Charges            = item.Charges,
                Durability         = item.Durability,
                ExpirationTimeLeft = item.ExpirationTimeLeft,
                Soulbound          = item.Soulbound
            };

            ulong itemGuid = item.Guid;
            database.Save(context =>
            {
                RealmBankItemModel realmBankItem = context.RealmBankItem.SingleOrDefault(i => i.Id == itemGuid);
                if (realmBankItem != null)
                    context.RealmBankItem.Remove(realmBankItem);

                ItemModel existing = context.Item.SingleOrDefault(i => i.Id == model.Id);
                if (existing == null)
                    context.Item.Add(model);
                else
                {
                    existing.OwnerId            = model.OwnerId;
                    existing.ItemId             = model.ItemId;
                    existing.Location           = model.Location;
                    existing.BagIndex           = model.BagIndex;
                    existing.StackCount         = model.StackCount;
                    existing.Charges            = model.Charges;
                    existing.Durability         = model.Durability;
                    existing.ExpirationTimeLeft = model.ExpirationTimeLeft;
                    existing.Soulbound          = model.Soulbound;
                }
            }).FireAndForgetAsync(ex => log.Error(ex, "Failed to save character item {0} after realm bank move.", itemGuid));
        }

        public void DeleteItem(ulong itemGuid)
        {
            CharacterDatabase database = GetDatabase();
            if (database == null)
                return;

            database.Save(context =>
            {
                RealmBankItemModel model = context.RealmBankItem.SingleOrDefault(i => i.Id == itemGuid);
                if (model != null)
                    context.RealmBankItem.Remove(model);
            }).FireAndForgetAsync(ex => log.Error(ex, "Failed to delete realm bank item {0}.", itemGuid));
        }

        CharacterDatabase GetDatabase()
        {
            return databaseManager?.GetDatabase<CharacterDatabase>();
        }
    }
}
