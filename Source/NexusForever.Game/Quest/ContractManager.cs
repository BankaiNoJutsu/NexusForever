using System.Collections.Immutable;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Quest
{
    public class ContractManager : IContractManager
    {
        private const uint ContractFlag = 0x80000u;
        private const uint GoodContractQuality = 1u;

        private readonly IGameTableManager gameTableManager;
        private readonly Lazy<ImmutableDictionary<ushort, ContractDefinition>> contracts;

        public ContractManager(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager ?? throw new ArgumentNullException(nameof(gameTableManager));
            contracts = new Lazy<ImmutableDictionary<ushort, ContractDefinition>>(BuildContracts);
        }

        public bool CanUseReceiverlessLifecycle(IQuestInfo info)
        {
            return TryGetDefinition(info, out _);
        }

        public bool CanAccept(IQuestInfo info, IEnumerable<IQuest> activeQuests, IEnumerable<IQuest> completedQuests, DateTime utcNow)
        {
            if (!TryGetDefinition(info, out ContractDefinition definition))
                return false;

            if (!IsContractOffered(definition))
                return false;

            return !IsGroupAtCapacity(definition, activeQuests, completedQuests, utcNow);
        }

        public uint[] GetGoodQualityContractQuestIds(IEnumerable<IQuest> activeQuests, IEnumerable<IQuest> completedQuests, DateTime utcNow)
        {
            uint[] questIds = new uint[3];
            int i = 0;
            foreach (ContractDefinition definition in GetAvailableContracts(activeQuests, completedQuests, utcNow)
                .Where(c => c.PeriodicQuestGroup.ContractQualityEnum == GoodContractQuality)
                .Take(questIds.Length))
            {
                questIds[i++] = definition.QuestId;
            }

            return questIds;
        }

        public PeriodicQuestGroupEntry GetPeriodicQuestGroup(IQuestInfo info)
        {
            return TryGetDefinition(info, out ContractDefinition definition)
                ? definition.PeriodicQuestGroup
                : null;
        }

        private IEnumerable<ContractDefinition> GetAvailableContracts(IEnumerable<IQuest> activeQuests, IEnumerable<IQuest> completedQuests, DateTime utcNow)
        {
            foreach (IGrouping<uint, ContractDefinition> group in contracts.Value.Values
                .GroupBy(c => c.PeriodicQuestGroup.Id)
                .OrderBy(g => g.Key))
            {
                ContractDefinition first = group.First();
                if (IsGroupAtCapacity(first, activeQuests, completedQuests, utcNow))
                    continue;

                uint offered = first.PeriodicQuestGroup.PeriodicQuestsOffered;
                uint returned = 0u;
                foreach (ContractDefinition definition in group
                    .OrderBy(c => c.Entry.PeriodicQuestWeight == 0u ? uint.MaxValue : c.Entry.PeriodicQuestWeight)
                    .ThenBy(c => c.QuestId))
                {
                    if (returned++ >= offered)
                        break;

                    yield return definition;
                }
            }
        }

        private bool IsContractOffered(ContractDefinition definition)
        {
            uint offered = definition.PeriodicQuestGroup.PeriodicQuestsOffered;
            uint position = 0u;
            foreach (ContractDefinition offeredDefinition in contracts.Value.Values
                .Where(c => c.PeriodicQuestGroup.Id == definition.PeriodicQuestGroup.Id)
                .OrderBy(c => c.Entry.PeriodicQuestWeight == 0u ? uint.MaxValue : c.Entry.PeriodicQuestWeight)
                .ThenBy(c => c.QuestId))
            {
                if (offeredDefinition.QuestId == definition.QuestId)
                    return position < offered;

                position++;
            }

            return false;
        }

        private bool IsGroupAtCapacity(ContractDefinition definition, IEnumerable<IQuest> activeQuests, IEnumerable<IQuest> completedQuests, DateTime utcNow)
        {
            uint maxAllowed = definition.PeriodicQuestGroup.MaxPeriodicQuestsAllowed;
            if (maxAllowed == 0u)
                return true;

            uint used = CountGroupContracts(activeQuests, _ => true, definition.PeriodicQuestGroup.Id);
            used += CountGroupContracts(completedQuests, q => q.Reset > utcNow, definition.PeriodicQuestGroup.Id);

            return used >= maxAllowed;
        }

        private uint CountGroupContracts(IEnumerable<IQuest> quests, Func<IQuest, bool> include, uint periodicQuestGroupId)
        {
            uint count = 0u;
            foreach (IQuest quest in quests ?? [])
            {
                if (!include(quest))
                    continue;

                if (TryGetDefinition(quest.Info, out ContractDefinition definition)
                    && definition.PeriodicQuestGroup.Id == periodicQuestGroupId)
                    count++;
            }

            return count;
        }

        private bool TryGetDefinition(IQuestInfo info, out ContractDefinition definition)
        {
            definition = null;
            if (info == null || !info.IsContract() || info.Entry.Id > ushort.MaxValue)
                return false;

            return contracts.Value.TryGetValue((ushort)info.Entry.Id, out definition);
        }

        private ImmutableDictionary<ushort, ContractDefinition> BuildContracts()
        {
            ImmutableDictionary<ushort, ContractDefinition>.Builder builder = ImmutableDictionary.CreateBuilder<ushort, ContractDefinition>();
            foreach (Quest2Entry questEntry in gameTableManager.Quest2?.Entries ?? [])
            {
                if (!IsContractEntry(questEntry))
                    continue;

                PeriodicQuestGroupEntry periodicQuestGroup = gameTableManager.PeriodicQuestGroup?.GetEntry(questEntry.PeriodicQuestGroupId);
                if (!IsAvailablePeriodicGroup(periodicQuestGroup))
                    continue;

                if (!HasValidObjectives(questEntry) || !HasValidRewards(questEntry))
                    continue;

                builder[(ushort)questEntry.Id] = new ContractDefinition(questEntry, periodicQuestGroup);
            }

            return builder.ToImmutable();
        }

        private static bool IsContractEntry(Quest2Entry entry)
        {
            return entry != null
                && entry.Id <= ushort.MaxValue
                && (entry.Flags & ContractFlag) != 0u
                && entry.PeriodicQuestGroupId != 0u;
        }

        private static bool IsAvailablePeriodicGroup(PeriodicQuestGroupEntry entry)
        {
            return entry != null
                && entry.PeriodicQuestsOffered != 0u
                && entry.MaxPeriodicQuestsAllowed != 0u;
        }

        private bool HasValidObjectives(Quest2Entry questEntry)
        {
            bool hasObjective = false;
            foreach (uint objectiveId in (questEntry.Objectives ?? []).Where(o => o != 0u))
            {
                hasObjective = true;
                if (gameTableManager.QuestObjective?.GetEntry(objectiveId) == null)
                    return false;
            }

            return hasObjective;
        }

        private bool HasValidRewards(Quest2Entry questEntry)
        {
            Quest2RewardEntry[] rewards = (gameTableManager.Quest2Reward?.Entries ?? [])
                .Where(r => r.Quest2Id == questEntry.Id)
                .ToArray();
            if (rewards.Length == 0)
                return false;

            foreach (Quest2RewardEntry reward in rewards)
            {
                if (reward.ObjectAmount == 0u)
                    return false;

                if ((QuestRewardType)reward.Quest2RewardTypeId == QuestRewardType.Item
                    && (reward.ObjectId == 0u || gameTableManager.Item?.GetEntry(reward.ObjectId) == null))
                    return false;
            }

            return true;
        }

        private sealed class ContractDefinition
        {
            public ContractDefinition(Quest2Entry entry, PeriodicQuestGroupEntry periodicQuestGroup)
            {
                Entry              = entry;
                PeriodicQuestGroup = periodicQuestGroup;
            }

            public ushort QuestId => (ushort)Entry.Id;
            public Quest2Entry Entry { get; }
            public PeriodicQuestGroupEntry PeriodicQuestGroup { get; }
        }
    }
}
