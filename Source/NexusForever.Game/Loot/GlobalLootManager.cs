using System.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
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
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NLog;

namespace NexusForever.Game.Loot
{
    public class GlobalLootManager : Singleton<GlobalLootManager>, IGlobalLootManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const float LOOT_RANGE = 35f;
        private const double OMNIBIT_KILL_DROP_CHANCE = 0.35d;
        private const int OMNIBIT_KILL_MIN_AMOUNT = 7;
        private const int OMNIBIT_KILL_MAX_BASE_AMOUNT = 25;

        private sealed class LootRecipientContext
        {
            public Dictionary<ulong, uint> LooterIds { get; init; } = [];
            public LooterType LooterType { get; init; }
            public GroupLootState Group { get; init; }
            public IReadOnlyList<IPlayer> EligiblePlayers { get; init; } = [];
            public IReadOnlyList<GroupLootMember> EligibleMembers { get; init; } = [];
            public IReadOnlyList<IPlayer> MasterLootCandidates { get; init; } = [];
        }

        public uint NextLootId
        {
            get
            {
                uint next = nextLootId++;
                return next == 0u ? nextLootId++ : next;
            }
        }

        private uint nextLootId = 0x80000000u;

        private readonly Dictionary<uint, List<LootGroup>> creatureLoot = [];
        private readonly Dictionary<uint, List<LootGroup>> itemLoot = [];
        private readonly Dictionary<uint, List<LootItem>> directCreatureLoot = [];
        private readonly List<LootInstance> lootInstances = [];

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
            directCreatureLoot.Clear();
            lootInstances.Clear();

            WorldDatabase worldDatabase = DatabaseManager.Instance.GetDatabase<WorldDatabase>();

            foreach (ItemLootModel itemLootModel in worldDatabase.GetAllItemLootTables())
                BuildLoot(itemLootModel.Id, LootEntityType.Item, itemLootModel.LootGroup);

            foreach (EntityLootModel entityLootModel in worldDatabase.GetAllEntityLootTables())
                BuildLoot(entityLootModel.Id, LootEntityType.Creature, entityLootModel.LootGroup);

