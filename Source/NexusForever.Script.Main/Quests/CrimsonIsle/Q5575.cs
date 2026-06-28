using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: enter zone, activate checklist, trigger cinematic.
    /// Quest2 preq_flags=1 lets either Q5575 or Q5573 feed Q5596.
    /// </summary>
    [ScriptFilterOwnerId(5575u)]
    public class Q5575QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint QObjPowerRegulators = 8371u;
        private const uint QObjPowerRegulatorsComplete = 12871u;

        private readonly ILogger<Q5575QuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;
        private bool powerRegulatorsCompleteCredited;

        public Q5575QuestScript(
            ILogger<Q5575QuestScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log                = log;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner) { this.owner = owner; }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            if (objective.ObjectiveInfo.Id != QObjPowerRegulators)
                return;

            if (!objective.IsComplete())
                return;

            if (owner.State == QuestState.Achieved || powerRegulatorsCompleteCredited)
                return;

            powerRegulatorsCompleteCredited = true;
            owner.ObjectiveUpdate(QObjPowerRegulatorsComplete, 1u);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState == QuestState.Completed)
                CrimsonIsleQuestChain.GrantQuestsIfMissing(owner, globalQuestManager, log, CrimsonIsleQuestChain.Q5596OrdnanceRecovery);
        }
    }
}
