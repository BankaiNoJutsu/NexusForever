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
    public sealed partial class RealmBankManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        const uint BaseSlotCount = 16u;
        const uint SlotsPerEntitlementStack = 8u;

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
            if (player.Inventory?.IsSaveBlocked == true) { QueueSync(player, true); return; }

            EnsureLoaded(player);
            SyncRealmBankToClient(player);
        }

        public void SyncRealmBankToClient(IPlayer player)
        {
            if (player?.Session == null || !HasUnlock(player))
                return;
            if (player.Inventory?.IsSaveBlocked == true) { QueueSync(player, false); return; }

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
            if (player.Inventory?.IsSaveBlocked == true) { QueueSync(player, true, publish: false); return; }
            CharacterDatabase database = GetDatabase();
            if (database == null)
                return;
            LoadInventory(player, database);
        }

        public void SaveItem(IItem item, IPlayer player)
        {
            SaveItemAsync(item, player).FireAndForgetAsync(ex => log.Error(ex, "Failed to save realm bank item {0}.", item.Guid));
        }

        internal Task SaveItemAsync(IItem item, IPlayer player) =>
            item.Location == InventoryLocation.RealmBank ? SaveBankUnit([item], player) : Task.CompletedTask;

        public void SaveCharacterItem(IItem item, IPlayer player)
        {
            SaveCharacterItemAsync(item, player).FireAndForgetAsync(ex => log.Error(ex, "Failed to save character item {0} after realm bank move.", item.Guid));
        }

        internal Task SaveCharacterItemAsync(IItem item, IPlayer player) =>
            item.Location is InventoryLocation.RealmBank or InventoryLocation.None ? Task.CompletedTask : SaveBankUnit([item], player);

        public void SaveSwap(IItem first, IItem second, IPlayer player)
        {
            SaveSwapAsync(first, second, player).FireAndForgetAsync(ex => log.Error(ex,
                "Failed to save realm bank swap {0}/{1}.", first.Guid, second.Guid));
        }

        internal Task SaveSwapAsync(IItem first, IItem second, IPlayer player)
        {
            ArgumentNullException.ThrowIfNull(first); ArgumentNullException.ThrowIfNull(second);
            if (first.Guid == second.Guid || first.Location == InventoryLocation.None || second.Location == InventoryLocation.None
                || first.Location != InventoryLocation.RealmBank && second.Location != InventoryLocation.RealmBank)
                throw new InvalidOperationException("A bank swap requires two distinct surviving items.");
            return SaveBankUnit([first, second], player);
        }

        internal Task SaveConsumedMergeAsync(IItem source, IItem destination, IPlayer player)
        {
            ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(destination);
            if (source.Guid == destination.Guid || source.Location != InventoryLocation.None || !source.PendingDelete
                || destination.Location == InventoryLocation.None || destination.PendingDelete
                || source.PreviousLocation != InventoryLocation.RealmBank && destination.Location != InventoryLocation.RealmBank)
                throw new InvalidOperationException("A consumed bank merge requires its removed source and surviving destination.");
            return SaveBankUnit([source, destination], player);
        }

        internal Task SaveDeletedItemAsync(IItem item, IPlayer player)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Location != InventoryLocation.None || item.PreviousLocation != InventoryLocation.RealmBank || !item.PendingDelete)
                throw new InvalidOperationException("Bank deletion requires the removed Item and its retained deletion intent.");
            return SaveBankUnit([item], player);
        }

        internal Task SaveSplitAsync(IItem source, Entity.Item created, IPlayer player)
        {
            ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(created);
            if (source.Guid == created.Guid || source.Id != created.Id || source.StackCount == 0 || created.StackCount == 0
                || source.Location == InventoryLocation.None || created.Location == InventoryLocation.None
                || source.Location != InventoryLocation.RealmBank && created.Location != InventoryLocation.RealmBank)
                throw new InvalidOperationException("A bank split requires distinct surviving source and new destination Items.");
            return SaveBankUnit([source, created], player, created.ValidateSplitCreation);
        }

        // Periodic/logout saves share the caller's complete Character transaction.
        // Bag.Save cannot be used here: it writes the Character storage domain.
        internal void EnrollPeriodicItems(CharacterContext context, IItem[] items, IPlayer player)
        {
            try
            {
                uint accountId = player.Account.Id;
                ushort realmId = player.Identity.RealmId;
                ulong characterId = player.CharacterId;
                foreach (IItem item in items)
                {
                    if (item.Location != InventoryLocation.RealmBank)
                        throw new InvalidOperationException("Periodic bank save lost its bank location.");
                    if (item is Entity.Item concrete && !concrete.HasPendingSave && !concrete.NeedsRealmBankSave)
                        continue;
                    StageBankItem(context, item, item is Entity.Item ? null : CaptureAdapter(item, characterId),
                        accountId, realmId, characterId);
                }
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        private Task SaveBankUnit(IItem[] items, IPlayer player, Action<CharacterContext> beforeStage = null, Action<CharacterContext> companion = null, IReadOnlySet<ulong> detached = null, bool allowEmpty = false)
        {
            CharacterDatabase database = GetDatabase();
            if (database == null)
                return companion == null ? Task.CompletedTask : Task.FromException(new InvalidOperationException("A bank companion requires its Character database."));
            var command = new BankSaveCommand(database.CreateContext, items, player, beforeStage, companion, detached, allowEmpty);
            return bankExecution == null ? command.ExecuteAsync()
                : bankExecution(command) ?? throw new InvalidOperationException("Bank executor returned no operation.");
        }

        private static void StageConsumedItem(CharacterContext context, IItem item, ItemModel adapter,
            uint accountId, ushort realmId, ulong characterId, ulong itemGuid, uint itemId, Entity.Item.BankSaveSnapshot snapshot = null)
        {
            var bank = context.RealmBankItem.SingleOrDefault(row => row.Id == itemGuid);
            ValidateOwnership(bank, accountId, realmId, itemId);
            var character = context.Item.SingleOrDefault(row => row.Id == itemGuid);
            if (character != null && (character.OwnerId != characterId || character.ItemId != itemId))
                throw new InvalidOperationException("Consumed bank source has conflicting Character ownership or definition.");
            if (bank != null) context.RealmBankItem.Remove(bank);
            var captured = snapshot?.Stage(context, false, true) ?? (item is Entity.Item concrete ? concrete.SaveConsumedRealmBankItem(context) : adapter);
            if (captured.OwnerId != characterId || captured.Id != itemGuid || captured.ItemId != itemId
                || captured.Location != (ushort)InventoryLocation.None)
                throw new InvalidOperationException("Consumed bank source lost its captured Item owner.");
            if (item is not Entity.Item && character != null) context.Item.Remove(character);
            ProtectStorage(context, itemGuid, bank, false, null, false);
        }

        private static void StageBankItem(CharacterContext context, IItem item, ItemModel adapter,
            uint accountId, ushort realmId, ulong characterId, Entity.Item.BankSaveSnapshot snapshot = null)
        {
            // Reserve the real item's save ownership before staging the bank row.
            // Both storage domains settle only when this one context commits.
            var captured = snapshot?.Stage(context, true, false) ?? (item is Entity.Item concreteItem ? concreteItem.SaveToRealmBank(context) : adapter);
            if (captured.OwnerId != characterId) throw new InvalidOperationException("Bank deposit lost its captured Character owner.");
            var model = BankModel(captured, accountId, realmId);
            RealmBankItemModel existing = context.RealmBankItem.SingleOrDefault(i => i.Id == model.Id);
            ValidateOwnership(existing, model.AccountId, model.RealmId, model.ItemId);
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
                existing.MicrochipIds       = model.MicrochipIds;
                existing.RuneSlots          = model.RuneSlots;
            }

            if (item is not Entity.Item)
            {
                // Preserve the existing IItem adapter contract; runtime Items
                // use the coordinated actual-commit path above.
                ItemModel characterItem = context.Item.SingleOrDefault(i => i.Id == model.Id);
                if (characterItem != null)
                    context.Item.Remove(characterItem);
            }
            ProtectTransfer(context, existing ?? model, deposit: true, character: null);
        }

        private static void StageCharacterItem(CharacterContext context, IItem item, ItemModel model,
            uint accountId, ushort realmId, ulong characterId, ulong itemGuid, uint itemId, Entity.Item.BankSaveSnapshot snapshot = null, bool detached = false)
        {
            RealmBankItemModel realmBankItem = context.RealmBankItem.SingleOrDefault(i => i.Id == itemGuid);
            ValidateOwnership(realmBankItem, accountId, realmId, itemId);
            if (realmBankItem != null)
                context.RealmBankItem.Remove(realmBankItem);

            if (item is Entity.Item concreteItem)
            {
                var captured = snapshot == null ? concreteItem.SaveFromRealmBank(context) : snapshot.Stage(context, false, false);
                if (captured == null)
                {
                    if (realmBankItem != null) throw new InvalidOperationException("Withdrawal has no captured Character write.");
                    return;
                }
                if ((detached ? captured.OwnerId != null || captured.Location != (ushort)InventoryLocation.None : captured.OwnerId != characterId) || captured.ItemId != itemId || captured.Id != itemGuid)
                    throw new InvalidOperationException("Bank withdrawal lost its captured Character/item owner.");
                ProtectTransfer(context, realmBankItem, deposit: false, character: captured);
                return;
            }

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
                existing.MicrochipIds       = model.MicrochipIds;
                existing.RuneSlots          = model.RuneSlots;
            }
            ProtectTransfer(context, realmBankItem, deposit: false, character: model);
        }

        private static void ValidateOwnership(RealmBankItemModel row, uint accountId, ushort realmId, uint itemId)
        {
            if (row != null && (row.AccountId != accountId || row.RealmId != realmId || row.ItemId != itemId))
                throw new InvalidOperationException($"Realm bank item {row.Id} has conflicting ownership or definition.");
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
