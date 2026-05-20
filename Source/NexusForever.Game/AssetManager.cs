using System.Collections.Immutable;
using System.Reflection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game
{
    public sealed class AssetManager : Singleton<AssetManager>, IAssetManager
    {
        public static ImmutableDictionary<InventoryLocation, uint> InventoryLocationCapacities { get; private set; }

        /// <summary>
        /// Id to be assigned to the next created mail.
        /// </summary>
        public ulong NextMailId => nextMailId++;

        private ulong nextMailId;

        private ImmutableDictionary<uint, ImmutableList<ItemDisplaySourceEntryEntry>> itemDisplaySourcesEntry;

        private ImmutableDictionary</*zoneId*/uint, /*tutorialId*/uint> zoneTutorials;
        private ImmutableDictionary</*creatureId*/uint, /*targetGroupIds*/ImmutableList<uint>> creatureAssociatedTargetGroups;
        private ImmutableDictionary</*questObjectiveId*/uint, /*targetIds*/ImmutableList<uint>> questObjectiveTargets;

        private ImmutableDictionary<AccountTier, ImmutableList<RewardPropertyPremiumModifierEntry>> rewardPropertiesByTier;

        public void Initialise()
        {
            nextMailId = DatabaseManager.Instance.GetDatabase<CharacterDatabase>().GetNextMailId() + 1ul;

            CacheInventoryBagCapacities();
            CacheItemDisplaySourceEntries();
            CacheTutorials();
            CacheCreatureTargetGroups();
            CacheQuestObjectiveTargetGroups();
            CacheRewardPropertiesByTier();
        }

        private void CacheInventoryBagCapacities()
        {
            var entries = new Dictionary<InventoryLocation, uint>();
            foreach (FieldInfo field in typeof(InventoryLocation).GetFields())
            {
                foreach (InventoryLocationAttribute attribute in field.GetCustomAttributes<InventoryLocationAttribute>())
                {
                    InventoryLocation location = (InventoryLocation)field.GetValue(null);
                    entries.Add(location, attribute.DefaultCapacity);
                }
            }

            InventoryLocationCapacities = entries.ToImmutableDictionary();
        }

        private void CacheItemDisplaySourceEntries()
        {
            var entries = new Dictionary<uint, List<ItemDisplaySourceEntryEntry>>();
            foreach (ItemDisplaySourceEntryEntry entry in GameTableManager.Instance.ItemDisplaySourceEntry.Entries)
            {
                if (!entries.ContainsKey(entry.ItemSourceId))
                    entries.Add(entry.ItemSourceId, new List<ItemDisplaySourceEntryEntry>());

                entries[entry.ItemSourceId].Add(entry);
            }

            itemDisplaySourcesEntry = entries.ToImmutableDictionary(e => e.Key, e => e.Value.ToImmutableList());
        }

        private void CacheTutorials()
        {
            var zoneEntries =  ImmutableDictionary.CreateBuilder<uint, uint>();
            foreach (TutorialModel tutorial in DatabaseManager.Instance.GetDatabase<WorldDatabase>().GetTutorialTriggers())
            {
                if (tutorial.TriggerId == 0) // Don't add Tutorials with no trigger ID
                    continue;

                if (tutorial.Type == 29 && !zoneEntries.ContainsKey(tutorial.TriggerId))
                    zoneEntries.Add(tutorial.TriggerId, tutorial.Id);
            }

            zoneTutorials = zoneEntries.ToImmutable();
        }

        private void CacheCreatureTargetGroups()
        {
            var entries = ImmutableDictionary.CreateBuilder<uint, List<uint>>();
            foreach (TargetGroupEntry entry in GameTableManager.Instance.TargetGroup.Entries)
            {
                if ((TargetGroupType)entry.Type != TargetGroupType.CreatureIdGroup)
                    continue;

                foreach (uint creatureId in entry.DataEntries)
                {
                    if (!entries.ContainsKey(creatureId))
                        entries.Add(creatureId, new List<uint>());

                    entries[creatureId].Add(entry.Id);
                }
            }

            creatureAssociatedTargetGroups = entries.ToImmutableDictionary(e => e.Key, e => e.Value.ToImmutableList());
        }

        private void AddToTargets(TargetGroupEntry entry, ISet<uint> targetIds, ISet<TargetGroupType> unhandledTargetGroups, ISet<uint> visitedTargetGroups)
        {
            if (entry == null || !visitedTargetGroups.Add(entry.Id))
                return;

            switch ((TargetGroupType)entry.Type)
            {
                case TargetGroupType.CreatureIdGroup:
                    foreach (uint targetId in entry.DataEntries.Where(id => id != 0u))
                        targetIds.Add(targetId);
                    break;
                case TargetGroupType.OtherTargetGroup:
                case TargetGroupType.OtherTargetGroupCreatures:
                    foreach (uint targetGroupId in entry.DataEntries.Where(id => id != 0u))
                        AddToTargets(GameTableManager.Instance.TargetGroup.GetEntry(targetGroupId), targetIds, unhandledTargetGroups, visitedTargetGroups);
                    break;
                default:
                    unhandledTargetGroups.Add((TargetGroupType)entry.Type);
                    break;
            }
        }

        private void CacheQuestObjectiveTargetGroups()
        {
            var entries = ImmutableDictionary.CreateBuilder<uint, ImmutableList<uint>>();
            var unhandledTargetGroups = new HashSet<TargetGroupType>();

            foreach (QuestObjectiveEntry questObjectiveEntry in GameTableManager.Instance.QuestObjective.Entries
                .Where(o => o.TargetGroupIdRewardPane > 0u
                    || (QuestObjectiveType)o.Type == QuestObjectiveType.ActivateTargetGroup
                    || (QuestObjectiveType)o.Type == QuestObjectiveType.ActivateTargetGroupChecklist
                    || (QuestObjectiveType)o.Type == QuestObjectiveType.KillTargetGroup
                    || (QuestObjectiveType)o.Type == QuestObjectiveType.KillTargetGroups
                    || (QuestObjectiveType)o.Type == QuestObjectiveType.TalkToTargetGroup
                    || (QuestObjectiveType)o.Type == QuestObjectiveType.ScriptedTargetGroupChecklist))
            {
                uint targetGroupId = questObjectiveEntry.Data > 0u
                    ? questObjectiveEntry.Data
                    : questObjectiveEntry.TargetGroupIdRewardPane;

                if (targetGroupId == 0u)
                    continue;

                var targetIds = new HashSet<uint>();
                AddToTargets(GameTableManager.Instance.TargetGroup.GetEntry(targetGroupId), targetIds, unhandledTargetGroups, new HashSet<uint>());
                entries[questObjectiveEntry.Id] = targetIds.Order().ToImmutableList();
            }

            questObjectiveTargets = entries.ToImmutable();
        }

        private void CacheRewardPropertiesByTier()
        {
            // VIP was intended to be used in China from what I can see, you can force the VIP premium system in the client with the China game mode parameter
            // not supported as the system was unfinished
            IEnumerable<RewardPropertyPremiumModifierEntry> hybridEntries = GameTableManager.Instance
                .RewardPropertyPremiumModifier.Entries
                .Where(e => (PremiumSystem)e.PremiumSystemEnum == PremiumSystem.Hybrid)
                .ToList();

            // base reward properties are determined by current account tier and lower if fall through flag is set
            rewardPropertiesByTier = hybridEntries
                .Select(e => e.Tier)
                .Distinct()
                .ToImmutableDictionary(k => (AccountTier)k, k => hybridEntries
                    .Where(r => r.Tier == k)
                    .Concat(hybridEntries
                        .Where(r => r.Tier < k && ((RewardPropertyPremiumModiferFlags)r.Flags & RewardPropertyPremiumModiferFlags.FallThrough) != 0))
                    .ToImmutableList());
        }

        /// <summary>
        /// Returns an <see cref="ImmutableList{T}"/> containing all <see cref="ItemDisplaySourceEntryEntry"/>'s for the supplied itemSource.
        /// </summary>
        public ImmutableList<ItemDisplaySourceEntryEntry> GetItemDisplaySource(uint itemSource)
        {
            return itemDisplaySourcesEntry.TryGetValue(itemSource, out ImmutableList<ItemDisplaySourceEntryEntry> entries) ? entries : null;
        }

        /// <summary>
        /// Returns a Tutorial ID if it's found in the Zone Tutorials cache
        /// </summary>
        public uint GetTutorialIdForZone(uint zoneId)
        {
            return zoneTutorials.TryGetValue(zoneId, out uint tutorialId) ? tutorialId : 0;
        }

        /// <summary>
        /// Returns an <see cref="ImmutableList{T}"/> containing all TargetGroup ID's associated with the creatureId.
        /// </summary>
        public ImmutableList<uint> GetTargetGroupsForCreatureId(uint creatureId)
        {
            return creatureAssociatedTargetGroups.TryGetValue(creatureId, out ImmutableList<uint> entries) ? entries : null;
        }

        /// <summary>
        /// Returns an <see cref="ImmutableList{T}"/> containing all target id's associated with the questObjectiveId.
        /// </summary>
        public ImmutableList<uint> GetQuestObjectiveTargetIds(uint questObjectiveId)
        {
            return questObjectiveTargets.TryGetValue(questObjectiveId, out ImmutableList<uint> entries) ? entries : ImmutableList<uint>.Empty;
        }

        /// <summary>
        /// Returns an <see cref="ImmutableList{T}"/> containing all <see cref="RewardPropertyPremiumModifierEntry"/> for the given <see cref="AccountTier"/>.
        /// </summary>
        public ImmutableList<RewardPropertyPremiumModifierEntry> GetRewardPropertiesForTier(AccountTier tier)
        {
            return rewardPropertiesByTier.TryGetValue(tier, out ImmutableList<RewardPropertyPremiumModifierEntry> entries) ? entries : ImmutableList<RewardPropertyPremiumModifierEntry>.Empty;
        }
    }
}
