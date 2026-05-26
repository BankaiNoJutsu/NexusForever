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
    /// Crimson Isle: Powering Down; power down regulators and play cinematic.
    /// Quest 5573: granted by Q5593. Quest2 preq_flags=1 lets either Q5573 or Q5575 feed Q5596.
    /// Retail chain: 5595 (root) -> 5575
    ///            5593 (root) -> 5573 -> [either-branch 5596] -> 5597 -> 5604 -> 5580/5583 -> [merge 5594].
    /// </summary>
    [ScriptFilterOwnerId(5573u)]
    public class Q5573PoweringDownQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint QObjPowerRegulators   = 8229u;
        private const uint QObjCinematicComplete = 12870u;

        private IQuest owner;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ILogger<Q5573PoweringDownQuestScript> log;

        public Q5573PoweringDownQuestScript(
            ICinematicFactory cinematicFactory,
            IGlobalQuestManager globalQuestManager,
            ILogger<Q5573PoweringDownQuestScript> log)
        {
            this.cinematicFactory   = cinematicFactory;
            this.globalQuestManager = globalQuestManager;
            this.log                = log;
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

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState == QuestState.Completed)
                CrimsonIsleQuestChain.GrantQuestsIfMissing(owner, globalQuestManager, log, CrimsonIsleQuestChain.Q5596OrdnanceRecovery);
        }
    }
}
