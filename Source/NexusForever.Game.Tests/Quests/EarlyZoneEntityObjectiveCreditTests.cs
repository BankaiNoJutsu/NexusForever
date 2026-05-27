using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Script.Main.Quests.CrimsonIsle;
using NexusForever.Script.Main.Quests.EverstarGrove;
using NexusForever.Script.Main.Quests.LevianBay;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;
using Path = NexusForever.Game.Static.PlayerPath.Path;

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
    public void Q3487DominionCannon_OnActivateSuccess_WhenQuestMissing_DoesNotCreditChecklistIndex()
    {
        ICreatureEntity owner = CreateCreature(11251u, checklistIndex: 3, health: 100u, out _);
        IPlayer player = CreatePlayerWithQuestState(3487, null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        var script = new Q3487DominionCannonEntityScript();

        script.OnLoad(owner);
        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
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

    [Fact]
    public void NorthernWildsMapScript_OnEnterZone_Q3486Accepted_ShowsTowerStoryPanelAndCreditsArrival()
    {
        var script = CreateNorthernWildsMapScript(out RecordingDispatchProxy<IStoryBuilder> storyBuilderProxy);
        IPlayer player = CreatePlayerWithQuestState(3486, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterZone(player, 729u);

        RecordingDispatchProxy<IStoryBuilder>.Invocation storyPanel = Assert.Single(storyBuilderProxy.GetInvocations(nameof(IStoryBuilder.SendServerStoryPanelShow)));
        Assert.Same(player, storyPanel.Arguments[0]);
        Assert.Equal(1575u, storyPanel.Arguments[1]);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(4987u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void NorthernWildsMapScript_OnEnterZone_Q3486Missing_DoesNotCreditArrival()
    {
        var script = CreateNorthernWildsMapScript(out RecordingDispatchProxy<IStoryBuilder> storyBuilderProxy);
        IPlayer player = CreatePlayerWithQuestState(3486, null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterZone(player, 729u);

        Assert.Empty(storyBuilderProxy.GetInvocations(nameof(IStoryBuilder.SendServerStoryPanelShow)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void CrimsonIsleMapScript_OnEnterZone_Q5596Accepted_CreditsCrashSiteObjective()
    {
        var script = CreateCrimsonIsleMapScript();
        IPlayer player = CreatePlayerWithQuestState(5596, QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterZone(player, 1611u);

        RecordingDispatchProxy<IQuestManager>.Invocation update = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(8255u, update.Arguments[0]);
        Assert.Equal(1u, update.Arguments[1]);
    }

    [Fact]
    public void CrimsonIsleMapScript_OnEnterZone_Q5596Missing_DoesNotCreditCrashSiteObjective()
    {
        var script = CreateCrimsonIsleMapScript();
        IPlayer player = CreatePlayerWithQuestState(5596, null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterZone(player, 1611u);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void NorthernWildsMapScript_OnAddToMap_OpeningQuestMissing_QueuesIntroCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<INorthernWildsOnCreate>.Create(out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(cinematic);
        var script = CreateNorthernWildsMapScript(out _, cinematicFactory);
        IPlayer player = CreatePlayerWithQuestState(3480, null, out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    [Fact]
    public void CrimsonIsleMapScript_OnAddToMap_OpeningQuestMissing_QueuesIntroCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICrimsonIsleOnCreate>.Create(out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(cinematic);
        var script = CreateCrimsonIsleMapScript(cinematicFactory);
        IPlayer player = CreatePlayerWithQuestState(5593, null, out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    [Fact]
    public void EverstarGroveMapScript_OnAddToMap_OpeningQuestMissing_QueuesIntroCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<IEverstarGroveOnCreate>.Create(out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(cinematic);
        var script = new EverstarGroveMapScript(cinematicFactory);
        IPlayer player = CreatePlayerWithQuestState(6296, null, out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    [Fact]
    public void LevianBayMapScript_OnAddToMap_OpeningQuestMissing_QueuesIntroCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ILevianBayOnCreate>.Create(out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(cinematic);
        var script = new LevianBayMapScript(cinematicFactory);
        IPlayer player = CreatePlayerWithQuestState(6780, null, out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    [Theory]
    [InlineData("NorthernWilds", 3480)]
    [InlineData("CrimsonIsle", 5593)]
    [InlineData("EverstarGrove", 6296)]
    [InlineData("LevianBay", 6780)]
    public void SurfaceStarterMapScript_OnAddToMap_OpeningQuestKnown_DoesNotQueueIntroCinematic(string map, ushort questId)
    {
        ICinematicFactory cinematicFactory = CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _));
        IMapScript script = map switch
        {
            "NorthernWilds" => CreateNorthernWildsMapScript(out _, cinematicFactory),
            "CrimsonIsle" => CreateCrimsonIsleMapScript(cinematicFactory),
            "EverstarGrove" => new EverstarGroveMapScript(cinematicFactory),
            "LevianBay" => new LevianBayMapScript(cinematicFactory),
            _ => throw new ArgumentOutOfRangeException(nameof(map), map, null)
        };
        IPlayer player = CreatePlayerWithQuestState(questId, QuestState.Accepted, out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        Assert.Empty(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
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
        return CreatePlayerWithQuestState(questId, questState, out questManagerProxy, out _);
    }

    private static IPlayer CreatePlayerWithQuestState(
        ushort questId,
        QuestState? questState,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == questId ? questState : null);

        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        playerProxy.SetProperty(nameof(IPlayer.Path), (Path)byte.MaxValue);
        return player;
    }

    private static NorthernWildsMapScript CreateNorthernWildsMapScript(
        out RecordingDispatchProxy<IStoryBuilder> storyBuilderProxy,
        ICinematicFactory cinematicFactory = null)
    {
        cinematicFactory ??= CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _));
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out storyBuilderProxy);

        return new NorthernWildsMapScript(
            NullLogger<NorthernWildsMapScript>.Instance,
            entityFactory,
            gameTableManager,
            cinematicFactory,
            storyBuilder);
    }

    private static CrimsonIsleMapScript CreateCrimsonIsleMapScript(ICinematicFactory cinematicFactory = null)
    {
        cinematicFactory ??= CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _));
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        return new CrimsonIsleMapScript(
            NullLogger<CrimsonIsleMapScript>.Instance,
            entityFactory,
            gameTableManager,
            cinematicFactory);
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }
}
