using System.Diagnostics;
using System.Threading;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Retail;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Loot;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NLog;
using NetworkItemLocation = NexusForever.Network.World.Message.Model.Shared.ItemLocation;
using ServerLootRemovePacket = NexusForever.Network.World.Message.Model.Loot.ServerLootRemove;

namespace NexusForever.Game.Loot
{
    public partial class GlobalLootManager : Singleton<GlobalLootManager>, IGlobalLootManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const float LOOT_RANGE = 35f;
        private const double OMNIBIT_KILL_DROP_CHANCE = 0.35d;
        private const int OMNIBIT_KILL_MIN_AMOUNT = 7;
        private const int OMNIBIT_KILL_MAX_BASE_AMOUNT = 25;
        private const uint CLIENT_LOOT_UNIT_ID_OVERLAY = 0x40000000u;
        private const string ITEM_SALVAGE_LOOT_GROUP_COMMENT_PREFIX = "DataMapping item_salvage";
        private const string AccountCurrencyTypeTableName = "AccountCurrencyType.tbl";
        private const string AccountItemTableName = "AccountItem.tbl";
        private const string VirtualItemTableName = "VirtualItem.tbl";

        private sealed class LootRecipientContext
        {
            public Dictionary<ulong, uint> LooterIds { get; init; } = [];
            public LooterType LooterType { get; init; }
            public GroupLootState Group { get; init; }
            public IReadOnlyList<IPlayer> EligiblePlayers { get; init; } = [];
            public IReadOnlyList<GroupLootMember> EligibleMembers { get; init; } = [];
            public IReadOnlyList<IPlayer> MasterLootCandidates { get; init; } = [];
        }

        /// <summary>
        /// Monotonically increasing loot-unit id.
        /// </summary>
        /// <remarks>
        /// Allocation is atomic so future off-tick loot producers cannot duplicate
        /// transient loot-unit ids. The id range intentionally starts in the high-bit
        /// server-owned space and never returns zero.
        /// </remarks>
        public uint NextLootId
        {
            get
            {
                uint next = unchecked((uint)Interlocked.Increment(ref nextLootId));
                return next == 0u ? unchecked((uint)Interlocked.Increment(ref nextLootId)) : next;
            }
        }

        private int nextLootId = int.MaxValue;

        private readonly Dictionary<uint, List<LootGroup>> creatureLoot = [];
        private readonly Dictionary<uint, List<LootGroup>> itemLoot = [];
        private readonly Dictionary<uint, List<ItemSalvageModel>> itemSalvageByItem = [];
        private readonly Dictionary<(uint Item2TypeId, uint Level), List<ItemSalvageModel>> itemSalvageByTypeLevel = [];
        private readonly Dictionary<uint, List<LootItem>> directCreatureLoot = [];

        /// <summary>
        /// Active loot instances in the world.
        /// </summary>
        /// <remarks>
        /// Mutated only on the world thread (via <see cref="Update"/>, <see cref="DropLoot"/>,
        /// <see cref="GiveLoot"/>, and other player-driven actions that are dispatched
        /// synchronously on the world tick). No lock is required because the world server
        /// processes a single tick at a time on one thread.
        /// </remarks>
        private readonly List<LootInstance> lootInstances = [];
        private readonly Dictionary<uint, List<LootInstance>> lootInstancesByOwnerUnit = [];
        private readonly Dictionary<ulong, List<LootInstance>> lootInstancesByLooter = [];

        private readonly UpdateTimer updateTimer = new(1d);

        private readonly IGroupStateManager groupStateManager;

        public GlobalLootManager(
            IGroupStateManager groupStateManager)
        {
            this.groupStateManager = groupStateManager;
        }

        public void Initialise()
        {
            Stopwatch sw = Stopwatch.StartNew();

            creatureLoot.Clear();
            itemLoot.Clear();
            itemSalvageByItem.Clear();
            itemSalvageByTypeLevel.Clear();
            directCreatureLoot.Clear();
            lootInstances.Clear();
            lootInstancesByOwnerUnit.Clear();
            lootInstancesByLooter.Clear();

            WorldDatabase worldDatabase = DatabaseManager.Instance.GetDatabase<WorldDatabase>();

            foreach (ItemLootModel itemLootModel in worldDatabase.GetAllItemLootTables())
                BuildLoot(itemLootModel.Id, LootEntityType.Item, itemLootModel.LootGroup);

            foreach (EntityLootModel entityLootModel in worldDatabase.GetAllEntityLootTables())
                BuildLoot(entityLootModel.Id, LootEntityType.Creature, entityLootModel.LootGroup);

            int skippedItemSalvageRows = 0;
            int exactItemSalvageRows = 0;
            int typeLevelItemSalvageRows = 0;
            foreach (ItemSalvageModel itemSalvageModel in worldDatabase.GetItemSalvage())
                BuildItemSalvage(itemSalvageModel, ref skippedItemSalvageRows, ref exactItemSalvageRows, ref typeLevelItemSalvageRows);

            int skippedDirectRows = 0;
            int skippedDirectRowsWithMappedFlatLootTables = 0;
            foreach (CreatureLootModel creatureLootModel in worldDatabase.GetCreatureLoot())
            {
                if (creatureLoot.TryGetValue(creatureLootModel.CreatureId, out List<LootGroup> creatureGroups) &&
                    creatureGroups.Any(IsMappedFlatLootGroup))
                {
                    skippedDirectRowsWithMappedFlatLootTables++;
                    continue;
                }

                if (ItemManager.Instance.GetItemInfo(creatureLootModel.ItemId) == null)
                {
                    skippedDirectRows++;
                    continue;
                }

                if (!directCreatureLoot.TryGetValue(creatureLootModel.CreatureId, out List<LootItem> items))
                {
                    items = [];
                    directCreatureLoot.Add(creatureLootModel.CreatureId, items);
                }

                float probability = (float)Math.Clamp(creatureLootModel.Chance, 0m, 1m) * 100f;
                items.Add(new LootItem(creatureLootModel.ItemId, LootItemType.StaticItem, probability, 1u, 1u));
            }

            log.Info($"Loaded loot for {creatureLoot.Count} old-table creature(s), {itemLoot.Count} item(s), and {directCreatureLoot.Count} imported creature(s) in {sw.ElapsedMilliseconds}ms.");
            if (skippedDirectRows > 0)
                log.Warn($"Skipped {skippedDirectRows} imported creature loot row(s) because their Item2 id is not present in the current game tables.");
            if (skippedDirectRowsWithMappedFlatLootTables > 0)
                log.Info($"Skipped {skippedDirectRowsWithMappedFlatLootTables} imported creature loot row(s) because mapped flat creature loot_group rows are already present for those creatures.");
            log.Info($"Loaded item salvage for {itemSalvageByItem.Count} exact item(s) ({exactItemSalvageRows} row(s)) and {itemSalvageByTypeLevel.Count} client type/level pair(s) ({typeLevelItemSalvageRows} row(s)).");
            if (skippedItemSalvageRows > 0)
                log.Warn($"Skipped {skippedItemSalvageRows} item_salvage row(s) because their source key or reward is invalid.");
        }

        private void BuildLoot(uint entityId, LootEntityType type, LootGroupModel lootGroupModel)
        {
            if (lootGroupModel == null)
                return;

            switch (type)
            {
                case LootEntityType.Creature:
                    if (!creatureLoot.TryGetValue(entityId, out List<LootGroup> creatureGroups))
                    {
                        creatureGroups = [];
                        creatureLoot.Add(entityId, creatureGroups);
                    }

                    creatureGroups.Add(new LootGroup(lootGroupModel, !IsMappedFlatLootGroup(lootGroupModel)));
                    break;
                case LootEntityType.Item:
                    if (!itemLoot.TryGetValue(entityId, out List<LootGroup> itemGroups))
                    {
                        itemGroups = [];
                        itemLoot.Add(entityId, itemGroups);
                    }

                    itemGroups.Add(new LootGroup(lootGroupModel));
                    break;
            }
        }

