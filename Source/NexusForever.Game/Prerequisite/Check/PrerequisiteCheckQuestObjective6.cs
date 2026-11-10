using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 94 (enum name <c>QuestObjective6</c>): evaluator case <c>0x5e</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x410</c>
    /// (<c>14049f5b0</c>) with the unit, the comparison, <b>ObjectId</b> (a quest id), <b>Value</b> (an objective index) and the second unit.
    /// The creature is the unit itself when it is not a Player or Ghost, otherwise the second unit when that is not a Player or Ghost. With no such creature, or no quest definition for ObjectId,
    /// only NotEqual is met. Otherwise the flag is: the quest is in the active quest map (<c>140552f50</c>), the objective at index Value exists and is of a checklist kind
    /// (<c>1405517a0</c>: objective types 10, 13, 14 and 18, or 27 and 28 when their settler mission has mode 1 or 2), the checklist index of the creature (<c>unit+0x35f8</c>) is below 32, and
    /// that bit is set in the progress word of the objective (<c>140551730</c>, objective virtual <c>+0x40</c>). Equal returns the flag, NotEqual its negation, any other comparison false.
    /// </summary>
    /// <remarks>
    /// Read as "this creature has already been ticked off in the checklist objective". Of the 722 retail slots whose quest and index resolve, 620 point at objective types 14
    /// (ActivateTargetGroupChecklist, 564) and 10 (ScriptedTargetGroupChecklist, 56); they sit in creature visibility, activate-spell and animation-state prerequisites.
    /// <c>unit+0x35f8</c> is written when a unit is created and is the <c>QuestChecklistIdx</c> of the entity, the reading type 107 already uses for path missions.
    /// The previous implementation tested whether the objective was complete (the type 68 rule), which is true for every creature of a finished checklist and false for a ticked creature of an
    /// unfinished one, and counted a Completed quest as complete where the native test requires the quest to be active.
    /// Not modelled: objective types 13 and 18 and the settler types 27 and 28 are bit-tested natively, but the server keeps a count, not a bit mask, for them (see QuestObjective.IsChecklist),
    /// so they answer as "bit not set" here (16 retail slots).
    /// Status: Implemented from the native bodies; the meaning is inferred from the objective types. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.QuestObjective6)]
    public class PrerequisiteCheckQuestObjective6 : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckQuestObjective6(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity second = parameters?.Target;

            IWorldEntity creature;
            IPlayer player;
            if (subject != null && !IsPlayerLike(subject))
            {
                creature = subject;
                player   = second as IPlayer;
            }
            else
            {
                creature = second != null && !IsPlayerLike(second) ? second : null;
                player   = subject as IPlayer;
            }

            if (creature == null || gameTableManager?.Quest2?.GetEntry(objectId) == null)
                return comparison == PrerequisiteComparison.NotEqual;

            return PrerequisiteCompare.CompareFlag(comparison, IsChecklistBitSet(player, objectId, value, creature.QuestChecklistIdx));
        }

        /// <summary>
        /// Native <c>140551730</c>: the bit of the checklist index in the progress word of the checklist objective at the index, for an active quest.
        /// </summary>
        internal static bool IsChecklistBitSet(IPlayer player, uint questId, uint objectiveIndex, byte checklistIdx)
        {
            if (player?.QuestManager == null || checklistIdx >= 32)
                return false;

            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                if (quest.Id != questId)
                    continue;

                foreach (IQuestObjective objective in quest)
                {
                    if (objective.Index != objectiveIndex)
                        continue;

                    // types 10, 13, 14 and 18, and 27 and 28 for activate modes 1 and 2
                    if (!QuestObjective.IsChecklistObjective(objective.ObjectiveInfo))
                        return false;

                    return (objective.Progress & (1u << checklistIdx)) != 0u;
                }

                return false;
            }

            return false;
        }

        private static bool IsPlayerLike(IWorldEntity entity)
            => entity.Type is EntityType.Player or EntityType.Ghost;
    }
}
