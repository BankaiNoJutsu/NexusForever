using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: Tactical Demolitions; destroy Exile cannons and play cinematic.
    /// Quest 5604 grants 5580 on completion.
    /// Retail chain: ... -> 5597 -> 5604 -> 5580 -> [merge gate 5594].
    /// </summary>
    [ScriptFilterOwnerId(5604u)]
    public class Q5604TacticalDemolitionsQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 5580;
        private const uint QObjExileCannons      = 8268u;
        private const uint QObjCinematicComplete = 15918u;

        private IQuest owner;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ILogger<Q5604TacticalDemolitionsQuestScript> log;

        public Q5604TacticalDemolitionsQuestScript(
            ICinematicFactory cinematicFactory,
            IGlobalQuestManager globalQuestManager,
            ILogger<Q5604TacticalDemolitionsQuestScript> log)
        {
            this.cinematicFactory = cinematicFactory;
            this.globalQuestManager = globalQuestManager;
            this.log = log;
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

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Completed)
                GrantNext();
        }

        private void GrantNext()
        {
            if (owner.Player.QuestManager.GetQuestState(NextQuestId) != null)
                return;

            IQuestInfo info = globalQuestManager.GetQuestInfo(NextQuestId);
            if (info == null)
            {
                log.LogWarning("Next quest {NextId} info missing.", NextQuestId);
                return;
            }

            owner.Player.QuestManager.QuestAdd(info);
            log.LogDebug("Granted follow-up quest {NextId} after {QuestId}.", NextQuestId, owner.Id);
        }
    }
}
