using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3886 (Fiery Distraction) - grants 3673 (Contact with Thayd) on completion.
    /// Objectives: SucceedCSI on creature 13630, ActivateTargetGroupChecklist tg=1460 x3.
    /// </summary>
    [ScriptFilterOwnerId(3886u)]
    public class Q3886FieryDistractionQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 3673;
        private readonly ILogger<Q3886FieryDistractionQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q3886FieryDistractionQuestScript(ILogger<Q3886FieryDistractionQuestScript> log, IGlobalQuestManager globalQuestManager)
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
