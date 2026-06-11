using System.Collections.Immutable;
using System.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game
{
    public sealed class ItemManager : IItemManager
    {
        private const string ItemTableName = "Item2.tbl";
        private const string ItemSlotTableName = "ItemSlot.tbl";

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Id to be assigned to the next created item.
        /// </summary>
        public ulong NextItemId => nextItemId++;

        private ulong nextItemId;

        private ImmutableDictionary<uint, IItemInfo> item;
        private ImmutableDictionary<ItemSlot, ImmutableList<EquippedItem>> equippedItemSlots;

        private readonly IDatabaseManager databaseManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IAssetManager assetManager;

        public ItemManager(
            IDatabaseManager databaseManager = null,
            IGameTableManager gameTableManager = null,
            IAssetManager assetManager = null)
        {
            this.databaseManager  = databaseManager;
            this.gameTableManager = gameTableManager;
            this.assetManager     = assetManager;
        }

        public void Initialise()
        {
            var sw = Stopwatch.StartNew();
            log.Info("Initialise item info...");

            nextItemId = databaseManager.GetDatabase<CharacterDatabase>().GetNextItemId() + 1ul;

            InitialiseItemInfo();
            InitialiseEquippedItemSlots();

            log.Info($"Cached {item.Count} items in {sw.ElapsedMilliseconds}ms.");
        }

        private void InitialiseItemInfo()
        {
            var builder = ImmutableDictionary.CreateBuilder<uint, IItemInfo>();
            if (gameTableManager.Item?.Entries == null)
                MissingGameDataDiagnostics.ReportMissingTable(
                    ItemTableName,
                    nameof(ItemManager) + "." + nameof(InitialiseItemInfo),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot cache item definitions.");

            IEnumerable<Item2Entry> itemEntries =
                gameTableManager.Item?.Entries ?? Enumerable.Empty<Item2Entry>();
            foreach (Item2Entry entry in itemEntries)
            {
                var info = new ItemInfo(entry, gameTableManager, assetManager);
                builder.Add(info.Id, info);
            }

            item = builder.ToImmutable();
        }

        private void InitialiseEquippedItemSlots()
        {
            var builder = new Dictionary<ItemSlot, List<EquippedItem>>();
            if (gameTableManager.ItemSlot?.Entries == null)
                MissingGameDataDiagnostics.ReportMissingTable(
                    ItemSlotTableName,
                    nameof(ItemManager) + "." + nameof(InitialiseEquippedItemSlots),
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot cache equipped item slot mappings.");

            IEnumerable<ItemSlotEntry> itemSlotEntries =
                gameTableManager.ItemSlot?.Entries ?? Enumerable.Empty<ItemSlotEntry>();
            foreach (ItemSlotEntry entry in itemSlotEntries)
            {
                for (EquippedItem slot = EquippedItem.Chest; slot < EquippedItem.BankBag9; slot++)
                {
                    uint flags = 1u << (int)slot;
                    if ((entry.EquippedSlotFlags & flags) == 0)
                        continue;

                    if (!builder.TryGetValue((ItemSlot)entry.Id, out List<EquippedItem> equippedItems))
                    {
                        equippedItems = new List<EquippedItem>();
                        builder.Add((ItemSlot)entry.Id, equippedItems);
                    }

                    equippedItems.Add(slot);
                }
            }

            equippedItemSlots = builder.ToImmutableDictionary(e => e.Key, e => e.Value.ToImmutableList());
        }

        /// <summary>
        /// Return <see cref="IItemInfo"/> with supplied id.
        /// </summary>
        public IItemInfo GetItemInfo(uint id)
        {
            return item.TryGetValue(id, out IItemInfo info) ? info : null;
        }

        /// <summary>
        /// Returns a collection of bag indexes for <see cref="InventoryLocation.Equipped"/> for supplied <see cref="ItemSlot"/>.
        /// </summary>
        public IEnumerable<EquippedItem> GetEquippedBagIndexes(ItemSlot slot)
        {
            return equippedItemSlots.TryGetValue(slot, out ImmutableList<EquippedItem> entries) ? entries : Enumerable.Empty<EquippedItem>();
        }
    }
}
