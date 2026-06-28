using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.Galeras;
using NexusForever.Script.Main.Quests.NorthernWilds;

namespace NexusForever.Game.Tests.Quests;

public class GalerasQuestScriptTests
{
    private const ushort QuestByLeapsAndBounds = 3777;
    private const ushort QuestHoldTheLine = 4694;
    private const ushort QuestLeavingTheTempleOfOsiric = 4696;
    private const uint ByLeapsAndBoundsFirstFragmentActivateEntity = 6952u;
    private const uint PureLoftiteFragmentItem = 6998u;
    private const uint LegacyLoftiteCrystal = 6987u;
    private const uint ByLeapsAndBoundsLoftiteCrystal = 13120u;
    private const uint HoldoutEventObjectiveData = 111u;
    private const uint FlamewingAdvanceScout = 17189u;
    private const uint StormwingVanquisher = 19595u;

    [Fact]
    public void Q4694StormwingStriker_OnKilled_WhenQuestAccepted_CreditsHoldoutCompleteEvent()
    {
        var script = new Q4694HoldTheLineStormwingStrikerEntityScript();
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnKilled(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.CompleteEvent, update.Arguments[0]);
        Assert.Equal(HoldoutEventObjectiveData, update.Arguments[1]);
        Assert.Equal(1u, update.Arguments[2]);
    }

    [Fact]
    public void Q4694StormwingStriker_OnKilled_WhenRepeated_CreditsHoldoutCompleteEventOnce()
    {
        var script = new Q4694HoldTheLineStormwingStrikerEntityScript();
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnKilled(player);
        script.OnKilled(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.CompleteEvent, update.Arguments[0]);
        Assert.Equal(HoldoutEventObjectiveData, update.Arguments[1]);
        Assert.Equal(1u, update.Arguments[2]);
    }

