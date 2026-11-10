using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Credits quest objective types 27 and 28, whose <c>Data</c> is a PathSoldierActivate id: "activate N of this row's creatures".
    /// </summary>
    /// <remarks>
    /// Evidence, see Decomp/Analysis/FORK_CROSSCHECK_20261110.md and PREREQUISITE_CHECK_REVIEW_20261110.md:
    /// <list type="bullet">
    /// <item>Native: the client's checklist test for these two types looks <c>Data</c> up in PathSoldierActivate (<c>PathSoldierActivate_LookupById</c> 140223380) and treats
    /// activate modes 1 and 2 as a bit mask of creature checklist indices.</item>
    /// <item>Tables: the row's <c>count</c> equals the objective count on the four type 28 objectives that resolve, and the client's own Creature2 descriptions of the row
    /// creatures name the quest and objective ("marauder cage - q4520 obj2", "ellie tashmore's marauder cage - q4520 obj3").</item>
    /// <item>World data: the 14 spawns of the q4520 obj2 cage carry checklist indices 0 to 13 against a count of 6.</item>
    /// </list>
    /// Inferred: that an activation of the row's creature is what advances the objective (the same producer rule as the Soldier path missions that share the table), and
    /// that mode 0 counts every activation. Objective 14973 (<c>Data</c> 159) has no PathSoldierActivate row and gets no credit here. No retail capture; no retail parity claim.
    /// </remarks>
    public static class SoldierActivateQuestObjectiveUpdater
    {
        private static readonly QuestObjectiveType[] Types = [QuestObjectiveType.Unknown27, QuestObjectiveType.RescuePublicEventCreatures];

        /// <summary>
        /// Return if the objective keeps a bit mask of creature checklist indices: type 27 or 28 with a PathSoldierActivate row of activate mode 1 or 2.
        /// </summary>
        public static bool IsChecklist(QuestObjectiveEntry objective, IGameTableManager tables)
        {
            if (objective == null || !Types.Contains((QuestObjectiveType)objective.Type))
                return false;

            PathSoldierActivateEntry row = tables?.PathSoldierActivate?.GetEntry(objective.Data);
            return row != null && row.SoldierActivateModeEnum is 1u or 2u;
        }

        /// <summary>
        /// Credit the activation of a creature to the activator's objectives of types 27 and 28.
        /// </summary>
        public static void OnActivate(IPlayer activator, uint creature2Id, IReadOnlyCollection<uint> targetGroupIds, byte checklistIdx, IGameTableManager tables)
        {
            if (activator?.QuestManager == null || tables?.PathSoldierActivate == null)
                return;

            targetGroupIds ??= [];
            if (creature2Id == 0u && targetGroupIds.Count == 0)
                return;

            foreach (PathSoldierActivateEntry row in tables.PathSoldierActivate.Entries)
            {
                bool matches = (row.Creature2Id != 0u && row.Creature2Id == creature2Id)
                    || (row.TargetGroupId != 0u && targetGroupIds.Contains(row.TargetGroupId));
                if (!matches)
                    continue;

                // a checklist objective takes the index of the bit to set, a counted one the amount to add
                uint update = row.SoldierActivateModeEnum is 1u or 2u ? checklistIdx : 1u;
                foreach (QuestObjectiveType type in Types)
                    activator.QuestManager.ObjectiveUpdate(type, row.Id, update);
            }
        }
    }
}
