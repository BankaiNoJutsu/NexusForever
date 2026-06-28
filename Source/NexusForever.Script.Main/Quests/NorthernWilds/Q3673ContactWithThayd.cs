using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds finale — Contact With Thayd.
    /// Quest 3673. On Achieved, plays cinematic. On Completed, grants the retail follow-up quest 3670.
    /// </summary>
    [ScriptFilterOwnerId(3673u)]
    public class Q3673ContactWithThaydQuestScript : FollowUpQuestScript<Q3673ContactWithThaydQuestScript>
    {
        private const uint SignalFlareChecklistObjective = 4748u;
        private const uint SignalFlareOneCsiObjective = 4888u;
        private const uint SignalFlareTwoCsiObjective = 4889u;
        private const uint SignalFlareThreeCsiObjective = 4890u;
        private const uint HiddenCompletionObjective = 13391u;

        protected override ushort NextQuestId => 3670;

        private readonly ICinematicFactory cinematicFactory;
        private bool hiddenCompletionObjectiveCredited;

        public Q3673ContactWithThaydQuestScript(
            ICinematicFactory cinematicFactory,
            ILogger<Q3673ContactWithThaydQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public override void OnLoad(IQuest owner)
        {
            base.OnLoad(owner);
            TryCreditHiddenCompletionObjective();
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            if (!IsCompletionGateObjective(objective.ObjectiveInfo.Id))
                return;

            if (!objective.IsComplete())
                return;

            TryCreditHiddenCompletionObjective();
        }

        private void TryCreditHiddenCompletionObjective()
        {
            if (hiddenCompletionObjectiveCredited)
                return;

            if (Owner.State != QuestState.Accepted)
                return;

            if (!AreCompletionGateObjectivesComplete())
                return;

            // Quest2 objective 13391 is a hidden required ActivateEntity data=0 gate after the flare checklist.
            hiddenCompletionObjectiveCredited = true;
            Owner.ObjectiveUpdate(HiddenCompletionObjective, 1u);
        }

        private bool AreCompletionGateObjectivesComplete()
        {
            return IsObjectiveComplete(SignalFlareChecklistObjective)
                && IsObjectiveComplete(SignalFlareOneCsiObjective)
                && IsObjectiveComplete(SignalFlareTwoCsiObjective)
                && IsObjectiveComplete(SignalFlareThreeCsiObjective);
        }

        private bool IsObjectiveComplete(uint objectiveId)
        {
            IEnumerator<IQuestObjective> enumerator = Owner.GetEnumerator();
            if (enumerator == null)
                return false;

            using (enumerator)
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.ObjectiveInfo.Id == objectiveId)
                        return enumerator.Current.IsComplete();
                }
            }

            return false;
        }

        private static bool IsCompletionGateObjective(uint objectiveId)
        {
            return objectiveId is SignalFlareChecklistObjective
                or SignalFlareOneCsiObjective
                or SignalFlareTwoCsiObjective
                or SignalFlareThreeCsiObjective;
        }

        public override void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Achieved)
            {
                // External retail video shows this cinematic as the flare-lighting sequence resolves; exact local packet timing still needs client smoke.
                Owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3673ContactWithThaydCinematic>());
            }

            base.OnQuestStateChange(newState, oldState);
        }
    }
}