    [Fact]
    public void Q4694StormwingStriker_OnKilled_WhenQuestMissing_DoesNotCreditHoldout()
    {
        var script = new Q4694HoldTheLineStormwingStrikerEntityScript();
        IPlayer player = CreatePlayerWithQuestState(null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnKilled(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q4694StormwingStriker_OnKilled_WhenKillerIsNotPlayer_DoesNothing()
    {
        var script = new Q4694HoldTheLineStormwingStrikerEntityScript();
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnKilled(creature);
    }

    [Theory]
    [InlineData(LegacyLoftiteCrystal)]
    [InlineData(ByLeapsAndBoundsLoftiteCrystal)]
    public void Q3777LoftiteCrystal_OnEnterRange_WhenQuestAccepted_CreditsFirstFragmentAndCollectItem(uint creatureId)
    {
        var script = new Q3777LoftiteCrystalEntityScript();
        ICreatureEntity owner = CreateCreature(creatureId, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(
            QuestByLeapsAndBounds,
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(QuestObjectiveType.ActivateEntity, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(ByLeapsAndBoundsFirstFragmentActivateEntity, objectiveUpdates[0].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[2]);

        Assert.Equal(QuestObjectiveType.CollectItem, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(PureLoftiteFragmentItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(LegacyLoftiteCrystal)]
    [InlineData(ByLeapsAndBoundsLoftiteCrystal)]
    public void Q3777LoftiteCrystalCollectable_OnEnterRange_WhenQuestAccepted_CreditsFirstFragmentAndCollectItem(uint creatureId)
    {
        var script = new Q3777LoftiteCrystalCollectableEntityScript();
        ICollectableUnitEntity owner = CreateCollectableUnit(creatureId, out RecordingDispatchProxy<ICollectableUnitEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(
            QuestByLeapsAndBounds,
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(QuestObjectiveType.ActivateEntity, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(ByLeapsAndBoundsFirstFragmentActivateEntity, objectiveUpdates[0].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[2]);

        Assert.Equal(QuestObjectiveType.CollectItem, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(PureLoftiteFragmentItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Q3777LoftiteCrystal_OnEnterRange_WhenQuestMissing_DoesNotCreditObjectives()
    {
        var script = new Q3777LoftiteCrystalEntityScript();
        ICreatureEntity owner = CreateCreature(ByLeapsAndBoundsLoftiteCrystal, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(
            QuestByLeapsAndBounds,
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnEnterRange(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void Q3777LoftiteCrystal_OnEnterRange_AfterCollected_DoesNotGrantAgain()
    {
        var script = new Q3777LoftiteCrystalEntityScript();
        ICreatureEntity owner = CreateCreature(ByLeapsAndBoundsLoftiteCrystal, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(
            QuestByLeapsAndBounds,
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        Assert.Equal(2, questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Count);
        Assert.Single(ownerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(FlamewingAdvanceScout)]
    [InlineData(StormwingVanquisher)]
    public void Q4696TempleRetreatEnemy_OnKilled_WhenQuestAccepted_CreditsRetreatObjective(uint creatureId)
    {
        var script = new Q4696TempleRetreatEnemyEntityScript();
        ICreatureEntity owner = CreateCreature(creatureId);
        IPlayer player = CreatePlayerWithQuestState(
            QuestLeavingTheTempleOfOsiric,
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.ActivateEntity, update.Arguments[0]);
        Assert.Equal(creatureId, update.Arguments[1]);
        Assert.Equal(1u, update.Arguments[2]);
    }

    [Theory]
    [InlineData(FlamewingAdvanceScout)]
    [InlineData(StormwingVanquisher)]
    public void Q4696TempleRetreatEnemy_OnKilled_WhenRepeated_CreditsRetreatObjectiveOnce(uint creatureId)
    {
        var script = new Q4696TempleRetreatEnemyEntityScript();
        ICreatureEntity owner = CreateCreature(creatureId);
        IPlayer player = CreatePlayerWithQuestState(
            QuestLeavingTheTempleOfOsiric,
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(player);
        script.OnKilled(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.ActivateEntity, update.Arguments[0]);
        Assert.Equal(creatureId, update.Arguments[1]);
        Assert.Equal(1u, update.Arguments[2]);
    }

    [Fact]
    public void Q4696TempleRetreatEnemy_OnKilled_WhenQuestMissing_DoesNotCreditRetreatObjective()
    {
        var script = new Q4696TempleRetreatEnemyEntityScript();
        ICreatureEntity owner = CreateCreature(FlamewingAdvanceScout);
        IPlayer player = CreatePlayerWithQuestState(
            QuestLeavingTheTempleOfOsiric,
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q4696TempleRetreatEnemy_OnKilled_WhenKillerIsNotPlayer_DoesNothing()
    {
        var script = new Q4696TempleRetreatEnemyEntityScript();
        ICreatureEntity owner = CreateCreature(FlamewingAdvanceScout);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnLoad(owner);
        script.OnKilled(creature);
    }

    private static IPlayer CreatePlayerWithQuestState(
        QuestState? questState,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        return CreatePlayerWithQuestState(QuestHoldTheLine, questState, out questManagerProxy);
    }

    private static IPlayer CreatePlayerWithQuestState(
        ushort questId,
        QuestState? questState,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
        {
            Assert.Equal(questId, args[0]);
            return questState;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }

    private static ICreatureEntity CreateCreature(uint creatureId)
    {
        return CreateCreature(creatureId, out _);
    }

    private static ICreatureEntity CreateCreature(uint creatureId, out RecordingDispatchProxy<ICreatureEntity> creatureProxy)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out creatureProxy);
        creatureProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creatureId);
        return creature;
    }

    private static ICollectableUnitEntity CreateCollectableUnit(uint creatureId, out RecordingDispatchProxy<ICollectableUnitEntity> collectableProxy)
    {
        ICollectableUnitEntity collectable = RecordingDispatchProxy<ICollectableUnitEntity>.Create(out collectableProxy);
        collectableProxy.SetProperty(nameof(ICollectableUnitEntity.CreatureId), creatureId);
        return collectable;
    }
}
