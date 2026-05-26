using NexusForever.Game.Abstract.Quest;

namespace NexusForever.Script.Main.Tutorial
{
    internal sealed class TutorialCommunicatorSequence
    {
        private readonly IQuest owner;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly HashSet<uint> sentMessages = [];

        public TutorialCommunicatorSequence(IQuest owner, IGlobalQuestManager globalQuestManager)
        {
            this.owner              = owner;
            this.globalQuestManager = globalQuestManager;
        }

        public void TrySendWhenComplete(IQuestObjective objective, uint objectiveId, params uint[] communicatorMessageIds)
        {
            if (objective.ObjectiveInfo.Id != objectiveId || !objective.IsComplete())
                return;

            foreach (uint communicatorMessageId in communicatorMessageIds)
                TrySend(communicatorMessageId);
        }

        public void TrySend(uint communicatorMessageId)
        {
            if (sentMessages.Contains(communicatorMessageId))
                return;

            ICommunicatorMessage message = globalQuestManager.GetCommunicatorMessage(communicatorMessageId);
            if (message == null)
                return;

            sentMessages.Add(communicatorMessageId);
            message.Send(owner.Player.Session);
        }
    }
}
