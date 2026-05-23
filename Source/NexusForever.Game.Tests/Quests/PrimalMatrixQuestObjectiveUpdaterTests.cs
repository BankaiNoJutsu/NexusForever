using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quests;

public class PrimalMatrixQuestObjectiveUpdaterTests
{
    [Fact]
    public void OnPrimalEssenceAccountCurrencyGranted_CreditsBeginMatrix()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        PrimalMatrixQuestObjectiveUpdater.OnPrimalEssenceAccountCurrencyGranted(player, AccountCurrencyType.CrimsonEssence, 10ul);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.BeginMatrix, 0u, 1u);
    }

    [Fact]
    public void OnPrimalEssenceAccountCurrencyGranted_IgnoresNonEssenceCurrency()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        PrimalMatrixQuestObjectiveUpdater.OnPrimalEssenceAccountCurrencyGranted(player, AccountCurrencyType.Omnibit, 10ul);

        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void SyncBeginMatrixForQuest_CreditsWhenQuestHasObjectiveAndAccountHasEssence()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        IAccountCurrencyManager currencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out RecordingDispatchProxy<IAccountCurrencyManager> currencyProxy);
        currencyProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), true);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currencyManager);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);

        IQuestObjective objective = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> objectiveProxy);
        objectiveProxy.SetMethodReturn(nameof(IQuestObjective.IsComplete), false);
        objectiveProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), new QuestObjectiveInfo(new QuestObjectiveEntry
        {
            Id = 21491u,
            Type = (uint)QuestObjectiveType.BeginMatrix,
            Data = 0u,
            Count = 1u,
        }));

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Player), player);
        questProxy.SetMethodReturn(nameof(IQuest.GetEnumerator), new List<IQuestObjective> { objective }.GetEnumerator());

        PrimalMatrixQuestObjectiveUpdater.SyncBeginMatrixForQuest(quest);

        AssertObjectiveUpdate(questManagerProxy, QuestObjectiveType.BeginMatrix, 0u, 1u);
    }

    private static void AssertObjectiveUpdate(
        RecordingDispatchProxy<IQuestManager> questProxy,
        QuestObjectiveType type,
        uint data,
        uint progress)
    {
        Assert.Contains(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)), i =>
            i.Arguments.Length == 3
            && (QuestObjectiveType)i.Arguments[0] == type
            && (uint)i.Arguments[1] == data
            && (uint)i.Arguments[2] == progress);
    }
}
