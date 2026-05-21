using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Script.Template
{
    public abstract class FollowUpQuestScript<TScript> : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<TScript> log;
        private readonly IGlobalQuestManager globalQuestManager;

        protected IQuest Owner { get; private set; }

        protected abstract ushort NextQuestId { get; }

        protected FollowUpQuestScript(
            ILogger<TScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log                = log;
            this.globalQuestManager = globalQuestManager;
        }

        public virtual void OnLoad(IQuest owner)
        {
            Owner = owner;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}: faction={Faction}, state={QuestState}.",
                owner.Id, owner.Player.CharacterId, owner.Player.Faction1, owner.State);
        }

        public virtual void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state changed for character {CharacterId}: {OldState} -> {NewState}.",
                Owner.Id, Owner.Player.CharacterId, oldState, newState);

            if (newState == QuestState.Completed)
                GrantNextQuest();
        }

        protected virtual void GrantNextQuest()
        {
            if (Owner.Player.QuestManager.GetQuestState(NextQuestId) != null)
                return;

            IQuestInfo questInfo = globalQuestManager.GetQuestInfo(NextQuestId);
            if (questInfo == null)
            {
                log.LogWarning("Next quest {NextQuestId} info missing for character {CharacterId}.",
                    NextQuestId, Owner.Player.CharacterId);
                return;
            }

            Owner.Player.QuestManager.QuestAdd(questInfo);
            log.LogDebug("Granted follow-up quest {NextQuestId} to character {CharacterId} after completing {QuestId}.",
                NextQuestId, Owner.Player.CharacterId, Owner.Id);
        }
    }
}