        internal static bool IsMappedFlatLootGroup(LootGroup lootGroup)
        {
            return IsMappedFlatLootGroupComment(lootGroup.Comment);
        }

        private static bool IsMappedFlatLootGroup(LootGroupModel lootGroupModel)
        {
            return IsMappedFlatLootGroupComment(lootGroupModel.Comment);
        }

        private static bool IsMappedFlatLootGroupComment(string comment)
        {
            return comment?.StartsWith("DataMapping creature_loot", StringComparison.OrdinalIgnoreCase) == true;
        }

        public void Update(double lastTick)
        {
            updateTimer.Update(lastTick);

            foreach (LootInstance lootInstance in lootInstances)
                lootInstance.Update(lastTick);

            if (!updateTimer.HasElapsed)
                return;

            RemoveExpiredLootInstances();
            updateTimer.Reset();
        }

        public bool DropLoot(IPlayer looter, IWorldEntity lootedEntity)
        {
            if (looter == null || lootedEntity == null)
            {
                log.Trace($"Creature loot drop skipped: looter or looted entity is null. looterCharacter={looter?.CharacterId.ToString() ?? "none"}, ownerUnit={lootedEntity?.Guid.ToString() ?? "none"}.");
                return false;
            }

            if (lootedEntity is IPetEntity)
            {
                log.Trace($"Creature loot drop skipped for owner {lootedEntity.Guid}: owner is a pet.");
                return false;
            }

            if (lootedEntity.CreatureId == 0u)
            {
                log.Trace($"Creature loot drop skipped for owner {lootedEntity.Guid}: creature id is 0.");
                return false;
            }

            Creature2Entry entry = GameTableManager.Instance.Creature2?.GetEntry(lootedEntity.CreatureId);
            if (entry == null)
            {
                log.Warn($"Creature2 entry {lootedEntity.CreatureId} not found while generating loot for owner {lootedEntity.Guid}.");
                return false;
            }

            LootRecipientContext recipients = CreateLootRecipientContext(looter, lootedEntity);
            if (recipients.EligiblePlayers.Count == 0)
            {
                log.Trace($"Creature loot drop skipped because no eligible recipients were in range: looterCharacter={looter.CharacterId}, ownerUnit={lootedEntity.Guid}, creatureId={lootedEntity.CreatureId}, ownerPosition=({lootedEntity.Position.X:R},{lootedEntity.Position.Y:R},{lootedEntity.Position.Z:R}), looterPosition=({looter.Position.X:R},{looter.Position.Y:R},{looter.Position.Z:R}).");
                return false;
            }

            log.Trace(
                $"Generating creature loot: looterCharacter={looter.CharacterId}, looterGuid={looter.Guid}, ownerUnit={lootedEntity.Guid}, creatureId={lootedEntity.CreatureId}, creature2Id={entry.Id}, looterType={recipients.LooterType}, eligiblePlayers=[{string.Join(",", recipients.EligiblePlayers.Select(p => $"{p.CharacterId}:{p.Guid}"))}], ownerPosition=({lootedEntity.Position.X:R},{lootedEntity.Position.Y:R},{lootedEntity.Position.Z:R}), looterPosition=({looter.Position.X:R},{looter.Position.Y:R},{looter.Position.Z:R}).");
            LootInstance lootInstance = GenerateLootInstance(entry.Id, lootedEntity.Guid, looter, recipients.LooterIds, recipients.LooterType, LootEntityType.Creature);
            ConfigureLootDistribution(lootInstance, recipients);
            if (lootInstance.HasExpired)
            {
                log.Trace($"Creature loot expired immediately after generation: ownerUnit={lootedEntity.Guid}, creature2Id={entry.Id}, looterCharacter={looter.CharacterId}, generatedItems=[{FormatLootInstanceItems(lootInstance)}].");
                return false;
            }

            AddLootInstance(lootInstance);
            log.Trace($"Creature loot instance created: ownerUnit={lootedEntity.Guid}, creature2Id={entry.Id}, looterCharacter={looter.CharacterId}, looterType={recipients.LooterType}, items=[{FormatLootInstanceItems(lootInstance)}].");
            foreach (IPlayer player in recipients.EligiblePlayers)
            {
                log.Trace($"Sending creature loot notify: recipientCharacter={player.CharacterId}, recipientGuid={player.Guid}, ownerUnit={lootedEntity.Guid}, items=[{FormatLootInstanceItems(lootInstance)}].");
                lootInstance.SendLootNotify(player);
            }

            return true;
        }

        public bool HasLoot(IItem lootedItem)
        {
            return HasItemLoot(lootedItem, IsNonSalvageItemLootGroup);
        }

