using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: Tactical Demolitions — destroy Exile cannons, play cinematic.
    /// Quest 5604 has multiple retail follow-ups; do not force a direct grant.
    /// </summary>
    [ScriptFilterOwnerId(5604u)]
    public class Q5604TacticalDemolitionsQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint QObjExileCannons      = 8268u;
        private const uint QObjCinematicComplete = 15918u;

        private IQuest owner;
        private readonly ICinematicFactory cinematicFactory;

        public Q5604TacticalDemolitionsQuestScript(ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            if (objective.ObjectiveInfo.Id != QObjExileCannons)
                return;

            if (objective.IsComplete() && owner.State != QuestState.Achieved)
            {
                owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ5604TacticalDemolitionsCinematic>());
                owner.ObjectiveUpdate(QObjCinematicComplete, 1u);
            }
        }
    }
}
