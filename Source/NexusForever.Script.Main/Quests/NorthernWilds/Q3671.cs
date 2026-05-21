using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: Setting Up Camp. Auto-complete quest mentioned by Q3486 Empowered Tower.
    /// Quest 3671 -> grants 3668 on completion.
    /// </summary>
    [ScriptFilterOwnerId(3671u)]
    public class Q3671QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 3668;
        private readonly ILogger<Q3671QuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q3671QuestScript(ILogger<Q3671QuestScript> log, IGlobalQuestManager globalQuestManager)
        {
            this.log = log; this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}.", owner.Id, owner.Player.CharacterId);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);
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