        public bool DropLoot(IPlayer looter, IItem lootedItem)
        {
            if (looter == null || lootedItem?.Info == null)
            {
                log.Trace($"Item loot drop skipped: looter or item info is null. looterCharacter={looter?.CharacterId.ToString() ?? "none"}, itemGuid={lootedItem?.Guid.ToString() ?? "none"}.");
                return false;
            }

            if (!TryGenerateItemLoot(lootedItem, looter, IsNonSalvageItemLootGroup, "missing-item-loot", "empty-item-loot", out IReadOnlyList<GeneratedLootItem> items, out string reason))
            {
                log.Trace($"Item loot drop skipped for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: generation failed with reason {reason}.");
                return false;
            }

            if (!CanDeliverGeneratedLoot(looter, items, out reason))
            {
                log.Trace($"Item loot drop skipped for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: delivery validation failed with reason {reason}; generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            bool delivered = TryDeliverGeneratedItemLoot(looter, items, looter.Guid);
            log.Trace($"Item loot drop delivery for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: delivered={delivered}, generatedItems=[{FormatGeneratedLootItems(items)}].");
            return delivered;
        }

        public bool TryUseLootBag(IPlayer looter, IItem lootedItem, out string reason)
        {
            reason = string.Empty;
            if (looter == null)
            {
                reason = "no-looter";
                return false;
            }

            if (lootedItem?.Info == null)
            {
                reason = "missing-item";
                return false;
            }

            if (!TryGenerateItemLoot(lootedItem, looter, IsNonSalvageItemLootGroup, "missing-item-loot", "empty-item-loot", out IReadOnlyList<GeneratedLootItem> items, out reason))
            {
                log.Trace($"Loot bag use failed during generation for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}.");
                return false;
            }

            if (!CanDeliverGeneratedLoot(looter, items, out reason))
            {
                log.Trace($"Loot bag use failed during delivery validation for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            if (!CanDeliverGeneratedItemLoot(looter, items, out reason))
            {
                log.Trace($"Loot bag use failed during delivery preflight for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            if (!looter.Inventory.ItemUse(lootedItem))
            {
                reason = "item-use-failed";
                log.Trace($"Loot bag use failed while consuming item for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            if (lootedItem.Info.Entry.MaxCharges == 0u && lootedItem.Info.Entry.MaxStackCount == 1u)
            {
                IItem deletedItem;
                try
                {
                    deletedItem = looter.Inventory.ItemDelete(new NetworkItemLocation
                    {
                        Location = lootedItem.Location,
                        BagIndex = lootedItem.BagIndex
                    }, ItemUpdateReason.ConsumeCharge);
                }
                catch (InvalidPacketValueException)
                {
                    reason = "item-delete-failed";
                    log.Trace($"Loot bag use failed while deleting single-stack item for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                    return false;
                }
                catch (ArgumentException)
                {
                    reason = "item-delete-failed";
                    log.Trace($"Loot bag use failed while deleting single-stack item for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                    return false;
                }

                if (deletedItem == null)
                {
                    reason = "item-delete-failed";
                    log.Trace($"Loot bag use failed while deleting single-stack item for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: reason={reason}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                    return false;
                }
            }

            if (!TryDeliverGeneratedItemLoot(looter, items, looter.Guid))
            {
                reason = "loot-delivery-failed";
                log.Warn($"Loot bag use failed during final delivery after item consumption for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            log.Trace($"Loot bag use succeeded for player {looter.CharacterId}, item {lootedItem.Info.Entry.Id}: generatedItems=[{FormatGeneratedLootItems(items)}].");
            return true;
        }

        /// <summary>
        /// Delivers resource/harvest loot using the group's <see cref="HarvestLootRule"/> when applicable.
        /// Corpse loot uses <see cref="ConfigureLootDistribution"/> instead.
        /// </summary>
        public bool TryDeliverHarvestLoot(IPlayer harvester, IReadOnlyList<GeneratedLootItem> items, uint ownerUnitId)
        {
            if (harvester == null)
                return false;

            ArgumentNullException.ThrowIfNull(items);
            if (items.Count == 0)
                return false;

            IPlayer recipient = harvester;
            if (harvester.GroupAssociation != 0ul
                && groupStateManager.TryGetGroup(harvester.GroupAssociation, out GroupLootState group)
                && group.HasMember(harvester.Identity))
            {
                List<(IPlayer Player, GroupLootMember Member)> eligible = [];
                foreach (GroupLootMember member in group.Members)
                {
                    IPlayer player = PlayerManager.Instance.GetPlayer(member.Identity);
                    if (player == null || player.Map == null || player.Map != harvester.Map)
                        continue;

                    float distance = player.Position.GetDistance(harvester.Position);
                    if (distance > LOOT_RANGE)
                    {
                        log.Trace($"Harvest loot recipient skipped because they are out of range: harvesterCharacter={harvester.CharacterId}, recipientCharacter={player.CharacterId}, distance={distance:R}, lootRange={LOOT_RANGE:R}, ownerUnit={ownerUnitId}.");
                        continue;
                    }

                    if (!CanReceiveHarvestLoot(player, items, out string reason))
                    {
                        log.Trace($"Harvest loot recipient skipped because delivery preflight failed: harvesterCharacter={harvester.CharacterId}, recipientCharacter={player.CharacterId}, reason={reason}, ownerUnit={ownerUnitId}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                        continue;
                    }

                    eligible.Add((player, member));
                }

                if (eligible.Count == 1)
                {
                    recipient = eligible[0].Player;
                }
                else if (eligible.Count > 1)
                {
                    GroupLootMember winner = groupStateManager.ResolveHarvestLootRecipient(
                        group,
                        harvester,
                        eligible.Select(e => e.Member).ToList());
                    recipient = eligible.First(e => e.Member.Identity.Id == winner.Identity.Id).Player
                        ?? harvester;
                }
            }

            if (!CanReceiveHarvestLoot(recipient, items, out string deliveryReason))
            {
                log.Trace($"Harvest loot delivery skipped because recipient preflight failed: harvesterCharacter={harvester.CharacterId}, recipientCharacter={recipient.CharacterId}, reason={deliveryReason}, ownerUnit={ownerUnitId}, generatedItems=[{FormatGeneratedLootItems(items)}].");
                return false;
            }

            return TryDeliverGeneratedItemLoot(recipient, items, ownerUnitId);
        }

        private bool CanReceiveHarvestLoot(IPlayer recipient, IReadOnlyList<GeneratedLootItem> items, out string reason)
        {
            if (!CanDeliverGeneratedLoot(recipient, items, out reason))
                return false;

            return CanDeliverGeneratedItemLoot(recipient, items, out reason);
        }

        private static Dictionary<ulong, uint> CreatePlayerLooterMap(IPlayer player)
        {
            return new Dictionary<ulong, uint>
            {
                { player.CharacterId, player.Guid }
            };
        }

        private LootRecipientContext CreateLootRecipientContext(IPlayer looter, IWorldEntity lootedEntity)
        {
            if (looter.GroupAssociation == 0ul
                || !groupStateManager.TryGetGroup(looter.GroupAssociation, out GroupLootState group)
                || !group.HasMember(looter.Identity))
            {
                return CreateSoloRecipientContext(looter);
            }

            List<(IPlayer Player, GroupLootMember Member)> sameMapMembers = [];
            List<(IPlayer Player, GroupLootMember Member)> eligible = [];
            foreach (GroupLootMember member in group.Members)
            {
                IPlayer player = PlayerManager.Instance.GetPlayer(member.Identity);
                if (player == null || player.Map == null || player.Map != lootedEntity.Map)
                    continue;

                sameMapMembers.Add((player, member));

                float distance = player.Position.GetDistance(lootedEntity.Position);
                if (distance > LOOT_RANGE)
                {
                    log.Trace($"Corpse loot recipient skipped because they are out of range: looterCharacter={looter.CharacterId}, recipientCharacter={player.CharacterId}, distance={distance:R}, lootRange={LOOT_RANGE:R}, ownerUnit={lootedEntity.Guid}.");
                    continue;
                }

                eligible.Add((player, member));
            }

            if (eligible.Count <= 1 && sameMapMembers.Count <= 1)
                return CreateSoloRecipientContext(looter);

            IReadOnlyList<IPlayer> masterLootCandidates = eligible
                .Select(e => e.Player)
                .Distinct()
                .ToList();

            if (sameMapMembers.Count > 1)
            {
                return new LootRecipientContext
                {
                    LooterType           = LooterType.Group,
                    Group                = group,
                    LooterIds            = eligible.ToDictionary(e => e.Player.CharacterId, e => e.Player.Guid),
                    EligiblePlayers      = eligible.Select(e => e.Player).ToList(),
                    EligibleMembers      = eligible.Select(e => e.Member).ToList(),
                    MasterLootCandidates = masterLootCandidates
                };
            }

            return new LootRecipientContext
            {
                LooterType           = LooterType.Group,
                Group                = group,
                LooterIds            = eligible.ToDictionary(e => e.Player.CharacterId, e => e.Player.Guid),
                EligiblePlayers      = eligible.Select(e => e.Player).ToList(),
                EligibleMembers      = eligible.Select(e => e.Member).ToList(),
                MasterLootCandidates = masterLootCandidates
            };
        }

        private static LootRecipientContext CreateSoloRecipientContext(IPlayer player)
        {
            return new LootRecipientContext
            {
                LooterType      = LooterType.Player,
                LooterIds       = CreatePlayerLooterMap(player),
                EligiblePlayers = [player]
            };
        }

        private LootInstance GenerateLootInstance(uint entityId, uint ownerUnitId, IPlayer player, Dictionary<ulong, uint> looterIds, LooterType looterType, LootEntityType lootEntityType, uint parentUnitId = 0u)
        {
            uint resolvedParentUnitId = parentUnitId != 0u ? parentUnitId : ownerUnitId;
            LootInstance lootInstance = new(ownerUnitId, resolvedParentUnitId, looterIds, looterType, lootEntityType);
            log.Trace($"Generating loot instance: entityId={entityId}, ownerUnit={ownerUnitId}, parentUnit={resolvedParentUnitId}, playerCharacter={player?.CharacterId.ToString() ?? "none"}, looterType={looterType}, lootEntityType={lootEntityType}, looterIds=[{string.Join(",", looterIds.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}].");

            switch (lootEntityType)
            {
                case LootEntityType.Creature:
                    if (creatureLoot.TryGetValue(entityId, out List<LootGroup> creatureLootGroups))
                    {
                        log.Trace($"Creature loot tables found for entityId={entityId}: groupCount={creatureLootGroups.Count}.");
                        foreach (LootGroup lootGroup in creatureLootGroups)
                        {
                            foreach ((LootItem item, uint count) in lootGroup.GenerateLootDrops(player))
                                TryAddLootItem(lootInstance, item, count, $"creature-table:{entityId}:group:{lootGroup.Id}");
                        }
                    }
                    else
                    {
                        log.Trace($"No creature loot table groups found for entityId={entityId}.");
                    }

                    if (directCreatureLoot.TryGetValue(entityId, out List<LootItem> importedItems))
                    {
                        log.Trace($"Imported direct creature loot rows found for entityId={entityId}: itemCount={importedItems.Count}.");
                        foreach (LootItem item in importedItems)
                        {
                            if (item.TryGetDrop(out uint count))
                                TryAddLootItem(lootInstance, item, count, $"creature-direct:{entityId}");
                            else
                                log.Trace($"Imported direct creature loot did not roll: entityId={entityId}, type={item.Type}, staticId={item.StaticId}.");
                        }
                    }
                    else
                    {
                        log.Trace($"No imported direct creature loot rows found for entityId={entityId}.");
                    }

                    TryGrantRandomOmnibitKillReward(player, ownerUnitId);
                    break;
                case LootEntityType.Item:
                    if (itemLoot.TryGetValue(entityId, out List<LootGroup> itemLootGroups))
                    {
                        log.Trace($"Item loot tables found for entityId={entityId}: groupCount={itemLootGroups.Count}.");
                        foreach (LootGroup lootGroup in itemLootGroups)
                        {
                            foreach ((LootItem item, uint count) in lootGroup.GenerateLootDrops(player))
                                TryAddLootItem(lootInstance, item, count, $"item-table:{entityId}:group:{lootGroup.Id}");
                        }
                    }
                    else
                    {
                        log.Trace($"No item loot table groups found for entityId={entityId}.");
                    }
                    break;
            }

            return lootInstance;
        }

        /// <summary>
        /// Applies corpse loot rules (Need/Greed/Master/RR/FFA). Resource harvest uses
        /// <see cref="TryDeliverHarvestLoot"/> with <see cref="GroupLootState.HarvestRule"/>.
        /// </summary>
        private void ConfigureLootDistribution(LootInstance lootInstance, LootRecipientContext recipients)
        {
            if (recipients.Group == null || recipients.EligiblePlayers.Count <= 1)
                return;

            List<Identity> eligibleIdentities = recipients.EligiblePlayers
                .Select(p => p.Identity)
                .ToList();
            List<Identity> masterLootCandidates = recipients.MasterLootCandidates
                .Select(p => p.Identity)
                .Distinct()
                .ToList();
            if (masterLootCandidates.Count == 0)
                masterLootCandidates = eligibleIdentities;

            foreach (LootInstanceItem item in lootInstance.Where(i => !i.Delivered))
            {
                if (item.ItemQualityId <= RetailCertainRules.TrashItemMaxQualityId && eligibleIdentities.Count > 1)
                {
                    item.ConfigureRoll(eligibleIdentities);
                    continue;
                }

                switch (recipients.Group.GetLootRule(item.ItemQualityId))
                {
                    case LootRule.RoundRobin:
                    {
                        GroupLootMember winner = groupStateManager.NextRoundRobinWinner(recipients.Group, recipients.EligibleMembers);
                        if (recipients.LooterIds.TryGetValue(winner.Identity.Id, out uint winnerGuid))
                            item.ConfigureAssigned(winner.Identity, winnerGuid);
                        else
                            item.ConfigureFreeForAll(eligibleIdentities);
                        break;
                    }
                    case LootRule.NeedBeforeGreed:
                        item.ConfigureRoll(eligibleIdentities);
                        break;
                    case LootRule.Master:
                    {
                        List<Identity> masters = eligibleIdentities.Contains(recipients.Group.Leader)
                            ? [recipients.Group.Leader]
                            : [recipients.EligiblePlayers[0].Identity];
                        item.ConfigureMaster(masters, eligibleIdentities, masterLootCandidates);
                        break;
                    }
                    case LootRule.FreeForAll:
                    default:
                        item.ConfigureFreeForAll(eligibleIdentities);
                        break;
                }
            }
        }

        private void TryGrantRandomOmnibitKillReward(IPlayer player, uint ownerUnitId)
        {
            if (Random.Shared.NextDouble() >= OMNIBIT_KILL_DROP_CHANCE)
            {
                log.Trace($"Random omnibit kill reward did not roll for player {player?.CharacterId.ToString() ?? "none"}, ownerUnit={ownerUnitId}.");
                return;
            }

            int maxExclusive = Math.Max(OMNIBIT_KILL_MIN_AMOUNT + 1, OMNIBIT_KILL_MAX_BASE_AMOUNT + (int)player.Level);
            uint amount = (uint)Random.Shared.Next(OMNIBIT_KILL_MIN_AMOUNT, maxExclusive);
            log.Trace($"Random omnibit kill reward rolled for player {player.CharacterId}, ownerUnit={ownerUnitId}, amount={amount}.");
            GiveRandomOmnibitKillReward(player, amount, ownerUnitId);
        }

        internal static void GiveRandomOmnibitKillReward(IPlayer player, uint amount, uint ownerUnitId)
        {
            GiveImmediateLoot(player, LootItemType.AccountCurrency, (uint)AccountCurrencyType.Omnibit, amount, ownerUnitId);
        }

        private static void TryAddLootItem(LootInstance lootInstance, LootItem item, uint count, string source)
        {
            if (count == 0u)
            {
                log.Trace($"Loot item skipped because count was zero: source={source}, ownerUnit={lootInstance.OwnerUnitId}, type={item.Type}, staticId={item.StaticId}.");
                return;
            }

            if (!CanDeliverLootItem(item))
            {
                log.Trace($"Loot item skipped because it is not deliverable: source={source}, ownerUnit={lootInstance.OwnerUnitId}, type={item.Type}, staticId={item.StaticId}, count={count}.");
                return;
            }

            LootInstanceItem instanceItem = lootInstance.AddLootItem(item.StaticId, item.Type, count);
            log.Trace($"Loot item added: source={source}, ownerUnit={lootInstance.OwnerUnitId}, lootUnitId={instanceItem.Id}, type={item.Type}, staticId={item.StaticId}, count={count}, mergedAmount={instanceItem.Amount}.");
        }

        private static bool CanDeliverLootItem(LootItem item)
        {
            return item.Type switch
            {
                LootItemType.AccountCurrency => IsDefinedAccountCurrency(item.StaticId),
                LootItemType.AccountItem     => HasAccountItem(item.StaticId),
                LootItemType.Cash            => IsDefinedCharacterCurrency(item.StaticId),
                LootItemType.StaticItem      => ItemManager.Instance.GetItemInfo(item.StaticId) != null,
                LootItemType.VirtualItem     => HasVirtualItem(item.StaticId),
                _                            => false
            };
        }

        private bool HasItemLoot(IItem lootedItem, Func<LootGroup, bool> lootGroupFilter)
        {
            return lootedItem?.Info != null
                && itemLoot.TryGetValue(lootedItem.Info.Entry.Id, out List<LootGroup> itemLootGroups)
                && itemLootGroups.Any(lootGroupFilter);
        }

        private bool TryGenerateItemLoot(
            IItem lootedItem,
            IPlayer looter,
            Func<LootGroup, bool> lootGroupFilter,
            string missingReasonPrefix,
            string emptyReasonPrefix,
            out IReadOnlyList<GeneratedLootItem> items,
            out string reason)
        {
            items  = [];
            reason = string.Empty;

            if (lootedItem?.Info == null)
            {
                reason = "missing-item";
                return false;
            }

            if (!itemLoot.TryGetValue(lootedItem.Info.Entry.Id, out List<LootGroup> itemLootGroups))
            {
                reason = $"{missingReasonPrefix}:{lootedItem.Info.Entry.Id}";
                return false;
            }

            var generatedItems = new Dictionary<(LootItemType Type, uint StaticId), uint>();
            foreach (LootGroup lootGroup in itemLootGroups.Where(lootGroupFilter))
            {
                foreach ((LootItem item, uint count) in lootGroup.GenerateLootDrops(looter))
                {
                    if (count == 0u)
                        continue;

                    if (!CanDeliverLootItem(item))
                    {
                        reason = $"invalid-loot-item:{item.Type}:{item.StaticId}";
                        return false;
                    }

                    var key = (item.Type, item.StaticId);
                    ulong total = generatedItems.GetValueOrDefault(key) + (ulong)count;
                    if (total > uint.MaxValue)
                    {
                        reason = $"loot-count-overflow:{item.Type}:{item.StaticId}";
                        return false;
                    }

                    generatedItems[key] = (uint)total;
                }
            }

            if (generatedItems.Count == 0)
            {
                reason = $"{emptyReasonPrefix}:{lootedItem.Info.Entry.Id}";
                return false;
            }

            items = generatedItems
                .Select(i => new GeneratedLootItem(i.Key.Type, i.Key.StaticId, i.Value))
                .ToList();
            return true;
        }

        private static bool CanDeliverGeneratedItemLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, out string reason)
        {
            reason = string.Empty;
            if (looter == null)
            {
                reason = "missing-looter";
                return false;
            }

            ArgumentNullException.ThrowIfNull(items);
            foreach (GeneratedLootItem item in items)
            {
                if (item.Count == 0u)
                    continue;

                switch (item.Type)
                {
                    case LootItemType.StaticItem:
                        if (!LootInstanceItem.CanDeliverStaticItem(looter, item.StaticId, item.Count, out bool inventoryFull))
                        {
                            reason = inventoryFull
                                ? "inventory-full"
                                : $"invalid-loot-item:{item.Type}:{item.StaticId}";
                            return false;
                        }
                        break;
                    case LootItemType.AccountCurrency:
                    case LootItemType.AccountItem:
                    case LootItemType.Cash:
                    case LootItemType.VirtualItem:
                        break;
                    default:
                        reason = $"invalid-loot-item:{item.Type}:{item.StaticId}";
                        return false;
                }
            }

            return true;
        }

        private static bool TryDeliverGeneratedItemLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, uint ownerUnitId)
        {
            if (looter == null)
                return false;

            ArgumentNullException.ThrowIfNull(items);
            List<GeneratedLootItem> generatedItems = items.ToList();
            log.Trace($"Generated item loot delivery started for player {looter.CharacterId}, ownerUnit={ownerUnitId}, generatedItems=[{FormatGeneratedLootItems(generatedItems)}].");

            LootInstance lootInstance = new(ownerUnitId, CreatePlayerLooterMap(looter), LooterType.Player, LootEntityType.Item)
            {
                Explosion = true
            };

            bool addedAny = false;
            foreach (GeneratedLootItem item in generatedItems)
            {
                if (item.Count == 0u)
                    continue;

                LootInstanceItem grantedItem = lootInstance.AddLootItem(item.StaticId, item.Type, item.Count);
                grantedItem.SetWinner(looter);
                addedAny = true;
            }

            if (!addedAny || !lootInstance.DeliverAllLoot(looter))
            {
                log.Trace($"Generated item loot delivery failed for player {looter.CharacterId}, ownerUnit={ownerUnitId}: addedAny={addedAny}, items=[{FormatLootInstanceItems(lootInstance)}].");
                return false;
            }

            log.Trace($"Generated item loot delivery succeeded for player {looter.CharacterId}, ownerUnit={ownerUnitId}, items=[{FormatLootInstanceItems(lootInstance)}].");
            SendGrantedLootNotifyAndRemove(lootInstance, looter);
            return true;
        }

        private static bool IsNonSalvageItemLootGroup(LootGroup lootGroup)
        {
            return !IsSalvageItemLootGroup(lootGroup);
        }

        private static bool IsSalvageItemLootGroup(LootGroup lootGroup)
        {
            return lootGroup?.Comment?.StartsWith(ITEM_SALVAGE_LOOT_GROUP_COMMENT_PREFIX, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static void SendGrantedLootNotifyAndRemove(LootInstance lootInstance, IPlayer looter)
        {
            lootInstance.SendLootNotify(looter, includeGrantedItems: true);
            lootInstance.SendLootRemove(looter);
        }

        public void SendLootNotify(IPlayer looter, uint ownerUnitId)
        {
            SendLootNotify(looter, ownerUnitId, suppressIfUnchanged: true, sendRemoveWhenEmpty: true);
        }

        private void SendLootNotify(IPlayer looter, uint ownerUnitId, bool suppressIfUnchanged, bool sendRemoveWhenEmpty)
        {
            List<LootInstance> matchingInstances = GetLootInstancesForOwner(ownerUnitId)
                .Where(i => i.HasLooter(looter.CharacterId) && !i.HasExpired)
                .ToList();
            log.Trace($"Loot notify lookup for player {looter.CharacterId}, ownerUnit={ownerUnitId}: matchingInstances={matchingInstances.Count}.");
            if (matchingInstances.Count == 0)
            {
                if (sendRemoveWhenEmpty)
                {
                    log.Trace($"Loot notify request found no matching active loot for player {looter.CharacterId}, ownerUnit={ownerUnitId}; sending remove.");
                    looter.Session.EnqueueMessageEncrypted(new ServerLootRemovePacket
                    {
                        OwnerUnitId = ownerUnitId
                    });
                }

                return;
            }

            foreach (LootInstance lootInstance in matchingInstances)
            {
                log.Trace($"Sending loot notify for player {looter.CharacterId}, ownerUnit={ownerUnitId}, items=[{FormatLootInstanceItems(lootInstance)}].");
                lootInstance.SendLootNotify(looter, suppressIfUnchanged: suppressIfUnchanged);
            }
        }

        public void SendLootNotifyForVisibleOwner(IPlayer looter, IWorldEntity owner)
        {
            if (looter == null || owner == null)
                return;

            SendLootNotify(looter, owner.Guid, suppressIfUnchanged: false, sendRemoveWhenEmpty: false);
        }

        public bool TryGetLootRuntimeSnapshot(IPlayer looter, uint ownerUnitId, out LootRuntimeSnapshot snapshot)
        {
            snapshot = null;
            if (looter == null)
                return false;

            LootInstance lootInstance = GetLootInstancesForOwner(ownerUnitId).FirstOrDefault(i => i.HasLooter(looter.CharacterId) && !i.HasExpired);
            if (lootInstance == null)
                return false;

            snapshot = lootInstance.CreateRuntimeSnapshot(looter);
            return true;
        }

        public void GiveLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId)
        {
            if (looter == null)
                return;

            log.Trace($"Loot collect lookup for player {looter.CharacterId}, ownerUnit={ownerUnitId}, requestedLootUnitId={lootUnitId}.");
            LootInstance lootInstance = FindLootInstance(ownerUnitId, lootUnitId, out uint resolvedLootUnitId);
            if (lootInstance == null)
            {
                log.Debug($"Player {looter.CharacterId} requested unknown loot item {lootUnitId} from owner {ownerUnitId}.");
                return;
            }
            log.Trace($"Loot collect resolved for player {looter.CharacterId}, ownerUnit={ownerUnitId}, requestedLootUnitId={lootUnitId}, resolvedLootUnitId={resolvedLootUnitId}, items=[{FormatLootInstanceItems(lootInstance)}].");

            if (!CanAccessLootInstance(lootInstance, looter, ownerUnitId, resolvedLootUnitId, "collect loot"))
                return;

            IWorldEntity owner = GetLootOwner(looter, ownerUnitId);
            float distance = owner?.Position.GetDistance(looter.Position) ?? float.PositiveInfinity;
            if (owner == null || distance > LOOT_RANGE)
            {
                log.Debug($"Player {looter.CharacterId} cannot collect loot {resolvedLootUnitId} from owner {ownerUnitId}: ownerFound={owner != null}, distance={distance:R}, lootRange={LOOT_RANGE:R}.");
                return;
            }

            if (lootInstance.HasExpired)
            {
                log.Trace($"Loot collect request found expired instance for player {looter.CharacterId}, ownerUnit={ownerUnitId}, resolvedLootUnitId={resolvedLootUnitId}; sending remove.");
                lootInstance.SendLootRemove(looter);
                return;
            }

            bool delivered = lootInstance.GiveLoot(looter, resolvedLootUnitId);
            log.Trace($"Loot collect delivery result for player {looter.CharacterId}, ownerUnit={ownerUnitId}, resolvedLootUnitId={resolvedLootUnitId}: delivered={delivered}, hasExpired={lootInstance.HasExpired}, items=[{FormatLootInstanceItems(lootInstance)}].");
            if (delivered && lootInstance.HasExpired)
                RemoveExpiredLootInstance(lootInstance, sendRemove: true);
        }

        public void GiveAllLootInRange(IPlayer looter)
        {
            if (looter == null)
                return;

            log.Trace($"Loot vacuum started for player {looter.CharacterId}.");
            foreach (LootInstance lootInstance in GetLootInstancesForLooter(looter.CharacterId).Where(i => !i.HasExpired).ToList())
            {
                IWorldEntity owner = GetLootOwner(looter, lootInstance.OwnerUnitId);
                float distance = owner?.Position.GetDistance(looter.Position) ?? float.PositiveInfinity;
                if (owner == null || distance > LOOT_RANGE)
                {
                    log.Trace($"Loot vacuum skipped instance for player {looter.CharacterId}, ownerUnit={lootInstance.OwnerUnitId}: ownerFound={owner != null}, distance={distance:R}, lootRange={LOOT_RANGE:R}.");
                    continue;
                }

                int deliveredCount = 0;
                bool skippedPendingLoot = false;
                foreach (LootInstanceItem item in lootInstance.Where(i => !i.Delivered).ToList())
                {
                    if (!item.CanLoot(looter.CharacterId))
                        skippedPendingLoot = true;

                    if (lootInstance.GiveLoot(looter, item.Id))
                        deliveredCount++;
                }

                log.Trace($"Loot vacuum processed instance for player {looter.CharacterId}, ownerUnit={lootInstance.OwnerUnitId}: deliveredCount={deliveredCount}, hasExpired={lootInstance.HasExpired}, items=[{FormatLootInstanceItems(lootInstance)}].");

                if (lootInstance.HasExpired)
                {
                    RemoveExpiredLootInstance(lootInstance, sendRemove: true);
                }
                else if (deliveredCount == 0 && skippedPendingLoot)
                {
                    log.Trace($"Loot vacuum refreshed pending loot notify for player {looter.CharacterId}, ownerUnit={lootInstance.OwnerUnitId}.");
                    lootInstance.SendLootNotify(looter);
                }
            }
        }

        public void RollLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId, LootRollAction action)
        {
            if (looter == null)
                return;

            log.Trace($"Loot roll lookup for player {looter.CharacterId}, ownerUnit={ownerUnitId}, requestedLootUnitId={lootUnitId}, action={action}.");
            LootInstance lootInstance = FindLootInstance(ownerUnitId, lootUnitId, out uint resolvedLootUnitId);
            if (lootInstance == null)
            {
                log.Debug($"Player {looter.CharacterId} requested unknown loot roll {lootUnitId} from owner {ownerUnitId}.");
                return;
            }

            if (!CanAccessLootInstance(lootInstance, looter, ownerUnitId, resolvedLootUnitId, "roll on loot"))
                return;

            IWorldEntity owner = GetLootOwner(looter, ownerUnitId);
            float distance = owner?.Position.GetDistance(looter.Position) ?? float.PositiveInfinity;
            if (owner == null || distance > LOOT_RANGE)
            {
                log.Debug($"Player {looter.CharacterId} cannot roll on loot {resolvedLootUnitId} from owner {ownerUnitId}: ownerFound={owner != null}, distance={distance:R}, lootRange={LOOT_RANGE:R}.");
                return;
            }

            if (lootInstance.HasExpired)
            {
                log.Trace($"Loot roll request found expired instance for player {looter.CharacterId}, ownerUnit={ownerUnitId}, resolvedLootUnitId={resolvedLootUnitId}; sending remove.");
                lootInstance.SendLootRemove(looter);
                return;
            }

            bool recorded = lootInstance.RollLoot(looter, resolvedLootUnitId, action);
            log.Trace($"Loot roll result for player {looter.CharacterId}, ownerUnit={ownerUnitId}, resolvedLootUnitId={resolvedLootUnitId}, action={action}: recorded={recorded}, items=[{FormatLootInstanceItems(lootInstance)}].");
            if (recorded && lootInstance.HasExpired)
                RemoveExpiredLootInstance(lootInstance, sendRemove: false);
        }

        public void AssignMasterLoot(IPlayer master, uint ownerUnitId, uint lootUnitId, Identity assignee)
        {
            if (master == null || assignee == null)
                return;

            log.Trace($"Master loot assignment lookup for player {master.CharacterId}, ownerUnit={ownerUnitId}, requestedLootUnitId={lootUnitId}, assignee={assignee}.");
            LootInstance lootInstance = FindLootInstance(ownerUnitId, lootUnitId, out uint resolvedLootUnitId);
            if (lootInstance == null)
            {
                log.Debug($"Player {master.CharacterId} requested unknown master-loot assignment {lootUnitId} from owner {ownerUnitId}.");
                return;
            }

            if (!CanAccessLootInstance(lootInstance, master, ownerUnitId, resolvedLootUnitId, "assign master loot"))
                return;

            IWorldEntity owner = GetLootOwner(master, ownerUnitId);
            float distance = owner?.Position.GetDistance(master.Position) ?? float.PositiveInfinity;
            if (owner == null || distance > LOOT_RANGE)
            {
                log.Debug($"Player {master.CharacterId} cannot assign master loot {resolvedLootUnitId} from owner {ownerUnitId}: ownerFound={owner != null}, distance={distance:R}, lootRange={LOOT_RANGE:R}.");
                return;
            }

            if (lootInstance.HasExpired)
            {
                log.Trace($"Master loot assignment found expired instance for player {master.CharacterId}, ownerUnit={ownerUnitId}, resolvedLootUnitId={resolvedLootUnitId}; sending remove.");
                lootInstance.SendLootRemove(master);
                return;
            }

            bool assigned = lootInstance.AssignMasterLoot(master, resolvedLootUnitId, assignee);
            log.Trace($"Master loot assignment result for player {master.CharacterId}, ownerUnit={ownerUnitId}, resolvedLootUnitId={resolvedLootUnitId}, assignee={assignee}: assigned={assigned}, items=[{FormatLootInstanceItems(lootInstance)}].");
            if (assigned && lootInstance.HasExpired)
                RemoveExpiredLootInstance(lootInstance, sendRemove: false);
        }

        private static bool CanAccessLootInstance(LootInstance lootInstance, IPlayer player, uint ownerUnitId, uint resolvedLootUnitId, string action)
        {
            if (lootInstance.HasLooter(player.CharacterId))
                return true;

            log.Debug($"Player {player.CharacterId} cannot {action} {resolvedLootUnitId} from owner {ownerUnitId}: not a tracked looter.");
            return false;
        }

        private LootInstance FindLootInstance(uint ownerUnitId, uint lootUnitId, out uint resolvedLootUnitId)
        {
            foreach (LootInstance lootInstance in GetLootInstancesForOwner(ownerUnitId))
            {
                if (TryResolveLootUnitId(lootInstance, lootUnitId, out resolvedLootUnitId))
                    return lootInstance;
            }

            resolvedLootUnitId = lootUnitId;
            return null;
        }

        private static bool TryResolveLootUnitId(LootInstance lootInstance, uint lootUnitId, out uint resolvedLootUnitId)
        {
            if (lootInstance.HasLootInstanceId(lootUnitId))
            {
                resolvedLootUnitId = lootUnitId;
                return true;
            }

            uint normalisedLootUnitId = RemoveClientLootUnitIdOverlay(lootUnitId);
            if (normalisedLootUnitId != lootUnitId && lootInstance.HasLootInstanceId(normalisedLootUnitId))
            {
                resolvedLootUnitId = normalisedLootUnitId;
                return true;
            }

            resolvedLootUnitId = lootUnitId;
            return false;
        }

        internal static uint RemoveClientLootUnitIdOverlay(uint lootUnitId)
        {
            return lootUnitId & ~CLIENT_LOOT_UNIT_ID_OVERLAY;
        }

        private static IWorldEntity GetLootOwner(IPlayer looter, uint ownerUnitId)
        {
            if (looter.Guid == ownerUnitId)
                return looter;

            return looter.GetVisible<IWorldEntity>(ownerUnitId);
        }

        public void GiveLoot(IPlayer looter, Item2Entry entry, uint count, uint ownerUnitId)
        {
            if (entry == null)
                return;

            GiveImmediateLoot(looter, LootItemType.StaticItem, entry.Id, count, ownerUnitId);
        }

        public void GiveLoot(IPlayer looter, VirtualItemEntry entry, uint count, uint ownerUnitId)
        {
            if (entry == null)
                return;

            GiveImmediateLoot(looter, LootItemType.VirtualItem, entry.Id, count, ownerUnitId);
        }

        public void GiveLoot(IPlayer looter, AccountCurrencyType accountCurrencyType, uint count, uint ownerUnitId)
        {
            GiveImmediateLoot(looter, LootItemType.AccountCurrency, (uint)accountCurrencyType, count, ownerUnitId, sendGrantedNotify: true);
        }

        public void GiveLoot(IPlayer looter, CurrencyType currencyType, uint count, uint ownerUnitId)
        {
            GiveImmediateLoot(looter, LootItemType.Cash, (uint)currencyType, count, ownerUnitId);
        }

        public bool TryGenerateLoot(uint lootGroupId, IPlayer looter, uint rollCount, out IReadOnlyList<GeneratedLootItem> items, out string reason)
        {
            items  = [];
            reason = string.Empty;

            if (looter == null)
            {
                reason = "no-looter";
                return false;
            }

            if (lootGroupId == 0u)
            {
                reason = "missing-loot-group";
                return false;
            }

            if (rollCount == 0u)
            {
                reason = "zero-roll-count";
                return false;
            }

            LootGroupModel lootGroupModel = DatabaseManager.Instance.GetDatabase<WorldDatabase>().GetLootGroup(lootGroupId);
            if (lootGroupModel == null)
            {
                reason = $"unknown-loot-group:{lootGroupId}";
                return false;
            }

            var lootGroup = new LootGroup(lootGroupModel);
            var generatedItems = new Dictionary<(LootItemType Type, uint StaticId), uint>();
            for (uint i = 0u; i < rollCount; i++)
            {
                foreach ((LootItem item, uint count) in lootGroup.GenerateLootDrops(looter))
                {
                    if (count == 0u)
                        continue;

                    if (!CanDeliverLootItem(item))
                    {
                        reason = $"invalid-loot-item:{item.Type}:{item.StaticId}";
                        return false;
                    }

                    var key = (item.Type, item.StaticId);
                    ulong total = generatedItems.GetValueOrDefault(key) + (ulong)count;
                    if (total > uint.MaxValue)
                    {
                        reason = $"loot-count-overflow:{item.Type}:{item.StaticId}";
                        return false;
                    }

                    generatedItems[key] = (uint)total;
                }
            }

            if (generatedItems.Count == 0)
            {
                reason = $"empty-loot-group:{lootGroupId}";
                return false;
            }

            items = generatedItems
                .Select(i => new GeneratedLootItem(i.Key.Type, i.Key.StaticId, i.Value))
                .ToList();
            return true;
        }

        public bool CanDeliverGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, out string reason)
        {
            reason = string.Empty;
            if (looter == null)
            {
                reason = "no-looter";
                return false;
            }

            ArgumentNullException.ThrowIfNull(items);

            foreach (GeneratedLootItem item in items)
            {
                if (item.Count == 0u)
                {
                    reason = $"zero-loot-count:{item.Type}:{item.StaticId}";
                    return false;
                }

                if (!CanDeliverLootItem(item))
                {
                    reason = $"invalid-loot-item:{item.Type}:{item.StaticId}";
                    return false;
                }
            }

            List<GeneratedLootItem> staticItems = items
                .Where(i => i.Type == LootItemType.StaticItem)
                .ToList();
            if (staticItems.Count == 0)
                return true;

            return CanHoldStaticLoot(looter, staticItems, out reason);
        }

        public void GiveGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, uint ownerUnitId, bool sendGrantedNotify = false, uint parentUnitId = 0u)
        {
            if (looter == null)
                return;

            ArgumentNullException.ThrowIfNull(items);
            List<GeneratedLootItem> generatedItems = items.ToList();
            uint resolvedParentUnitId = parentUnitId != 0u ? parentUnitId : ownerUnitId;
            log.Trace($"Giving generated loot to player {looter.CharacterId}, ownerUnit={ownerUnitId}, parentUnit={resolvedParentUnitId}, sendGrantedNotify={sendGrantedNotify}, generatedItems=[{FormatGeneratedLootItems(generatedItems)}].");

            if (sendGrantedNotify)
            {
                GiveGeneratedLootWithGrantedNotify(looter, generatedItems, ownerUnitId, resolvedParentUnitId);
                return;
            }

            foreach (GeneratedLootItem item in generatedItems)
                GiveImmediateLoot(looter, item.Type, item.StaticId, item.Count, ownerUnitId, sendGrantedNotify, resolvedParentUnitId);
        }

        private static void GiveGeneratedLootWithGrantedNotify(IPlayer looter, IReadOnlyCollection<GeneratedLootItem> items, uint ownerUnitId, uint parentUnitId)
        {
            LootInstance lootInstance = new(ownerUnitId, parentUnitId, CreatePlayerLooterMap(looter), LooterType.Player, LootEntityType.Creature)
            {
                Explosion = true
            };

            foreach (GeneratedLootItem item in items)
            {
                if (item.Count == 0u)
                    continue;

                LootInstanceItem grantedItem = lootInstance.AddLootItem(item.StaticId, item.Type, item.Count);
                grantedItem.SetWinner(looter);
            }

            bool deliveredAny = false;
            int failedCount = 0;
            foreach (LootInstanceItem grantedItem in lootInstance.ToList())
            {
                if (grantedItem.DeliverItem(looter, sendAsGrant: false))
                    deliveredAny = true;
                else
                    failedCount++;
            }

            if (deliveredAny && failedCount == 0)
            {
                log.Trace($"Generated loot delivered with granted notify for player {looter.CharacterId}: ownerUnit={ownerUnitId}, parentUnit={parentUnitId}, items=[{FormatLootInstanceItems(lootInstance)}].");
                SendGrantedLootNotifyAndRemove(lootInstance, looter);
            }
            else
            {
                log.Trace($"Generated loot delivery with granted notify failed for player {looter.CharacterId}: ownerUnit={ownerUnitId}, parentUnit={parentUnitId}, deliveredAny={deliveredAny}, failedCount={failedCount}, items=[{FormatLootInstanceItems(lootInstance)}].");
            }
        }

        private static void GiveImmediateLoot(IPlayer looter, LootItemType type, uint staticId, uint count, uint ownerUnitId, bool sendGrantedNotify = false, uint parentUnitId = 0u)
        {
            if (looter == null || count == 0u)
                return;

            uint resolvedParentUnitId = parentUnitId != 0u ? parentUnitId : ownerUnitId;
            log.Trace($"Giving immediate loot to player {looter.CharacterId}: ownerUnit={ownerUnitId}, parentUnit={resolvedParentUnitId}, type={type}, staticId={staticId}, count={count}, sendGrantedNotify={sendGrantedNotify}.");

            if (sendGrantedNotify)
            {
                LootInstance lootInstance = new(ownerUnitId, resolvedParentUnitId, CreatePlayerLooterMap(looter), LooterType.Player, LootEntityType.Creature)
                {
                    Explosion = true
                };

                LootInstanceItem grantedItem = lootInstance.AddLootItem(staticId, type, count);
                grantedItem.SetWinner(looter);
                if (grantedItem.DeliverItem(looter, sendAsGrant: false))
                {
                    log.Trace($"Immediate loot delivered with granted notify for player {looter.CharacterId}: ownerUnit={ownerUnitId}, parentUnit={resolvedParentUnitId}, items=[{FormatLootInstanceItems(lootInstance)}].");
                    SendGrantedLootNotifyAndRemove(lootInstance, looter);
                }
                else
                {
                    log.Trace($"Immediate loot delivery with granted notify failed for player {looter.CharacterId}: ownerUnit={ownerUnitId}, parentUnit={resolvedParentUnitId}, items=[{FormatLootInstanceItems(lootInstance)}].");
                }

                return;
            }

            LootInstanceItem item = new(staticId, type, count);
            item.SetOwnerUnit(ownerUnitId);
            item.SetWinner(looter);
            bool delivered = item.DeliverItem(looter);
            log.Trace($"Immediate loot delivery result for player {looter.CharacterId}: ownerUnit={ownerUnitId}, lootUnitId={item.Id}, type={type}, staticId={staticId}, count={count}, delivered={delivered}.");
        }

        private static bool CanDeliverLootItem(GeneratedLootItem item)
        {
            return item.Type switch
            {
                LootItemType.AccountCurrency => IsDefinedAccountCurrency(item.StaticId),
                LootItemType.AccountItem     => HasAccountItem(item.StaticId),
                LootItemType.Cash            => IsDefinedCharacterCurrency(item.StaticId),
                LootItemType.StaticItem      => ItemManager.Instance.GetItemInfo(item.StaticId) != null,
                LootItemType.VirtualItem     => HasVirtualItem(item.StaticId),
                _                            => false
            };
        }

        private static string FormatGeneratedLootItems(IEnumerable<GeneratedLootItem> items)
        {
            return string.Join(", ", items.Select(item => $"type={item.Type}:staticId={item.StaticId}:count={item.Count}"));
        }

        private static string FormatLootInstanceItems(IEnumerable<LootInstanceItem> items)
        {
            return string.Join(", ", items.Select(item => $"lootUnitId={item.Id}:type={item.Type}:staticId={item.StaticId}:amount={item.Amount}:delivered={item.Delivered}:winnerCharacter={item.WinnerCharacterId}:winnerGuid={item.WinnerGuid}:requiresRoll={item.RequiresRoll}:onlyMasterLootable={item.OnlyMasterLootable}:rollTime={item.RollTime}:quality={item.ItemQualityId}"));
        }

        private static bool IsDefinedAccountCurrency(uint staticId)
        {
            return staticId <= int.MaxValue
                && Enum.IsDefined(typeof(AccountCurrencyType), (int)staticId)
                && HasRewardTableEntry(
                    GameTableManager.Instance.AccountCurrencyType,
                    AccountCurrencyTypeTableName,
                    staticId,
                    nameof(IsDefinedAccountCurrency));
        }

        private static bool HasAccountItem(uint staticId)
        {
            return HasRewardTableEntry(
                GameTableManager.Instance.AccountItem,
                AccountItemTableName,
                staticId,
                nameof(HasAccountItem));
        }

        private static bool HasVirtualItem(uint staticId)
        {
            return HasRewardTableEntry(
                GameTableManager.Instance.VirtualItem,
                VirtualItemTableName,
                staticId,
                nameof(HasVirtualItem));
        }

        private static bool IsDefinedCharacterCurrency(uint staticId)
        {
            return staticId <= int.MaxValue && Enum.IsDefined(typeof(CurrencyType), (int)staticId);
        }

        private static bool HasRewardTableEntry<T>(GameTable<T> table, string tableName, uint staticId, string context) where T : class, new()
        {
            context = nameof(GlobalLootManager) + "." + context;
            if (table == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    tableName,
                    context,
                    MissingGameDataSeverity.PlayerImpacting,
                    "Cannot validate generated loot reward.");
                return false;
            }

            if (table.GetEntry(staticId) != null)
                return true;

            MissingGameDataDiagnostics.ReportMissingRow(
                tableName,
                staticId,
                context,
                MissingGameDataSeverity.PlayerImpacting,
                "Cannot validate generated loot reward.");
            return false;
        }

        private static bool CanHoldStaticLoot(IPlayer looter, IEnumerable<GeneratedLootItem> staticItems, out string reason)
        {
            reason = string.Empty;

            IBag inventoryBag = looter.Inventory.SingleOrDefault(bag => bag.Location == InventoryLocation.Inventory);
            if (inventoryBag == null)
            {
                reason = "missing-inventory-bag";
                return false;
            }

            ulong remainingSlots = inventoryBag.SlotsRemaining;
            foreach (IGrouping<uint, GeneratedLootItem> itemGroup in staticItems.GroupBy(i => i.StaticId))
            {
                IItemInfo itemInfo = ItemManager.Instance.GetItemInfo(itemGroup.Key);
                if (itemInfo == null)
                {
                    reason = $"invalid-static-item:{itemGroup.Key}";
                    return false;
                }

                ulong remainingCount = itemGroup.Aggregate(0ul, (current, item) => current + item.Count);
                if (itemInfo.IsStackable())
                {
                    foreach (IItem item in inventoryBag.Where(i => i.Info.Id == itemInfo.Id && i.ExpirationTimeLeft == 0u))
                    {
                        if (item.StackCount >= item.Info.Entry.MaxStackCount)
                            continue;

                        remainingCount -= Math.Min(remainingCount, item.Info.Entry.MaxStackCount - item.StackCount);
                        if (remainingCount == 0ul)
                            break;
                    }
                }

                if (remainingCount == 0ul)
                    continue;

                uint perNewStack = itemInfo.IsStackable() ? itemInfo.Entry.MaxStackCount : 1u;
                if (perNewStack == 0u)
                {
                    reason = $"invalid-stack-size:{itemInfo.Id}";
                    return false;
                }

                ulong requiredSlots = (remainingCount + perNewStack - 1ul) / perNewStack;
                if (requiredSlots > remainingSlots)
                {
                    reason = "inventory-full";
                    return false;
                }

                remainingSlots -= requiredSlots;
            }

            return true;
        }
    }
}
