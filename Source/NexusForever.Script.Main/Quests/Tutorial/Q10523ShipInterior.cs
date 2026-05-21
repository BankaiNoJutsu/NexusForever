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
    /// Dominion ship interior exploration quest.
    /// Quest 10523 → grants 10530 on completion.
    /// Objectives: checklist, kill creature 73499, reach cryopod zone.
    /// </summary>
    [ScriptFilterOwnerId(10523u)]
    public class Q10523ShipInteriorQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 10530;

        private readonly ILogger<Q10523ShipInteriorQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q10523ShipInteriorQuestScript(
            ILogger<Q10523ShipInteriorQuestScript> log,
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
