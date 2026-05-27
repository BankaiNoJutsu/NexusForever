using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests;

namespace NexusForever.Game.Tests.Quests;

public class Q10510LearningToShopTests
{
    private const uint SmartShopperObjective = 21267u;
    private const uint SmartShopperItem = 86245u;
    private const ushort SmartShopperTitle = 400;

    [Theory]
    [InlineData(QuestState.Accepted, true, false)]
    [InlineData(QuestState.Achieved, true, false)]
    [InlineData(QuestState.Accepted, false, true)]
    public void OnQuestStateChange_WhenSmartShopperRewardAlreadyOwned_CreditsObjective(
        QuestState newState,
        bool hasItem,
        bool hasTitle)
    {
        Q10510LearningToShopQuestScript script = CreateScript(
            hasItem,
            hasTitle,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnQuestStateChange(newState, QuestState.Unknown);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(SmartShopperObjective, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void OnQuestStateChange_WhenSmartShopperRewardMissing_DoesNotCreditObjective()
    {
        Q10510LearningToShopQuestScript script = CreateScript(
            hasItem: false,
            hasTitle: false,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnQuestStateChange(QuestState.Accepted, QuestState.Unknown);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Theory]
    [InlineData(QuestState.Completed)]
    [InlineData(QuestState.Botched)]
    public void OnQuestStateChange_WhenQuestNoLongerActive_DoesNotCheckOrCreditObjective(QuestState newState)
    {
        Q10510LearningToShopQuestScript script = CreateScript(
            hasItem: true,
            hasTitle: true,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ITitleManager> titleManagerProxy);

        script.OnQuestStateChange(newState, QuestState.Accepted);

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.HasItemCount)));
        Assert.Empty(titleManagerProxy.GetInvocations(nameof(ITitleManager.HasTitle)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    private static Q10510LearningToShopQuestScript CreateScript(
        bool hasItem,
        bool hasTitle,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        return CreateScript(
            hasItem,
            hasTitle,
            out questManagerProxy,
            out _,
            out _);
    }

    private static Q10510LearningToShopQuestScript CreateScript(
        bool hasItem,
        bool hasTitle,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ITitleManager> titleManagerProxy)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodHandler(nameof(IInventory.HasItemCount), args =>
        {
            Assert.Equal(SmartShopperItem, args[0]);
            Assert.Equal(1u, args[1]);
            return hasItem;
        });

        ITitleManager titleManager = RecordingDispatchProxy<ITitleManager>.Create(out titleManagerProxy);
        titleManagerProxy.SetMethodHandler(nameof(ITitleManager.HasTitle), args =>
        {
            Assert.Equal(SmartShopperTitle, args[0]);
            return hasTitle;
        });

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.TitleManager), titleManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Player), player);

        var script = new Q10510LearningToShopQuestScript();
        script.OnLoad(quest);
        return script;
    }
}
