using NexusForever.GameTable.Model;

namespace NexusForever.Game.Abstract.Quest
{
    public interface IContractManager
    {
        bool CanUseReceiverlessLifecycle(IQuestInfo info);

        bool CanAccept(IQuestInfo info, IEnumerable<IQuest> activeQuests, IEnumerable<IQuest> completedQuests, DateTime utcNow);

        uint[] GetGoodQualityContractQuestIds(IEnumerable<IQuest> activeQuests, IEnumerable<IQuest> completedQuests, DateTime utcNow);

        PeriodicQuestGroupEntry GetPeriodicQuestGroup(IQuestInfo info);
    }
}