            int skippedDirectRows = 0;
            int skippedDirectRowsWithLootTables = 0;
            foreach (CreatureLootModel creatureLootModel in worldDatabase.GetCreatureLoot())
            {
                if (creatureLoot.ContainsKey(creatureLootModel.CreatureId))
                {
                    skippedDirectRowsWithLootTables++;
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
            if (skippedDirectRowsWithLootTables > 0)
                log.Info($"Skipped {skippedDirectRowsWithLootTables} imported creature loot row(s) because mapped loot_group rows are already present for those creatures.");
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

        private static bool IsMappedFlatLootGroup(LootGroupModel lootGroupModel)
        {
            return lootGroupModel.Comment?.StartsWith("DataMapping creature_loot", StringComparison.OrdinalIgnoreCase) == true;
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

        private void RemoveExpiredLootInstances()
        {
            lootInstances.RemoveAll(i => i.HasExpired);
        }

        public bool DropLoot(IPlayer looter, IWorldEntity lootedEntity)
        {
            if (looter == null || lootedEntity == null)
                return false;

            if (lootedEntity is IPetEntity)
                return false;

            if (lootedEntity.CreatureId == 0u)
                return false;

            Creature2Entry entry = GameTableManager.Instance.Creature2.GetEntry(lootedEntity.CreatureId);
            if (entry == null)
            {
                log.Warn($"Creature2 entry {lootedEntity.CreatureId} not found while generating loot for owner {lootedEntity.Guid}.");
                return false;
            }

            LootRecipientContext recipients = CreateLootRecipientContext(looter, lootedEntity);
            LootInstance lootInstance = GenerateLootInstance(entry.Id, lootedEntity.Guid, looter, recipients.LooterIds, recipients.LooterType, LootEntityType.Creature);
            ConfigureLootDistribution(lootInstance, recipients);
            if (lootInstance.HasExpired)
                return false;

            lootInstances.Add(lootInstance);
            foreach (IPlayer player in recipients.EligiblePlayers)
                lootInstance.SendLootNotify(player);

            return true;
        }

        public bool HasLoot(IItem lootedItem)
        {
            return lootedItem?.Info != null && itemLoot.ContainsKey(lootedItem.Info.Entry.Id);
        }

        public bool DropLoot(IPlayer looter, IItem lootedItem)
        {
            if (looter == null || lootedItem?.Info == null)
                return false;

            if (!TryGenerateItemLoot(lootedItem, looter, out IReadOnlyList<GeneratedLootItem> items, out _))
                return false;

            if (!CanDeliverGeneratedLoot(looter, items, out _))
                return false;

            return TryDeliverGeneratedItemLoot(looter, items, looter.Guid);
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

            if (!TryGenerateItemLoot(lootedItem, looter, out IReadOnlyList<GeneratedLootItem> items, out reason))
                return false;

            if (!CanDeliverGeneratedLoot(looter, items, out reason))
                return false;

            if (!looter.Inventory.ItemUse(lootedItem))
            {
                reason = "item-use-failed";
                return false;
            }

            if (TryDeliverGeneratedItemLoot(looter, items, looter.Guid))
                return true;

            reason = "loot-delivery-failed";
            return false;
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

                if (player.Position.GetDistance(lootedEntity.Position) > LOOT_RANGE)
                    continue;

                eligible.Add((player, member));
            }

            if (eligible.All(e => e.Player.CharacterId != looter.CharacterId))
            {
                GroupLootMember member = group.GetMember(looter.Identity) ?? new GroupLootMember
                {
                    Identity   = looter.Identity,
                    GroupIndex = 0u
                };
                if (sameMapMembers.All(e => e.Player.CharacterId != looter.CharacterId))
                    sameMapMembers.Add((looter, member));

                eligible.Add((looter, member));
            }

            if (eligible.Count <= 1)
                return CreateSoloRecipientContext(looter);

            return new LootRecipientContext
            {
                LooterType      = LooterType.Group,
                Group           = group,
                LooterIds       = eligible.ToDictionary(e => e.Player.CharacterId, e => e.Player.Guid),
                EligiblePlayers = eligible.Select(e => e.Player).ToList(),
                EligibleMembers = eligible.Select(e => e.Member).ToList(),
                MasterLootCandidates = sameMapMembers.Select(e => e.Player).Distinct().ToList()
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

        private LootInstance GenerateLootInstance(uint entityId, uint ownerUnitId, IPlayer player, Dictionary<ulong, uint> looterIds, LooterType looterType, LootEntityType lootEntityType)
        {
            LootInstance lootInstance = new(ownerUnitId, looterIds, looterType, lootEntityType);

            switch (lootEntityType)
            {
                case LootEntityType.Creature:
                    if (creatureLoot.TryGetValue(entityId, out List<LootGroup> creatureLootGroups))
                    {
                        foreach (LootGroup lootGroup in creatureLootGroups)
                        {
                            foreach ((LootItem item, uint count) in lootGroup.GenerateLootDrops(player))
                                TryAddLootItem(lootInstance, item, count);
                        }
                    }

                    if (directCreatureLoot.TryGetValue(entityId, out List<LootItem> importedItems))
                    {
                        foreach (LootItem item in importedItems)
                        {
                            if (item.TryGetDrop(out uint count))
                                TryAddLootItem(lootInstance, item, count);
                        }
                    }

                    TryGrantRandomOmnibitKillReward(player, ownerUnitId);
                    break;
                case LootEntityType.Item:
                    if (itemLoot.TryGetValue(entityId, out List<LootGroup> itemLootGroups))
                    {
                        foreach (LootGroup lootGroup in itemLootGroups)
                        {
                            foreach ((LootItem item, uint count) in lootGroup.GenerateLootDrops(player))
                                TryAddLootItem(lootInstance, item, count);
                        }
                    }
                    break;
            }

            return lootInstance;
        }

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
                return;

            int maxExclusive = Math.Max(OMNIBIT_KILL_MIN_AMOUNT + 1, OMNIBIT_KILL_MAX_BASE_AMOUNT + (int)player.Level);
            uint amount = (uint)Random.Shared.Next(OMNIBIT_KILL_MIN_AMOUNT, maxExclusive);
            GiveLoot(player, AccountCurrencyType.Omnibit, amount, ownerUnitId);
        }

        private static void TryAddLootItem(LootInstance lootInstance, LootItem item, uint count)
        {
            if (count == 0u || !CanDeliverLootItem(item))
                return;

            lootInstance.AddLootItem(item.StaticId, item.Type, count);
        }

        private static bool CanDeliverLootItem(LootItem item)
        {
            return item.Type switch
            {
                LootItemType.AccountCurrency => IsDefinedAccountCurrency(item.StaticId),
                LootItemType.AccountItem     => GameTableManager.Instance.AccountItem.GetEntry(item.StaticId) != null,
                LootItemType.Cash            => IsDefinedCharacterCurrency(item.StaticId),
                LootItemType.StaticItem      => ItemManager.Instance.GetItemInfo(item.StaticId) != null,
                LootItemType.VirtualItem     => GameTableManager.Instance.VirtualItem.GetEntry(item.StaticId) != null,
                _                            => false
            };
        }

        private bool TryGenerateItemLoot(IItem lootedItem, IPlayer looter, out IReadOnlyList<GeneratedLootItem> items, out string reason)
        {
            items  = [];
            reason = string.Empty;

            if (lootedItem?.Info == null)
            {
                reason = "missing-item";
                return false;
            }

            if (!itemLoot.TryGetValue(lootedItem.Info.Entry.Id, out List<LootGroup> itemLootGroups) || itemLootGroups.Count == 0)
            {
                reason = $"missing-item-loot:{lootedItem.Info.Entry.Id}";
                return false;
            }

            var generatedItems = new Dictionary<(LootItemType Type, uint StaticId), uint>();
            foreach (LootGroup lootGroup in itemLootGroups)
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
                reason = $"empty-item-loot:{lootedItem.Info.Entry.Id}";
                return false;
            }

            items = generatedItems
                .Select(i => new GeneratedLootItem(i.Key.Type, i.Key.StaticId, i.Value))
                .ToList();
            return true;
        }

        private static bool TryDeliverGeneratedItemLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, uint ownerUnitId)
        {
            if (looter == null)
                return false;

            ArgumentNullException.ThrowIfNull(items);

            LootInstance lootInstance = new(ownerUnitId, CreatePlayerLooterMap(looter), LooterType.Player, LootEntityType.Item)
            {
                Explosion = true
            };

            bool addedAny = false;
            foreach (GeneratedLootItem item in items)
            {
                if (item.Count == 0u)
                    continue;

                LootInstanceItem grantedItem = lootInstance.AddLootItem(item.StaticId, item.Type, item.Count);
                grantedItem.SetWinner(looter);
                addedAny = true;
            }

            if (!addedAny || !lootInstance.DeliverAllLoot(looter))
                return false;

            lootInstance.SendLootNotify(looter, includeGrantedItems: true);
            return true;
        }

        public void SendLootNotify(IPlayer looter, uint ownerUnitId)
        {
            foreach (LootInstance lootInstance in lootInstances.Where(i => i.OwnerUnitId == ownerUnitId && i.HasLooter(looter.CharacterId) && !i.HasExpired))
                lootInstance.SendLootNotify(looter);
        }

        public void SendLootNotifyForVisibleOwner(IPlayer looter, IWorldEntity owner)
        {
            if (looter == null || owner == null)
                return;

            SendLootNotify(looter, owner.Guid);
        }

        public void GiveLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId)
        {
            if (looter == null)
                return;

            LootInstance lootInstance = lootInstances.FirstOrDefault(i => i.OwnerUnitId == ownerUnitId && i.HasLootInstanceId(lootUnitId));
            if (lootInstance == null)
            {
                log.Debug($"Player {looter.CharacterId} requested unknown loot item {lootUnitId} from owner {ownerUnitId}.");
                return;
            }

            IWorldEntity owner = GetLootOwner(looter, ownerUnitId);
            if (owner == null || owner.Position.GetDistance(looter.Position) > LOOT_RANGE)
                throw new InvalidOperationException($"Owner {ownerUnitId} is unavailable or out of loot range for player {looter.CharacterId}.");

            if (lootInstance.HasExpired)
            {
                lootInstance.SendLootRemove(looter);
                return;
            }

            if (lootInstance.GiveLoot(looter, lootUnitId) && lootInstance.HasExpired)
                lootInstance.SendLootRemove(looter);
        }

