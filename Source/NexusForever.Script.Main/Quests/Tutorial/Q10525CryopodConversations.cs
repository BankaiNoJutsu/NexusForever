using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile cryopod deck quest.
    /// Quest 10525 → grants 10540 on completion.
    /// Objectives: EnterZone 4965, TalkToTargetGroup 14368 & 14369, TalkTo 73663 & 73664.
    /// </summary>
    [ScriptFilterOwnerId(10525u)]
    public class Q10525CryopodConversationsQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 10540;

        private readonly ILogger<Q10525CryopodConversationsQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q10525CryopodConversationsQuestScript(
            ILogger<Q10525CryopodConversationsQuestScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log = log;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}: faction={Faction}, state={QuestState}.",
                owner.Id, owner.Player.CharacterId, owner.Player.Faction1, owner.State);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state changed for character {CharacterId}: {OldState} -> {NewState}.",
                owner.Id, owner.Player.CharacterId, oldState, newState);

            if (newState == QuestState.Completed)
                GrantNextQuest();
        }

        private void GrantNextQuest()
        {
            if (owner.Player.QuestManager.GetQuestState(NextQuestId) != null)
                return;

            IQuestInfo questInfo = globalQuestManager.GetQuestInfo(NextQuestId);
            if (questInfo == null)
            {
                log.LogWarning("Next quest {NextQuestId} info missing for character {CharacterId}.",
                    NextQuestId, owner.Player.CharacterId);
                return;
            }

            owner.Player.QuestManager.QuestAdd(questInfo);
            log.LogDebug("Granted follow-up quest {NextQuestId} to character {CharacterId} after completing {QuestId}.",
                NextQuestId, owner.Player.CharacterId, owner.Id);
        }
    }
}
