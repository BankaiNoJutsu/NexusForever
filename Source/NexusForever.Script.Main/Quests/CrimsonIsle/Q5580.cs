using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: activate target group checklist at two locations.
    /// Quest 5594 requires both 5580 and 5583, so completion must not force-grant it.
    /// </summary>
    [ScriptFilterOwnerId(5580u)]
    public class Q5580QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5580QuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q5580QuestScript(
            ILogger<Q5580QuestScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log                = log;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner) { this.owner = owner; }
        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState == QuestState.Completed)
            {
                CrimsonIsleQuestChain.GrantQuestIfPrerequisitesComplete(
                    owner,
                    globalQuestManager,
                    log,
                    CrimsonIsleQuestChain.Q5594LastResistance,
                    CrimsonIsleQuestChain.Q5580EnforcedRadioSilence,
                    CrimsonIsleQuestChain.Q5583HeavyArmor);
            }
        }
    }
}
