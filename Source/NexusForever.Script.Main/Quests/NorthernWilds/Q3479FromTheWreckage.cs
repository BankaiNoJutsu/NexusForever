using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3479 (From the Wreckage) - Exile faction start.
    /// Mutually exclusive with Q3480 (Reporting for Duty).
    /// Grants 3667 (The Tower) on completion.
    /// Objectives: SucceedCSI x3 (creature 11070), KillTargetGroup x8 (tg 7288).
    /// </summary>
    [ScriptFilterOwnerId(3479u)]
    public class Q3479FromTheWreckageQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 3667;
        private readonly ILogger<Q3479FromTheWreckageQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q3479FromTheWreckageQuestScript(ILogger<Q3479FromTheWreckageQuestScript> log, IGlobalQuestManager globalQuestManager)
        {
            this.log = log; this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner) { this.owner = owner; }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Completed)
                GrantNext();
        }

        private void GrantNext()
        {
            if (owner.Player.QuestManager.GetQuestState(NextQuestId) != null) return;
            IQuestInfo info = globalQuestManager.GetQuestInfo(NextQuestId);
            if (info == null) { log.LogWarning("Next quest {NextId} info missing.", NextQuestId); return; }
            owner.Player.QuestManager.QuestAdd(info);
            log.LogDebug("Granted follow-up quest {NextId} after {QuestId}.", NextQuestId, owner.Id);
        }
    }
}
