using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.CrimsonIsle;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;

namespace NexusForever.Game.Tests.Quests;

public class EarlyZoneEntityObjectiveCreditTests
{
    [Fact]
    public void Q3667ControlPanel_OnEnterRange_WhenQuestAccepted_CreditsTerminalObjective()
    {
        ICreatureEntity owner = CreateCreature(11194u, checklistIndex: 0, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3667, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3667ControlPanelEntityScript();

        script.OnLoad(owner);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(4770u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3667ControlPanel_OnEnterRange_WhenQuestMissing_DoesNotCreditObjective()
    {
        ICreatureEntity owner = CreateCreature(11194u, checklistIndex: 0, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3667, null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3667ControlPanelEntityScript();

        script.OnLoad(owner);
        script.OnEnterRange(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q3487DominionCannon_OnActivateSuccess_WhenQuestAccepted_CreditsChecklistIndex()
    {
        ICreatureEntity owner = CreateCreature(11251u, checklistIndex: 3, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3487, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3487DominionCannonEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.ScriptedTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(11251u, update.Arguments[1]);
        Assert.Equal(3u, update.Arguments[2]);
    }

    [Fact]
    public void Q3741SupplyCrate_OnActivateSuccess_WhenQuestAccepted_CreditsSupplyObjective()
    {
        ICreatureEntity owner = CreateCreature(12919u, checklistIndex: 0, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3741, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3741ExileSupplyCrateEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(4813u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q3777LoftiteCrystal_OnEnterRange_WhenQuestAccepted_CreditsFragmentObjectives()
    {
        ICreatureEntity owner = CreateCreature(6987u, checklistIndex: 0, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3777, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3777LoftiteCrystalEntityScript();

        script.OnLoad(owner);
        script.OnEnterRange(player);

        List<RecordingDispatchProxy<IQuestManager>.Invocation> updates = questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).ToList();
        Assert.Equal(2, updates.Count);
        Assert.Equal(5076u, updates[0].Arguments[0]);
        Assert.Equal(1u, updates[0].Arguments[1]);
        Assert.Equal(QuestObjectiveType.CollectItem, updates[1].Arguments[0]);
        Assert.Equal(6998u, updates[1].Arguments[1]);
        Assert.Equal(1u, updates[1].Arguments[2]);
    }

    [Fact]
    public void Q3781CaptiveSoldier_OnActivateSuccess_WhenQuestAccepted_CreditsRescueObjective()
    {
        ICreatureEntity owner = CreateCreature(12537u, checklistIndex: 0, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3781, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3781CaptiveExileSoldierEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(4880u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void Q8855DominionSoldier_OnEnterRange_WhenQuestAccepted_CreditsActivateObjectiveAndDespawns()
    {
        ICreatureEntity owner = CreateCreature(47687u, checklistIndex: 0, health: 75u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(8855, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q8855DominionSoldiersEntityScript();

        script.OnLoad(owner);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.ActivateEntity, update.Arguments[0]);
        Assert.Equal(47687u, update.Arguments[1]);
        Assert.Equal(1u, update.Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(75u, despawn.Arguments[0]);
        Assert.Equal(DamageType.Physical, despawn.Arguments[1]);
        Assert.Null(despawn.Arguments[2]);
    }

    [Fact]
    public void Q8855DominionSoldier_OnEnterRange_WhenQuestMissing_DoesNotCreditOrDespawn()
    {
        ICreatureEntity owner = CreateCreature(47687u, checklistIndex: 0, health: 75u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(8855, null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q8855DominionSoldiersEntityScript();

        script.OnLoad(owner);
        script.OnEnterRange(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Theory]
    [InlineData(typeof(Q3667ControlPanelEntityScript), 7f)]
    [InlineData(typeof(Q3777LoftiteCrystalEntityScript), 5f)]
    [InlineData(typeof(Q8855DominionSoldiersEntityScript), 5f)]
    public void OnAddToMap_SetsExpectedInteractionRange(Type scriptType, float expectedRange)
    {
        ICreatureEntity owner = CreateCreature(11194u, checklistIndex: 0, health: 100u, out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        IWorldEntityScript script = (IWorldEntityScript)Activator.CreateInstance(scriptType);
        ((IOwnedScript<ICreatureEntity>)script).OnLoad(owner);
        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(expectedRange, range.Arguments[0]);
    }

    private static ICreatureEntity CreateCreature(
        uint creatureId,
        byte checklistIndex,
        uint health,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.CreatureId), creatureId);
        ownerProxy.SetProperty(nameof(ICreatureEntity.QuestChecklistIdx), checklistIndex);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Health), health);
        return owner;
    }

    private static IPlayer CreatePlayerWithQuestState(
        ushort questId,
        QuestState? questState,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == questId ? questState : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
    }
}
