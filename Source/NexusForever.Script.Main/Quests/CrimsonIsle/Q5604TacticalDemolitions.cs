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
    /// Quest2 makes Q5580 and Q5583 available on completion.
    /// Retail chain: ... -> 5597 -> 5604 -> 5580/5583 -> [merge gate 5594].
    /// </summary>
    [ScriptFilterOwnerId(5604u)]
    public class Q5604TacticalDemolitionsQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint QObjExileCannons      = 8268u;
        private const uint QObjCinematicComplete = 15918u;

        private IQuest owner;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ILogger<Q5604TacticalDemolitionsQuestScript> log;
        private bool completionCinematicQueued;

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

            if (!objective.IsComplete())
                return;

            if (owner.State == QuestState.Achieved || completionCinematicQueued)
                return;

            completionCinematicQueued = true;

            // WIP/GUESSED: Questing-and-more advances the cinematic-complete objective immediately; exact retail cinematic timing is not live-smoked.
            owner.Player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ5604TacticalDemolitionsCinematic>());
            owner.ObjectiveUpdate(QObjCinematicComplete, 1u);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Completed)
            {
                CrimsonIsleQuestChain.GrantQuestsIfMissing(
                    owner,
                    globalQuestManager,
                    log,
                    CrimsonIsleQuestChain.Q5580EnforcedRadioSilence,
                    CrimsonIsleQuestChain.Q5583HeavyArmor);
            }
        }
    }
}
