using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: Powering Down — power down regulators, play cinematic.
    /// Quest 5573 has branched retail follow-ups; do not force a direct grant.
    /// </summary>
    [ScriptFilterOwnerId(5573u)]
    public class Q5573PoweringDownQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint QObjPowerRegulators   = 8229u;
        private const uint QObjCinematicComplete = 12870u;

        private IQuest owner;
        private readonly ICinematicFactory cinematicFactory;

        public Q5573PoweringDownQuestScript(ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            if (objective.ObjectiveInfo.Id != QObjPowerRegulators)
                return;

            if (objective.IsComplete() && owner.State != QuestState.Achieved)
            {
                owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ5573PoweringDownCinematic>());
                owner.ObjectiveUpdate(QObjCinematicComplete, 1u);
            }
        }
    }
}
