using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle branch root: Quest2 makes Q5573 and Q8855 available after Q5593.
    /// </summary>
    [ScriptFilterOwnerId(5593u)]
    public class Q5593QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5593QuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q5593QuestScript(
            ILogger<Q5593QuestScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log                = log;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState != QuestState.Completed)
                return;

            CrimsonIsleQuestChain.GrantQuestsIfMissing(
                owner,
                globalQuestManager,
                log,
                CrimsonIsleQuestChain.Q5573PoweringDown,
                CrimsonIsleQuestChain.Q8855StasisInterrupted);
        }
    }
}
