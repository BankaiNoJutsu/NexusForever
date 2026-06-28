using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3487 (Shellshock!) -> grants 3963 (More Important Than Revenge) on completion.
    /// </summary>
    [ScriptFilterOwnerId(3487u)]
    public class Q3487ShellshockQuestScript : FollowUpQuestScript<Q3487ShellshockQuestScript>
    {
        protected override ushort NextQuestId => 3963;

        public Q3487ShellshockQuestScript(
            ILogger<Q3487ShellshockQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }

    [ScriptFilterCreatureId(11251u)]
    public class Q3487DominionCannonEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestShellshock = 3487;
        private const uint OverloadDominionCannonsObjective = 4489;
        private const uint OverloadDominionCannonsChecklistCount = 4;
        private const uint ChecklistBitCount = 32;

        private ICreatureEntity owner;
        private readonly HashSet<ulong> creditedCharacters = [];

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            // WIP/GUESSED: Questing-and-more proves the checklist credit, but the Accepted-state gate is emulator-side safety pending retail activation smoke.
            if (activator.QuestManager.GetQuestState(QuestShellshock) != QuestState.Accepted)
                return;

            if (!creditedCharacters.Add(activator.CharacterId))
                return;

            uint checklistIndex = ResolveChecklistIndex(activator);
            if (checklistIndex >= ChecklistBitCount)
            {
                creditedCharacters.Remove(activator.CharacterId);
                return;
            }

            activator.QuestManager.ObjectiveUpdate(OverloadDominionCannonsObjective, checklistIndex);
        }

        private uint ResolveChecklistIndex(IPlayer activator)
        {
            IQuestObjective objective = GetOverloadDominionCannonsObjective(activator);
            uint checklistIndex = owner.QuestChecklistIdx;

            if (checklistIndex < OverloadDominionCannonsChecklistCount
                && !IsChecklistBitSet(objective, checklistIndex))
                return checklistIndex;

            return GetFirstIncompleteChecklistIndex(objective) ?? checklistIndex;
        }

        private static IQuestObjective GetOverloadDominionCannonsObjective(IPlayer activator)
        {
            IEnumerable<IQuest> activeQuests = activator.QuestManager.GetActiveQuests();
            if (activeQuests == null)
                return null;

            foreach (IQuest quest in activeQuests)
            {
                if (quest.Id != QuestShellshock)
                    continue;

                foreach (IQuestObjective objective in quest)
                {
                    if (objective.ObjectiveInfo.Id == OverloadDominionCannonsObjective)
                        return objective;
                }
            }

            return null;
        }

        private static uint? GetFirstIncompleteChecklistIndex(IQuestObjective objective)
        {
            if (objective == null)
                return null;

            for (uint index = 0; index < OverloadDominionCannonsChecklistCount; index++)
            {
                if (!IsChecklistBitSet(objective, index))
                    return index;
            }

            return null;
        }

        private static bool IsChecklistBitSet(IQuestObjective objective, uint index)
        {
            return objective != null
                && index < ChecklistBitCount
                && (objective.Progress & (1u << (int)index)) != 0u;
        }
    }

    [ScriptFilterCreatureId(12526u)]
    public class Q3487UltrabotEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort AchievementWarbot = 1296;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnKilled(IUnitEntity killer)
        {
            // WIP/GUESSED: branch grants achievement 1296 on death; duplicate-achievement handling is guarded until retail reward timing is smoke-tested.
            if (killer is IPlayer player
                && !player.AchievementManager.HasCompletedAchievement(AchievementWarbot))
                player.AchievementManager.GrantAchievement(AchievementWarbot);
        }
    }
}