        public void GiveAllLootInRange(IPlayer looter)
        {
            if (looter == null)
                return;

            foreach (LootInstance lootInstance in lootInstances.Where(i => i.HasLooter(looter.CharacterId) && !i.HasExpired).ToList())
            {
                IWorldEntity owner = GetLootOwner(looter, lootInstance.OwnerUnitId);
                if (owner == null || owner.Position.GetDistance(looter.Position) > LOOT_RANGE)
                    continue;

                foreach (LootInstanceItem item in lootInstance.Where(i => !i.Delivered).ToList())
                    lootInstance.GiveLoot(looter, item.Id);

                if (lootInstance.HasExpired)
                    lootInstance.SendLootRemove(looter);
            }
        }

        public void RollLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId, LootRollAction action)
        {
            if (looter == null)
                return;

            LootInstance lootInstance = lootInstances.FirstOrDefault(i => i.OwnerUnitId == ownerUnitId && i.HasLootInstanceId(lootUnitId));
            if (lootInstance == null)
            {
                log.Debug($"Player {looter.CharacterId} requested unknown loot roll {lootUnitId} from owner {ownerUnitId}.");
                return;
            }

            IWorldEntity owner = GetLootOwner(looter, ownerUnitId);
            if (owner == null || owner.Position.GetDistance(looter.Position) > LOOT_RANGE)
                throw new InvalidOperationException($"Owner {ownerUnitId} is unavailable or out of loot range for player {looter.CharacterId}.");

            if (lootInstance.HasExpired)
            {
                lootInstance.SendLootRemove(looter);
                return;
            }

            lootInstance.RollLoot(looter, lootUnitId, action);
        }

        public void AssignMasterLoot(IPlayer master, uint ownerUnitId, uint lootUnitId, Identity assignee)
        {
            if (master == null || assignee == null)
                return;

            LootInstance lootInstance = lootInstances.FirstOrDefault(i => i.OwnerUnitId == ownerUnitId && i.HasLootInstanceId(lootUnitId));
            if (lootInstance == null)
            {
                log.Debug($"Player {master.CharacterId} requested unknown master-loot assignment {lootUnitId} from owner {ownerUnitId}.");
                return;
            }

            IWorldEntity owner = GetLootOwner(master, ownerUnitId);
            if (owner == null || owner.Position.GetDistance(master.Position) > LOOT_RANGE)
                throw new InvalidOperationException($"Owner {ownerUnitId} is unavailable or out of loot range for player {master.CharacterId}.");

            if (lootInstance.HasExpired)
            {
                lootInstance.SendLootRemove(master);
                return;
            }

            lootInstance.AssignMasterLoot(master, lootUnitId, assignee);
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

        public void GiveGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, uint ownerUnitId, bool sendGrantedNotify = false)
        {
            if (looter == null)
                return;

            ArgumentNullException.ThrowIfNull(items);

            foreach (GeneratedLootItem item in items)
                GiveImmediateLoot(looter, item.Type, item.StaticId, item.Count, ownerUnitId, sendGrantedNotify);
        }

        private static void GiveImmediateLoot(IPlayer looter, LootItemType type, uint staticId, uint count, uint ownerUnitId, bool sendGrantedNotify = false)
        {
            if (looter == null || count == 0u)
                return;

            if (sendGrantedNotify)
            {
                LootInstance lootInstance = new(ownerUnitId, CreatePlayerLooterMap(looter), LooterType.Player, LootEntityType.Creature)
                {
                    Explosion = true
                };

                LootInstanceItem grantedItem = lootInstance.AddLootItem(staticId, type, count);
                grantedItem.SetWinner(looter);
                if (grantedItem.DeliverItem(looter))
                    lootInstance.SendLootNotify(looter, includeGrantedItems: true);

                return;
            }

            LootInstanceItem item = new(staticId, type, count);
            item.SetOwnerUnit(ownerUnitId);
            item.SetWinner(looter);
            item.DeliverItem(looter);
        }

        private static bool CanDeliverLootItem(GeneratedLootItem item)
        {
            return item.Type switch
            {
                LootItemType.AccountCurrency => IsDefinedAccountCurrency(item.StaticId),
                LootItemType.AccountItem     => GameTableManager.Instance.AccountItem.GetEntry(item.StaticId) != null,
                LootItemType.Cash            => IsDefinedCharacterCurrency(item.StaticId),
                LootItemType.StaticItem      => ItemManager.Instance.GetItemInfo(item.StaticId) != null,
                LootItemType.VirtualItem     => GameTableManager.Instance.VirtualItem.GetEntry(item.StaticId) != null,
                _                            => false
            };
        }

        private static bool IsDefinedAccountCurrency(uint staticId)
        {
            return staticId <= int.MaxValue
                && Enum.IsDefined(typeof(AccountCurrencyType), (int)staticId)
                && GameTableManager.Instance.AccountCurrencyType.GetEntry(staticId) != null;
        }

        private static bool IsDefinedCharacterCurrency(uint staticId)
        {
            return staticId <= int.MaxValue && Enum.IsDefined(typeof(CurrencyType), (int)staticId);
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
