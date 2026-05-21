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
    /// Exile holo-terminal quest.
    /// Quest 10540 → grants 10519 on completion.
    /// Objectives: ActivateEntity at terminal (WL 51709, 160m radius), EnterZone 4964.
    /// </summary>
    [ScriptFilterOwnerId(10540u)]
    public class Q10540HoloTerminalQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 10519;

        private readonly ILogger<Q10540HoloTerminalQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q10540HoloTerminalQuestScript(
            ILogger<Q10540HoloTerminalQuestScript> log,
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
