using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Quests;

public class InventoryQuestObjectiveUpdaterTests
{
    [Fact]
    public void RefreshCollectItemForItem_NoActiveQuests_DoesNothing()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        questProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItemCount), 5u);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        InventoryQuestObjectiveUpdater.RefreshCollectItemForItem(player, 2919u);

        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void OnPublicEventObjectiveSucceeded_CreditsEventLinkedObjectiveTypes()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        PublicEventQuestObjectiveUpdater.OnPublicEventObjectiveSucceeded(player, 5281u);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.CompleteEvent, 5281u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.Unknown31, 5281u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.CraftSchematic, 5281u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.Unknown20, 5281u, 1u);
        AssertObjectiveUpdate(questProxy, QuestObjectiveType.CombatMomentum, 5281u, 1u);
    }

    [Fact]
    public void OnCurrencyAdded_CreditsEarnCurrencyObjective()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        CurrencyQuestObjectiveUpdater.OnCurrencyAdded(player, Game.Static.Entity.CurrencyType.Prestige, 25);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.EarnCurrency, 5u, 25u);
    }

    [Fact]
    public void OnMatchEntered_CreditsParticipateInGroupContent()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        MatchingQuestObjectiveUpdater.OnMatchEntered(player, 2u);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.ParticipateInGroupContent, 2u, 1u);
    }

    [Fact]
    public void OnSchematicCrafted_CreditsCraftSchematicObjective()
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        CraftingQuestObjectiveUpdater.OnSchematicCrafted(player, 5599u, 2u);

        AssertObjectiveUpdate(questProxy, QuestObjectiveType.CraftSchematic, 5599u, 2u);
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
