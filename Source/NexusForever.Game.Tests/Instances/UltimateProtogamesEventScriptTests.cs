using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Dungeon.UltimateProtogames;
using NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class UltimateProtogamesEventScriptTests
{
    [Fact]
    public void PublicEventObjectiveCatalog_DustStormUsesUltimateProtogamesRow2924()
    {
        Assert.Equal(2924u, (uint)PublicEventObjective.DustStorm);
    }

    private static readonly ExpectedTankRoomSpawn[] ExpectedTankRoomSpawns =
    [
        new(1100300060u, 62546u, new Vector3(-29077f, -938f, 1552f), 36577u),
        new(1100300061u, 62543u, new Vector3(-29015f, -938f, 1544f), 36579u),
        new(1100300062u, 62542u, new Vector3(-29052f, -938f, 1497f), 36578u)
    ];

    private static readonly ExpectedPrototentiaryConsoleSpawn[] ExpectedPrototentiaryConsoleSpawns =
    [
        new(
            1100300085u,
            62427u,
            new Vector3(-20819f, -942f, -7005f),
            24324u,
            1322,
            "SneakyPrisonGateConsoleEntityScript"),
        new(
            1100300081u,
            63037u,
            new Vector3(-20703f, -945f, -6926f),
            24321u,
            219,
            "SneakyPrisonCageConsoleEntityScript"),
        new(
            1100300082u,
            68916u,
            new Vector3(-20874f, -943f, -6893f),
            24321u,
            219,
            "SneakyPrisonAlarmPanelEntityScript"),
        new(
            1100300083u,
            68916u,
            new Vector3(-20844f, -943f, -6863f),
            24321u,
            219,
            "SneakyPrisonAlarmPanelEntityScript"),
        new(
            1100300084u,
            68916u,
            new Vector3(-20848f, -941f, -6971f),
            24321u,
            219,
            "SneakyPrisonAlarmPanelEntityScript")
    ];

    private static readonly ExpectedPrototentiaryDeputySpawn ExpectedDeputySpawn =
        new(
            1100300086u,
            68949u,
            new Vector3(-20745f, -945f, -6885f),
            36776u,
            1322,
            "DeputyEntityScript");

    private static readonly ExpectedPrototentiaryWardenSpawn ExpectedWardenSpawn =
        new(
            1100300092u,
            62324u,
            new Vector3(-20718f, -945f, -6923f),
            24369u,
            1322,
            "WardenEntityScript");

    private static readonly ExpectedMondosMonstrositySpawn ExpectedMondoSpawn =
        new(
            1100300087u,
            62575u,
            new Vector3(-16501f, -910f, -10991f),
            21714u,
            1322,
            "MondosMonstrosityEntityScript");

    private static readonly ExpectedMondosCrateSpawn ExpectedMondoCrateSpawn =
        new(
            1100300090u,
            62549u,
            new Vector3(-16500f, -910f, -10998f),
            28700u,
            1330,
            "MondosCrateEntityScript");

    private static readonly ExpectedRufflesSpawn ExpectedRuffles =
        new(
            1100300088u,
            65794u,
            new Vector3(-16866f, -802f, 5570f),
            23396u,
            1322,
            "RufflesEntityScript");

    private static readonly ExpectedGildedFowlSpawn ExpectedGildedFowl =
        new(
            1100300089u,
            63055u,
            new Vector3(-21242f, -807f, -10806f),
            21895u,
            1322,
            "GildedFowlEntityScript");

    private static readonly ExpectedHutHutSpawn ExpectedHutHut =
        new(
            1100300091u,
            61417u,
            new Vector3(-12480f, -788f, -6870f),
            29048u,
            1322,
            "HutHutEntityScript");

    [Fact]
    public void OnLoad_SetsWipWelcomePhase()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Welcome, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Welcome_ActivatesInitiateObjective()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithStartButton(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Welcome);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.InitiateUltimateProtogames, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Welcome_SpawnsDataMappedStartButtonPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithStartButton(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedSimple startButton);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Welcome);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.InitiateUltimateProtogames);
        AssertStartButtonModel(startButton, new Vector3(-12543f, -776f, -2766f));
        AssertGridEntityAddedToMap(mapProxy, startButton.Instance, new Vector3(-12543f, -776f, -2766f));
    }

    [Fact]
    public void UltimateProtogamesStartButton_OnActivateSuccess_UpdatesInitiateObjectiveOnce()
    {
        var script = new UltimateProtogamesStartButtonEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity entity);

        script.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.InitiateUltimateProtogames, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void UltimateProtogamesStartButtonEntityScript_UsesInstanceScriptNameFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(UltimateProtogamesStartButtonEntityScript));

        IScriptFilterSearch startButtonSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByScriptNames(["UltimateProtogamesStartButtonEntityScript"]);
        IScriptFilterSearch academyStartButtonSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByScriptNames(["ProtogamesAcademyStartButtonEntityScript"]);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(startButtonSearch, parameters));
        Assert.False(match.Match(academyStartButtonSearch, parameters));
    }

    [Fact]
    public void OnPublicEventPhase_BevORage_ActivatesMappedObjectives()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithBevORage(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.BevORage);

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatBevORage);
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.UseBevORage);
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.Caffeinated);
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.OutOfOrder);
    }

    [Fact]
    public void OnPublicEventPhase_BevORage_SpawnsReviewedBossPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithBevORage(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss bevORage);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.BevORage);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatBevORage);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.UseBevORage);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.Caffeinated);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.OutOfOrder);
        AssertBevORageModel(bevORage, new Vector3(-12737.13f, -797.0872f, 1326.347f));
        AssertGridEntityAddedToMap(mapProxy, bevORage.Instance, new Vector3(-12737.13f, -797.0872f, 1326.347f));
    }

    [Fact]
    public void OnPublicEventPhase_TankRoom_ActivatesMappedObjectivesAndSpawnsReviewedPlacements()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTank> tanks);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestructODerby);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.DestructODerby, 3u);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.TankTrample);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.CanCrusher);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RedemptionValue);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GoingGreen);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.GoingGreen, 3u);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.NoDeaths2);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.NoDeaths2, 1u);
        Assert.Equal(ExpectedTankRoomSpawns.Length, tanks.Count);
        for (int i = 0; i < ExpectedTankRoomSpawns.Length; i++)
            AssertTankRoomModel(tanks[i], ExpectedTankRoomSpawns[i]);
        AssertGridEntitiesAddedToMap(mapProxy, tanks, ExpectedTankRoomSpawns);
    }

    [Fact]
    public void OnPublicEventPhase_TankRoom_DoesNotDuplicateReviewedPlacements()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTank> tanks);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        Assert.Equal(ExpectedTankRoomSpawns.Length, tanks.Count);
        Assert.Equal(ExpectedTankRoomSpawns.Length, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TankRoomSuccess_CreditsNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GoingGreen,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.NoDeaths2, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TankRoomSuccessAfterPlayerDeath_DoesNotCreditNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnDeath(player);
        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GoingGreen,
            PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TankRoomSuccessAfterNonPlayerDeath_CreditsNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);
        IUnitEntity nonPlayer = RecordingDispatchProxy<IUnitEntity>.Create(out _);

        script.OnDeath(nonPlayer);
        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GoingGreen,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.NoDeaths2, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TankRoomFailure_DoesNotCreditNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GoingGreen,
            PublicEventStatus.Failed));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void OnTankReachedHalfHealth_AllThreeReviewedTanks_CreditsRedemptionValueOnce()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        foreach (ExpectedTankRoomSpawn spawn in ExpectedTankRoomSpawns)
            script.OnTankReachedHalfHealth(CreateTankUnit(spawn.EntityId, 50u, 100u));

        // Repeated threshold notifications must not duplicate the count-one
        // Script objective credit.
        script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[0].EntityId, 40u, 100u));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            GetObjectiveUpdates(eventProxy, PublicEventObjective.RedemptionValue));
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnTankReachedHalfHealth_DuplicateTankDoesNotReplaceDistinctTank()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        for (int i = 0; i < ExpectedTankRoomSpawns.Length; i++)
            script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[0].EntityId, 50u, 100u));

        Assert.Empty(GetObjectiveUpdates(eventProxy, PublicEventObjective.RedemptionValue));

        script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[1].EntityId, 50u, 100u));
        script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[2].EntityId, 50u, 100u));

        Assert.Single(GetObjectiveUpdates(eventProxy, PublicEventObjective.RedemptionValue));
    }

    [Fact]
    public void OnTankReachedHalfHealth_TankDestroyedBeforeAllQualify_DoesNotCreditRedemptionValue()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTankRoom(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.TankRoom);

        script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[0].EntityId, 50u, 100u));
        script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[1].EntityId, 50u, 100u));
        script.OnDeath(CreateTankUnit(ExpectedTankRoomSpawns[0].EntityId, 0u, 100u));
        script.OnTankReachedHalfHealth(CreateTankUnit(ExpectedTankRoomSpawns[2].EntityId, 50u, 100u));

        Assert.Empty(GetObjectiveUpdates(eventProxy, PublicEventObjective.RedemptionValue));
    }

    [Fact]
    public void OnPublicEventPhase_MisplacedMammoth_ActivatesMappedObjectivesAndSpawnsReviewedPlacements()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithMisplacedMammoth(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss mammoth,
            out CreatedBoss mondo,
            out CreatedBoss crate);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.MisplacedMammoth);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.QuickReflexes2);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.MonstrosityMassacre);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.QuickReflexes);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.MondosCrate);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.MondosCrate, 1u);
        AssertMisplacedMammothModel(mammoth, new Vector3(-16482f, -910f, -10996f));
        AssertMondosMonstrosityModel(mondo, ExpectedMondoSpawn);
        AssertMondosCrateModel(crate, ExpectedMondoCrateSpawn);
        AssertMisplacedMammothRoomContentAddedToMap(mapProxy, mammoth, mondo, crate);
    }

    [Fact]
    public void OnPublicEventPhase_MisplacedMammoth_DoesNotDuplicateReviewedPlacements()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithMisplacedMammoth(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss mammoth,
            out CreatedBoss mondo,
            out CreatedBoss crate);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.MisplacedMammoth);
        script.OnPublicEventPhase((uint)PublicEventPhase.MisplacedMammoth);

        Assert.Single(mammoth.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mondo.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(crate.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_Prototentiary_ActivatesMappedObjectivesAndSpawnsReviewedPlacements()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithPrototentiaryContent(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> consoles,
            out CreatedBoss deputy,
            out CreatedBoss warden);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Prototentiary);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SneakThroughThePrototentiary);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.HackThecreature62987);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.HackThecreature63037);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.Deputy);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.FastHands);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DisableTheAlarm);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.NoDeaths);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.SneakThroughThePrototentiary, 2u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.HackThecreature62987, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.HackThecreature63037, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.FastHands, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.DisableTheAlarm, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.NoDeaths, 1u);
        Assert.Equal(ExpectedPrototentiaryConsoleSpawns.Length, consoles.Count);
        for (int i = 0; i < ExpectedPrototentiaryConsoleSpawns.Length; i++)
            AssertPrototentiaryConsoleModel(consoles[i], ExpectedPrototentiaryConsoleSpawns[i]);
        AssertPrototentiaryDeputyModel(deputy, ExpectedDeputySpawn);
        AssertPrototentiaryWardenModel(warden, ExpectedWardenSpawn);
        AssertPrototentiaryContentAddedToMap(mapProxy, consoles, deputy, warden);
    }

    [Fact]
    public void OnPublicEventPhase_Prototentiary_DoesNotDuplicateReviewedPlacements()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithPrototentiaryContent(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> consoles,
            out CreatedBoss deputy,
            out CreatedBoss warden);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Prototentiary);
        script.OnPublicEventPhase((uint)PublicEventPhase.Prototentiary);

        Assert.Equal(ExpectedPrototentiaryConsoleSpawns.Length, consoles.Count);
        Assert.Single(deputy.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(warden.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(ExpectedPrototentiaryConsoleSpawns.Length + 2, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_Ruffles_ActivatesMappedObjectiveAndSpawnsReviewedPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithRuffles(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss ruffles);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Ruffles);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.HuntRuffles);
        AssertRufflesModel(ruffles, ExpectedRuffles);
        AssertGridEntityAddedToMap(mapProxy, ruffles.Instance, ExpectedRuffles.Position);
    }

    [Fact]
    public void OnPublicEventPhase_Ruffles_DoesNotDuplicateReviewedPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithRuffles(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss ruffles);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Ruffles);
        script.OnPublicEventPhase((uint)PublicEventPhase.Ruffles);

        Assert.Single(ruffles.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_PowerPlunge_ActivatesGildedFowlObjectivesAndSpawnsReviewedPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithGildedFowl(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss gildedFowl);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.PowerPlunge);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.GildedFowl);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GildedFowl2);
        AssertGildedFowlModel(gildedFowl, ExpectedGildedFowl);
        AssertGridEntityAddedToMap(mapProxy, gildedFowl.Instance, ExpectedGildedFowl.Position);
    }

    [Fact]
    public void OnPublicEventPhase_PowerPlunge_DoesNotDuplicateReviewedPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithGildedFowl(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss gildedFowl);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.PowerPlunge);
        script.OnPublicEventPhase((uint)PublicEventPhase.PowerPlunge);

        Assert.Single(gildedFowl.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_HutHut_ActivatesMappedObjectiveAndSpawnsReviewedPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithHutHut(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss hutHut);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HutHut);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHutHut);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.TotalDomination);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.TotalDomination, 1u);
        AssertHutHutModel(hutHut, ExpectedHutHut);
        AssertGridEntityAddedToMap(mapProxy, hutHut.Instance, ExpectedHutHut.Position);
    }

    [Fact]
    public void OnPublicEventPhase_HutHut_DoesNotDuplicateReviewedPlacement()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithHutHut(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBoss hutHut);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HutHut);
        script.OnPublicEventPhase((uint)PublicEventPhase.HutHut);

        Assert.Single(hutHut.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void BevORageVendATron_OnActivateSuccess_UpdatesUseTargetGroupOnce()
    {
        var script = new BevORageVendATronEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity entity);

        script.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(12549u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void BevORageVendATronEntityScript_UsesReviewedCreatureFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(BevORageVendATronEntityScript));

        IScriptFilterSearch vendATronSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(66051u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(66052u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(vendATronSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Theory]
    [InlineData(typeof(SneakyPrisonGateConsoleEntityScript), 10569u)]
    [InlineData(typeof(SneakyPrisonCageConsoleEntityScript), 10583u)]
    [InlineData(typeof(SneakyPrisonAlarmPanelEntityScript), 12478u)]
    public void SneakyPrisonConsole_OnActivateSuccess_UpdatesMappedTargetGroupOnce(
        Type scriptType,
        uint targetGroupId)
    {
        var script = (IWorldEntityScript)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity entity);

        ((IOwnedScript<IWorldEntity>)script).OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(targetGroupId, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Theory]
    [InlineData(typeof(SneakyPrisonGateConsoleEntityScript), 62427u, 62988u)]
    [InlineData(typeof(SneakyPrisonGateConsoleEntityScript), 62987u, 62988u)]
    [InlineData(typeof(SneakyPrisonCageConsoleEntityScript), 63037u, 63038u)]
    [InlineData(typeof(SneakyPrisonAlarmPanelEntityScript), 68916u, 68917u)]
    public void SneakyPrisonConsoleEntityScripts_UseReviewedCreatureFilters(
        Type scriptType,
        uint creatureId,
        uint unrelatedCreatureId)
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(scriptType);

        IScriptFilterSearch consoleSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(creatureId);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(unrelatedCreatureId);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(consoleSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void MalfunctioningTank_OnDeath_UpdatesMappedTankObjectivesOnce()
    {
        var script = new MalfunctioningTankEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateCreatureWithPublicEventManager(out ICreatureEntity entity);

        script.OnLoad(entity);
        script.OnDeath();
        script.OnDeath();

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Where(i => i.Arguments.Length == 2)
            .ToList();
        Assert.Collection(updates,
            update =>
            {
                Assert.Equal(PublicEventObjective.DestructODerby, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            },
            update =>
            {
                Assert.Equal(PublicEventObjective.TankTrample, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            },
            update =>
            {
                Assert.Equal(PublicEventObjective.CanCrusher, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            },
            update =>
            {
                Assert.Equal(PublicEventObjective.GoingGreen, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            });
    }

    [Fact]
    public void MalfunctioningTank_OnDeath_ForwardsTankDeathToPublicEventScript()
    {
        var script = new MalfunctioningTankEntityScript();
        ICreatureEntity entity = CreateTankCreatureWithPublicEvent(
            ExpectedTankRoomSpawns[0].EntityId,
            50u,
            100u,
            out _,
            out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(entity);

        script.OnDeath();

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)));
    }

    [Fact]
    public void MalfunctioningTank_OnHealthChange_AtHalfHealthDamage_ForwardsThresholdSignal()
    {
        var script = new MalfunctioningTankEntityScript();
        ICreatureEntity entity = CreateTankCreatureWithPublicEvent(
            ExpectedTankRoomSpawns[0].EntityId,
            50u,
            100u,
            out _,
            out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(entity);

        script.OnHealthChange(null, 1u, DamageType.Physical);

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)));
    }

    [Fact]
    public void MalfunctioningTank_OnHealthChange_HealAboveHalfAndLethalDamage_DoNotForwardThresholdSignal()
    {
        var script = new MalfunctioningTankEntityScript();
        ICreatureEntity entity = CreateTankCreatureWithPublicEvent(
            ExpectedTankRoomSpawns[0].EntityId,
            50u,
            100u,
            out RecordingDispatchProxy<ICreatureEntity> entityProxy,
            out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(entity);

        script.OnHealthChange(null, 1u, DamageType.Heal);
        entityProxy.SetProperty(nameof(IWorldEntity.Health), 51u);
        script.OnHealthChange(null, 1u, DamageType.Physical);
        entityProxy.SetProperty(nameof(IWorldEntity.Health), 0u);
        script.OnHealthChange(null, 100u, DamageType.Physical);
        entityProxy.SetProperty(nameof(IWorldEntity.Health), 50u);
        script.OnHealthChange(null, 1u, null);

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)));
    }

    [Fact]
    public void GildedFowl_OnDeath_UpdatesMappedPowerPlungeObjectivesOnce()
    {
        var script = new GildedFowlEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateCreatureWithPublicEventManager(out ICreatureEntity entity);

        script.OnLoad(entity);
        script.OnDeath();
        script.OnDeath();

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Where(i => i.Arguments.Length == 2)
            .ToList();
        Assert.Collection(updates,
            update =>
            {
                Assert.Equal(PublicEventObjective.GildedFowl, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            },
            update =>
            {
                Assert.Equal(PublicEventObjective.GildedFowl2, update.Arguments[0]);
                Assert.Equal(100, update.Arguments[1]);
            });
    }

    [Fact]
    public void HutHut_OnDeath_UpdatesDefeatAndTotalDominationObjectivesOnce()
    {
        var script = new HutHutEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy =
            CreateCreatureWithPublicEventManager(out ICreatureEntity entity);

        script.OnLoad(entity);
        script.OnDeath();
        script.OnDeath();

        List<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Where(i => i.Arguments.Length == 2)
            .ToList();
        Assert.Collection(updates,
            update =>
            {
                Assert.Equal((uint)PublicEventObjective.DefeatHutHut, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            },
            update =>
            {
                Assert.Equal((uint)PublicEventObjective.TotalDomination, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            });
    }

    [Fact]
    public void Warden_OnDeath_UpdatesPrototentiaryAggregateObjectiveOnce()
    {
        var script = new WardenEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateCreatureWithPublicEventManager(out ICreatureEntity entity);

        script.OnLoad(entity);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal((uint)PublicEventObjective.SneakThroughThePrototentiary, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PrototentiaryAggregateSuccess_CreditsNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithPrototentiaryContent(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.Prototentiary);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SneakThroughThePrototentiary,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.NoDeaths, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PrototentiaryAggregateSuccessAfterPlayerDeath_DoesNotCreditNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithPrototentiaryContent(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.Prototentiary);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnDeath(player);
        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SneakThroughThePrototentiary,
            PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PrototentiaryAggregateSuccessAfterNonPlayerDeath_CreditsNoDeaths()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithPrototentiaryContent(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _,
            out _);
        script.OnLoad(publicEvent);
        script.OnPublicEventPhase((uint)PublicEventPhase.Prototentiary);
        IUnitEntity nonPlayer = RecordingDispatchProxy<IUnitEntity>.Create(out _);

        script.OnDeath(nonPlayer);
        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SneakThroughThePrototentiary,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.NoDeaths, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void MalfunctioningTankEntityScript_UsesReviewedCreatureFilters()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(MalfunctioningTankEntityScript));

        var match = new ScriptFilterMatch();
        foreach (uint creatureId in new[] { 62542u, 62543u, 62546u })
        {
            IScriptFilterSearch tankSearch = new ScriptFilterSearch()
                .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
                .FilterByCreatureId(creatureId);
            Assert.True(match.Match(tankSearch, parameters));
        }

        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(62575u);
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void GildedFowlEntityScript_UsesReviewedCreatureFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(GildedFowlEntityScript));

        IScriptFilterSearch gildedFowlSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(63055u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(63056u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(gildedFowlSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void WardenEntityScript_UsesReviewedCreatureFilter()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(WardenEntityScript));

        IScriptFilterSearch wardenSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(62324u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(62325u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(wardenSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_InitiateSuccess_AdvancesToWipTankRoom()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.InitiateUltimateProtogames,
            PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.TankRoom);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GoingGreenSuccess_AdvancesToWipMisplacedMammoth()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GoingGreen,
            PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.MisplacedMammoth);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DestructODerbySuccess_GrantsWasteManagementProfessionalToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DestructODerby,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5864, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.DestructODerby, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantWasteManagementProfessional(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_NoDeathsSuccess_GrantsImmortalWasteManagementFacilityToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.NoDeaths2,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5865, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.NoDeaths2, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantImmortalWasteManagementFacility(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_IncinerateSuccess_DoesNotGrantExpertIncineratorWithoutZeroPlayerHitEvidence()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Incinerate,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TankTrampleSuccess_GrantsTankTrampleAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.TankTrample,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5867, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.TankTrample, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantTankTrampleAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RedemptionValueSuccess_GrantsRedemptionValueAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.RedemptionValue,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5868, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.RedemptionValue, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantRedemptionValueAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CanCrusherSuccess_GrantsCanCrusherAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.CanCrusher,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5869, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.CanCrusher, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCanCrusherAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_EnvironmentalistSuccess_GrantsEnvironmentalistAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Environmentalist,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5870, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Environmentalist, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantEnvironmentalistAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KingsAndQueensSuccess_GrantsCrateRoyaltyAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.KingsAndQueensOfTheHill,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5871, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.KingsAndQueensOfTheHill, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCrateRoyaltyAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DestructODerbySuccess_DoesNotGrantRedTankRunawayWithoutMissileDistanceEvidence()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DestructODerby,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5872);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SurviveTheBlitzsquirgSuccess_GrantsAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SurviveTheBlitzsquirg,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5873, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SurviveTheBlitzsquirg, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GoingGreen, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSurviveTheBlitzsquirgAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.Empty(achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(28653u)]
    [InlineData(28654u)]
    [InlineData(28655u)]
    public void OnPublicEventObjectiveStatus_SurviveTheBlitzsquirgSuccessWithEquippedSquirgHelmet_GrantsSquirgception(
        uint squirgHelmetItemId)
    {
        IItem squirgHelmet = RecordingDispatchProxy<IItem>.Create(
            out RecordingDispatchProxy<IItem> squirgHelmetProxy);
        squirgHelmetProxy.SetProperty(nameof(IItem.Id), squirgHelmetItemId);

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(
            out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem), args =>
            args.Length == 2
            && (InventoryLocation)args[0] == InventoryLocation.Equipped
            && (uint)args[1] == (uint)EquippedItem.Head
                ? squirgHelmet
                : null);

        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SurviveTheBlitzsquirg,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5873);
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5882);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SurviveTheBlitzsquirgSuccessWithWrongHeadItem_DoesNotGrantSquirgception()
    {
        IItem wrongHeadItem = RecordingDispatchProxy<IItem>.Create(
            out RecordingDispatchProxy<IItem> wrongHeadItemProxy);
        wrongHeadItemProxy.SetProperty(nameof(IItem.Id), 28652u);

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(
            out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), wrongHeadItem);

        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SurviveTheBlitzsquirg,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5873);
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5882);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SurviveTheBlitzsquirgSuccessWithSquirgHelmetOnlyInInventory_DoesNotGrantSquirgception()
    {
        IItem squirgHelmet = RecordingDispatchProxy<IItem>.Create(
            out RecordingDispatchProxy<IItem> squirgHelmetProxy);
        squirgHelmetProxy.SetProperty(nameof(IItem.Id), 28653u);

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(
            out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodHandler(nameof(IInventory.GetItem), args =>
            args.Length == 2 && (InventoryLocation)args[0] == InventoryLocation.Inventory
                ? squirgHelmet
                : null);

        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SurviveTheBlitzsquirg,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5873);
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5882);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SurviveTheBlitzsquirgSuccessWithCompletedSquirgception_DoesNotGrantDuplicate()
    {
        IItem squirgHelmet = RecordingDispatchProxy<IItem>.Create(
            out RecordingDispatchProxy<IItem> squirgHelmetProxy);
        squirgHelmetProxy.SetProperty(nameof(IItem.Id), 28653u);

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(
            out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), squirgHelmet);

        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodHandler(
            nameof(ICharacterAchievementManager.HasCompletedAchievement),
            args => (ushort)args[0] == 5882);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SurviveTheBlitzsquirg,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5873);
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5882);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KickMaraudersIntoDeepSpaceSuccess_GrantsCosmicKickerToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.KickMaraudersIntoDeepSpace,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5883, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.KickMaraudersIntoDeepSpace, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Kick10Marauders, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCosmicKicker(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5883);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Kick10MaraudersSuccess_GrantsCosmicKick10ToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Kick10Marauders,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5886, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Kick10Marauders, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Kick20Marauders, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCosmicKick10(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5886);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Kick20MaraudersSuccess_GrantsCosmicKick20ToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Kick20Marauders,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5887, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Kick20Marauders, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Kick40Marauders, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCosmicKick20(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5887);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Kick40MaraudersSuccess_GrantsCosmicKick40ToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Kick40Marauders,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5888, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Kick40Marauders, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Kick80Marauders, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCosmicKick40(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5888);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Kick80MaraudersSuccess_GrantsCosmicKick80ToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Kick80Marauders,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5889, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Kick80Marauders, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Kick40Marauders, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCosmicKick80(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5889);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PerfectPrecisionSuccess_GrantsCosmicPrecisionToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.PerfectPrecision,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5890, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.PerfectPrecision, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Kick80Marauders, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCosmicPrecision(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5890);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_AcceleratedEradicationSuccess_GrantsOffMyShipToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.AcceleratedEradication,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5891, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.AcceleratedEradication, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.PerfectPrecision, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantOffMyShip(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5891);
    }

    [Theory]
    [InlineData(PublicEventObjective.KickMaraudersIntoDeepSpace)]
    [InlineData(PublicEventObjective.Kick10Marauders)]
    public void OnPublicEventObjectiveStatus_AggregateCosmicKickSuccess_DoesNotGrantTakingTurnsWithoutPerMemberEvidence(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5892);
    }

    [Theory]
    [InlineData(PublicEventObjective.KickMaraudersIntoDeepSpace)]
    [InlineData(PublicEventObjective.Kick20Marauders)]
    public void OnPublicEventObjectiveStatus_AggregateCosmicKickSuccess_DoesNotGrantMultiCosmicKickWithoutSameCastEvidence(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5893);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MasterTheElementsSuccess_GrantsMasterOfTheElementsToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.MasterTheElements,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5894, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.MasterTheElements, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.KickMaraudersIntoDeepSpace, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantMasterOfTheElements(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5894);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MasterTheElementsSuccess_DoesNotGrantImmortalElementalHospitalWithoutNoDeathsEvidence()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.MasterTheElements,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5894);
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5895);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PanicProntoSuccess_GrantsPanicProntoToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.PanicPronto,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5896, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.PanicPronto, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.ClaustrophobicSkip, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantPanicPronto(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5896);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ClaustrophobicSkipSuccess_GrantsClaustrophobicSkipToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.ClaustrophobicSkip,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5897, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.ClaustrophobicSkip, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.PanicPronto, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantClaustrophobicSkip(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5897);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_EscapedPatientSuccess_GrantsPatientSecuredToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.EscapedPatient,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5898, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.EscapedPatient, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.QuickVisit, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantPatientSecured(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5898);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_QuickVisitSuccess_GrantsInstitutionalizedToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.QuickVisit,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5899, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.QuickVisit, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.EscapedPatient, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantInstitutionalized(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5899);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ToxophobiaSuccess_GrantsToxophobiaToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Toxophobia,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5900, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Toxophobia, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Atychiphobia, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantToxophobia(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5900);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_AtychiphobiaSuccess_GrantsAtychiphobiaToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Atychiphobia,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5901, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Atychiphobia, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Toxophobia, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantAtychiphobia(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5901);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ChronophobiaSuccess_GrantsChronophobiaToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Chronophobia,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5902, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Chronophobia, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Atychiphobia, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantChronophobia(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5902);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MasterTheElementsSuccess_DoesNotGrantStageFrightWithoutPartyBeaconCarryHistory()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.MasterTheElements,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5903);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ToxophobiaSuccess_DoesNotGrantOvercomingToxophobiaWithoutTimedExposure()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Toxophobia,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5904);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SneakThroughThePrototentiarySuccess_GrantsPrototentiaryProwlToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SneakThroughThePrototentiary,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5905, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SneakThroughThePrototentiary, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.NoDeaths, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantPrototentiaryProwl(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5905);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_NoDeathsSuccess_GrantsImmortalPrototentiaryToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.NoDeaths,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5906, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.NoDeaths, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.NoDeaths2, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantImmortalPrototentiary(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5906);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ShadowsteppingSuccess_GrantsShadowstepperToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Shadowstepping,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5907, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Shadowstepping, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.KeenSenses, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantShadowstepper(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5907);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DriveBySuccess_GrantsGateGuardDriveByToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DriveBy,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5908, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.DriveBy, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Shadowstepping, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantGateGuardDriveBy(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5908);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KeenSensesSuccess_GrantsKeenSensesToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.KeenSenses,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5909, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.KeenSenses, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.LowProfile, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantKeenSenses(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5909);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_LowProfileSuccess_GrantsLowProfileToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.LowProfile,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5910, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.LowProfile, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.KeenSenses, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantLowProfile(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5910);
    }

    [Theory]
    [InlineData(PublicEventObjective.Undetectable)]
    [InlineData(PublicEventObjective.Deputy)]
    [InlineData(PublicEventObjective.SneakThroughThePrototentiary)]
    public void OnPublicEventObjectiveStatus_ConflictingUndetectableSignals_DoNotGrantAchievement(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5911);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GhostsSuccess_GrantsGhostToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Ghosts,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5912, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Ghosts, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.FastHands, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantGhost(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5912);
    }

    [Theory]
    [InlineData(PublicEventObjective.DisableTheAlarm)]
    [InlineData(PublicEventObjective.Ghosts)]
    public void OnPublicEventObjectiveStatus_AlarmRelatedSuccess_DoesNotGrantHitSnoozeWithoutTriggerTimerHistory(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5913);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FastHandsSuccess_GrantsPrestoPrototentiaryToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.FastHands,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5914, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.FastHands, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Ghosts, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantPrestoPrototentiary(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5914);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CompleteProtoPlungeSuccess_GrantsProtoplungeProfessionalToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.CompleteTheProtoPlunge,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5915, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.CompleteTheProtoPlunge, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GildedFowl, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantProtoplungeProfessional(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5915);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ExtraPointSuccess_GrantsExtraPointProfessionalToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.ExtraPoint,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5916, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.ExtraPoint, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.SlickMoves, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantExtraPointProfessional(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5916);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ProtoPlunges30Success_GrantsUltimateProtoPlungerToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.ProtoPlunges30,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5917, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.ProtoPlunges30, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.CompleteTheProtoPlunge, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantUltimateProtoPlunger(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5917);
    }

    [Theory]
    [InlineData(PublicEventObjective.CubigCarnage)]
    [InlineData(PublicEventObjective.GildedFowl2)]
    public void OnPublicEventObjectiveStatus_PowerPlungeCountMismatch_DoesNotGrantCubigCarnage(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5918);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GildedFowlSuccess_GrantsFowlFeastToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GildedFowl,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5919, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.GildedFowl, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GildedFowl2, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantFowlFeast(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5919);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CrystalCatcherSuccess_GrantsAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.CrystalCatcher,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5920, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.CrystalCatcher, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.GildedFowl, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCrystalCatcher(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5920);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_WaterHazardSuccess_GrantsSkySoaringToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.WaterHazard,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5921, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.WaterHazard, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.CrystalCatcher, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSkySoaring(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5921);
    }

    [Theory]
    [InlineData(PublicEventObjective.CompleteTheProtoPlunge)]
    [InlineData(PublicEventObjective.GildedFowl)]
    [InlineData(PublicEventObjective.CrystalCatcher)]
    [InlineData(PublicEventObjective.WaterHazard)]
    public void OnPublicEventObjectiveStatus_PowerPlungeObjectiveSuccess_DoesNotGrantDisorientedDescent(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5922);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_PestControlSuccess_GrantsProfessionalAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.PestControl,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5923, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.PestControl, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Slaughterhouse, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantPestControlProfessional(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5923);
    }

    [Theory]
    [InlineData(PublicEventObjective.NoDeaths)]
    [InlineData(PublicEventObjective.NoDeaths2)]
    public void OnPublicEventObjectiveStatus_PestControlAndUnscopedNoDeathsSuccess_DoNotGrantImmortalProtostarPettingZoo(
        PublicEventObjective noDeathsObjective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.PestControl,
            PublicEventStatus.Succeeded,
            1001ul));
        script.OnPublicEventObjectiveStatus(CreateObjective(
            noDeathsObjective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5924);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RunawayVeggieSuccess_GrantsSteamedVeggiesToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.RunawayVeggie,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5925, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.RunawayVeggie, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Slaughterhouse, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSteamedVeggies(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5925);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RowsdowerRoundUpSuccess_GrantsAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.RowsdowerRoundUp,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5926, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.RowsdowerRoundUp, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.RunawayVeggie, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantRowsdowerRoundUp(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5926);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DamageControlSuccess_GrantsAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DamageControl,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5927, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.DamageControl, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.RowsdowerRoundUp, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantDamageControl(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5927);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SplorgSpreeSuccess_GrantsAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SplorgSpree,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5928, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SplorgSpree, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.DamageControl, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSplorgSpree(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5928);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SlaughterhouseSuccess_GrantsJabbitJustificationToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Slaughterhouse,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5929, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.Slaughterhouse, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.SplorgSpree, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantJabbitJustification(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5929);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SplorgStepperSuccess_GrantsAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SplorgStepper,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5930, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SplorgStepper, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.Slaughterhouse, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSplorgStepper(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5930);
    }

    [Theory]
    [InlineData(PublicEventObjective.HuntRuffles)]
    [InlineData(PublicEventObjective.Slaughterhouse)]
    public void OnPublicEventObjectiveStatus_NearbySuccess_DoesNotGrantFluffyAndFluzzballWithoutLayEggsEvidence(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5931);
    }

    [Theory]
    [InlineData(PublicEventObjective.HuntRuffles)]
    [InlineData(PublicEventObjective.PestControl)]
    [InlineData(PublicEventObjective.Slaughterhouse)]
    public void OnPublicEventObjectiveStatus_NearbySuccess_DoesNotGrantLikeMotherLikeDaughtersWithoutVehicleKillAttribution(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5932);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ClearTheLostAndFoundSuccess_GrantsDustBusterToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.ClearTheLostAndFound,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5933, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.ClearTheLostAndFound, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.DustStorm, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantDustBuster(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5933);
    }

    [Theory]
    [InlineData(PublicEventObjective.ClearTheLostAndFound)]
    [InlineData(PublicEventObjective.SpringCleaning)]
    [InlineData(PublicEventObjective.NoDeaths)]
    [InlineData(PublicEventObjective.NoDeaths2)]
    public void OnPublicEventObjectiveStatus_LostAndFoundOrOtherRoomSuccess_DoesNotGrantImmortalLostAndFoundWithoutRoomScopedNoDeathsEvidence(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5934);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MondosCrateSuccess_GrantsWhatsInTheBoxToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.MondosCrate,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5935, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.MondosCrate, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.MonstrosityMassacre, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantWhatsInTheBox(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5935);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DustStormSuccess_GrantsDustStormAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DustStorm,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5936, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.DustStorm, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.ClearTheLostAndFound, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantDustStormAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5936);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_QuickReflexesSuccess_GrantsMonstrosityMassacreToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.QuickReflexes,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5937, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.QuickReflexes, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.MonstrosityMassacre, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantMonstrosityMassacreAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5937);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FriendOfCrateSuccess_GrantsFriendOfCrateToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.FriendOfCrate,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5938, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.FriendOfCrate, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.SpringCleaning, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantFriendOfCrateAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5938);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_WelcomeToTheThunderdomeSuccess_GrantsWeatheringTheStormToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.WelcomeToTheThunderdome,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5939, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.WelcomeToTheThunderdome, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.DustStorm, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantWeatheringTheStormAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5939);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SpringCleaningSuccess_GrantsSpringCleaningToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SpringCleaning,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5940, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SpringCleaning, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.ClearTheLostAndFound, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSpringCleaningAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5940);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_KickMaraudersIntoDeepSpaceSuccess_DoesNotGrantImmortalHmsPhineasWithoutNoDeathsEvidence()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.KickMaraudersIntoDeepSpace,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5883);
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5884);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_RapidFireSuccess_DoesNotGrantRapidKickerFromThreeKickObjective()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        script.OnLoad(CreatePublicEvent(out _));

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.RapidFire,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5885);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SurviveTheBlitzsquirgSuccess_DoesNotGrantImmortalSquirgnasiumWithoutNoDeathsEvidence()
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SurviveTheBlitzsquirg,
            PublicEventStatus.Succeeded,
            1001ul));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> grants =
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement));
        Assert.Contains(grants, invocation => (ushort)invocation.Arguments[0] == 5873);
        Assert.DoesNotContain(grants, invocation => (ushort)invocation.Arguments[0] == 5874);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_CleanSweepSuccess_GrantsCleanSweeperAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.CleanSweep,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5875, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.CleanSweep, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.SurviveTheBlitzsquirg, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantCleanSweeperAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5875);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SquirgDefuserSuccess_GrantsSquirgDefuserAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SquirgDefuser,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5876, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SquirgDefuser, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.CleanSweep, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSquirgDefuserAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5876);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_JumpJumpSuccess_GrantsJumpAroundAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.JumpJump,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5877, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.JumpJump, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.SquirgDefuser, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantJumpAroundAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5877);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_TwinkleToesSuccess_GrantsTwinkleToesAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.TwinkleToes,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5878, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.TwinkleToes, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.JumpJump, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantTwinkleToesAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5878);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SpeedySlaughterfestSuccess_GrantsSpeedySlaughterfestAchievementToEligibleOnlineMembers()
    {
        ICharacterAchievementManager eligibleAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> eligibleAchievementsProxy);
        eligibleAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer eligiblePlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> eligiblePlayerProxy);
        eligiblePlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), eligibleAchievements);

        ICharacterAchievementManager completedAchievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> completedAchievementsProxy);
        completedAchievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), true);
        IPlayer completedPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> completedPlayerProxy);
        completedPlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), completedAchievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] switch
        {
            1001ul => eligiblePlayer,
            1002ul => completedPlayer,
            _      => null
        });

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.SpeedySlaughterfest,
            PublicEventStatus.Succeeded,
            1001ul,
            1002ul,
            1003ul));

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            eligibleAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)5879, grant.Arguments[0]);
        Assert.Empty(completedAchievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    [Theory]
    [InlineData(PublicEventObjective.SpeedySlaughterfest, PublicEventStatus.Failed)]
    [InlineData(PublicEventObjective.TwinkleToes, PublicEventStatus.Succeeded)]
    public void OnPublicEventObjectiveStatus_NonQualifyingBoundary_DoesNotGrantSpeedySlaughterfestAchievement(
        PublicEventObjective objective,
        PublicEventStatus status)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, status, 1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5879);
    }

    [Theory]
    [InlineData(PublicEventObjective.TwinkleToes)]
    [InlineData(PublicEventObjective.SpeedySlaughterfest)]
    [InlineData(PublicEventObjective.JumpJump)]
    public void OnPublicEventObjectiveStatus_SquirgObjectiveSuccess_DoesNotGrantSquirgFarmerWithoutSurvivalProducer(
        PublicEventObjective objective)
    {
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementsProxy);
        achievementsProxy.SetMethodReturn(nameof(ICharacterAchievementManager.HasCompletedAchievement), false);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(
            out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        var script = new UltimateProtogamesEventScript(playerManager);
        IPublicEvent publicEvent = CreatePublicEvent(out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            objective,
            PublicEventStatus.Succeeded,
            1001ul));

        Assert.DoesNotContain(
            achievementsProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)),
            invocation => (ushort)invocation.Arguments[0] == 5880);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_QuickReflexes2Success_AdvancesToWipPrototentiary()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.QuickReflexes2,
            PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.Prototentiary);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DeputySuccess_AdvancesToWipRuffles()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Deputy,
            PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.Ruffles);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_HuntRufflesSuccess_AdvancesToWipPowerPlunge()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.HuntRuffles,
            PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.PowerPlunge);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_GildedFowl2Success_AdvancesToWipHutHut()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.GildedFowl2,
            PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.HutHut);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = new UltimateProtogamesEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.InitiateUltimateProtogames,
            PublicEventStatus.Active));

        RecordingDispatchProxy<IPublicEvent>.Invocation phase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Welcome, phase.Arguments[0]);
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithBevORage(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBoss bevORage)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        bevORage = CreateCreatedBoss();
        eventProxy.SetMethodReturn(nameof(IPublicEvent.CreateEntity), bevORage.Instance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithStartButton(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedSimple startButton)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        startButton = CreateCreatedSimple();
        eventProxy.SetMethodReturn(nameof(IPublicEvent.CreateEntity), startButton.Instance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithTankRoom(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTank> tanks)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        tanks = [];
        List<CreatedTank> createdTanks = tanks;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTank tank = CreateCreatedTank();
            createdTanks.Add(tank);
            return tank.Instance;
        });
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithMisplacedMammoth(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBoss mammoth,
        out CreatedBoss mondo,
        out CreatedBoss crate)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        mammoth = CreateCreatedBoss();
        CreatedBoss createdMammoth = mammoth;
        mondo = CreateCreatedBoss();
        CreatedBoss createdMondo = mondo;
        crate = CreateCreatedBoss();
        CreatedBoss createdCrate = crate;
        int createCount = 0;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            if (createCount++ == 0)
                return createdMammoth.Instance;

            if (createCount == 2)
                return createdMondo.Instance;

            return createdCrate.Instance;
        });
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithPrototentiaryContent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedSimple> consoles,
        out CreatedBoss deputy,
        out CreatedBoss warden)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        consoles = [];
        List<CreatedSimple> createdConsoles = consoles;
        deputy = CreateCreatedBoss();
        CreatedBoss createdDeputy = deputy;
        warden = CreateCreatedBoss();
        CreatedBoss createdWarden = warden;
        int createCount = 0;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            if (createCount < ExpectedPrototentiaryConsoleSpawns.Length)
            {
                createCount++;
                CreatedSimple console = CreateCreatedSimple();
                createdConsoles.Add(console);
                return console.Instance;
            }

            createCount++;
            if (createCount == ExpectedPrototentiaryConsoleSpawns.Length + 1)
                return createdDeputy.Instance;

            return createdWarden.Instance;
        });
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithRuffles(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBoss ruffles)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        ruffles = CreateCreatedBoss();
        CreatedBoss createdRuffles = ruffles;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => createdRuffles.Instance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithGildedFowl(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBoss gildedFowl)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        gildedFowl = CreateCreatedBoss();
        CreatedBoss createdGildedFowl = gildedFowl;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => createdGildedFowl.Instance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithHutHut(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBoss hutHut)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2980u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        hutHut = CreateCreatedBoss();
        CreatedBoss createdHutHut = hutHut;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => createdHutHut.Instance);
        return publicEvent;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateCreatureWithPublicEventManager(out ICreatureEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static ICreatureEntity CreateTankCreatureWithPublicEvent(
        uint entityId,
        uint health,
        uint maxHealth,
        out RecordingDispatchProxy<ICreatureEntity> entityProxy,
        out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        publicEventManagerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvent), publicEvent);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ICreatureEntity entity = RecordingDispatchProxy<ICreatureEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        entityProxy.SetProperty(nameof(IWorldEntity.EntityId), entityId);
        entityProxy.SetProperty(nameof(IWorldEntity.PublicEventId), 594u);
        entityProxy.SetProperty(nameof(IWorldEntity.Health), health);
        entityProxy.SetProperty(nameof(IWorldEntity.MaxHealth), maxHealth);
        return entity;
    }

    private static IUnitEntity CreateTankUnit(uint entityId, uint health, uint maxHealth)
    {
        IUnitEntity entity = RecordingDispatchProxy<IUnitEntity>.Create(
            out RecordingDispatchProxy<IUnitEntity> entityProxy);
        entityProxy.SetProperty(nameof(IWorldEntity.EntityId), entityId);
        entityProxy.SetProperty(nameof(IWorldEntity.Health), health);
        entityProxy.SetProperty(nameof(IWorldEntity.MaxHealth), maxHealth);
        return entity;
    }

    private static IReadOnlyList<RecordingDispatchProxy<IPublicEvent>.Invocation> GetObjectiveUpdates(
        RecordingDispatchProxy<IPublicEvent> eventProxy,
        PublicEventObjective objective)
    {
        return eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective))
            .Where(i => i.Arguments.Length == 2 && Equals(i.Arguments[0], objective))
            .ToList();
    }

    private static IPublicEventObjective CreateObjective(
        PublicEventObjective objective,
        PublicEventStatus status,
        params ulong[] characterIds)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out RecordingDispatchProxy<IPublicEventObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        if (characterIds.Length != 0)
        {
            IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out RecordingDispatchProxy<IPublicEventTeam> teamProxy);
            IPublicEventTeamMember[] members = characterIds
                .Select(characterId =>
                {
                    IPublicEventTeamMember member = RecordingDispatchProxy<IPublicEventTeamMember>.Create(
                        out RecordingDispatchProxy<IPublicEventTeamMember> memberProxy);
                    memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), characterId);
                    return member;
                })
                .ToArray();
            teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetMembers), members);
            objectiveProxy.SetProperty(nameof(IPublicEventObjective.Team), team);
        }
        return eventObjective;
    }

    private static CreatedBoss CreateCreatedBoss()
    {
        INonPlayerEntity boss = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> bossProxy);
        return new CreatedBoss(boss, bossProxy);
    }

    private static CreatedSimple CreateCreatedSimple()
    {
        ISimpleEntity simple = RecordingDispatchProxy<ISimpleEntity>.Create(
            out RecordingDispatchProxy<ISimpleEntity> simpleProxy);
        return new CreatedSimple(simple, simpleProxy);
    }

    private static CreatedTank CreateCreatedTank()
    {
        INonPlayerEntity tank = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> tankProxy);
        return new CreatedTank(tank, tankProxy);
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static void AssertObjectiveActivatedWithMax(
        RecordingDispatchProxy<IPublicEvent> eventProxy,
        PublicEventObjective objective,
        uint max)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
        Assert.Equal(max, activation.Arguments[1]);
    }

    private static void AssertBevORageModel(CreatedBoss boss, Vector3 position)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            boss.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300055u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(61463u, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(36682u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal(5u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("BevORageEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
    }

    private static void AssertMisplacedMammothModel(CreatedBoss boss, Vector3 position)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            boss.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300080u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(63312u, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4351u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(26004u, model.DisplayInfo);
        Assert.Equal((ushort)1322u, model.Faction1);
        Assert.Equal((ushort)1322u, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.MisplacedMammoth, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("MisplacedMammothEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 398594f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.InterruptArmour && stat.Value == 3f);
    }

    private static void AssertMondosMonstrosityModel(CreatedBoss boss, ExpectedMondosMonstrositySpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            boss.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4351u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.MisplacedMammoth, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1369030f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
    }

    private static void AssertMondosCrateModel(CreatedBoss boss, ExpectedMondosCrateSpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            boss.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4351u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.MisplacedMammoth, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1410065408f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.InterruptArmour && stat.Value == 2f);
    }

    private static void AssertStartButtonModel(CreatedSimple simple, Vector3 position)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            simple.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(6128905u, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(65900u, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4330u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(25115u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal(0u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("UltimateProtogamesStartButtonEntityScript", entityScript.ScriptName));
    }

    private static void AssertTankRoomModel(CreatedTank tank, ExpectedTankRoomSpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            tank.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4336u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.TankRoom, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("MalfunctioningTankEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1563977f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
    }

    private static void AssertPrototentiaryConsoleModel(CreatedSimple console, ExpectedPrototentiaryConsoleSpawn expected)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            console.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4333u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.Prototentiary, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
    }

    private static void AssertPrototentiaryDeputyModel(CreatedBoss deputy, ExpectedPrototentiaryDeputySpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            deputy.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4333u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.Prototentiary, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.InterruptArmour && stat.Value == 24f);
    }

    private static void AssertPrototentiaryWardenModel(CreatedBoss warden, ExpectedPrototentiaryWardenSpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            warden.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4333u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.Prototentiary, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 830314f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 90900f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.InterruptArmour && stat.Value == 5f);
    }

    private static void AssertRufflesModel(CreatedBoss ruffles, ExpectedRufflesSpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            ruffles.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4348u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.Ruffles, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 3420014f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.InterruptArmour && stat.Value == 2f);
    }

    private static void AssertGildedFowlModel(CreatedBoss gildedFowl, ExpectedGildedFowlSpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            gildedFowl.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4347u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.PowerPlunge, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 461770f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.InterruptArmour && stat.Value == 0f);
    }

    private static void AssertHutHutModel(CreatedBoss hutHut, ExpectedHutHutSpawn expected)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            hutHut.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(expected.EntityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(expected.CreatureId, model.Creature);
        Assert.Equal((ushort)2980u, model.World);
        Assert.Equal((ushort)4376u, model.Area);
        Assert.Equal(expected.Position.X, model.X);
        Assert.Equal(expected.Position.Y, model.Y);
        Assert.Equal(expected.Position.Z, model.Z);
        Assert.Equal(expected.DisplayInfo, model.DisplayInfo);
        Assert.Equal(expected.FactionId, model.Faction1);
        Assert.Equal(expected.FactionId, model.Faction2);
        Assert.Equal(594u, model.EntityEvent.EventId);
        Assert.Equal((uint)PublicEventPhase.HutHut, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(expected.ScriptName, entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 2400000f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(2980u, position.Info.Entry.Id);
    }

    private static void AssertGridEntitiesAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IReadOnlyList<CreatedTank> tanks,
        IReadOnlyList<ExpectedTankRoomSpawn> expectedSpawns)
    {
        IReadOnlyList<RecordingDispatchProxy<IMapInstance>.Invocation> enqueueAdds = mapProxy
            .GetInvocations(nameof(IMap.EnqueueAdd));
        Assert.Equal(expectedSpawns.Count, enqueueAdds.Count);

        for (int i = 0; i < expectedSpawns.Count; i++)
        {
            Assert.Same(tanks[i].Instance, enqueueAdds[i].Arguments[0]);
            IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdds[i].Arguments[1]);
            Assert.Equal(expectedSpawns[i].Position, position.Position);
            Assert.Equal(2980u, position.Info.Entry.Id);
        }
    }

    private static void AssertMisplacedMammothRoomContentAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        CreatedBoss mammoth,
        CreatedBoss mondo,
        CreatedBoss crate)
    {
        IReadOnlyList<RecordingDispatchProxy<IMapInstance>.Invocation> enqueueAdds = mapProxy
            .GetInvocations(nameof(IMap.EnqueueAdd));
        Assert.Equal(3, enqueueAdds.Count);

        Assert.Same(mammoth.Instance, enqueueAdds[0].Arguments[0]);
        IMapPosition mammothPosition = Assert.IsAssignableFrom<IMapPosition>(enqueueAdds[0].Arguments[1]);
        Assert.Equal(new Vector3(-16482f, -910f, -10996f), mammothPosition.Position);
        Assert.Equal(2980u, mammothPosition.Info.Entry.Id);

        Assert.Same(mondo.Instance, enqueueAdds[1].Arguments[0]);
        IMapPosition mondoPosition = Assert.IsAssignableFrom<IMapPosition>(enqueueAdds[1].Arguments[1]);
        Assert.Equal(ExpectedMondoSpawn.Position, mondoPosition.Position);
        Assert.Equal(2980u, mondoPosition.Info.Entry.Id);

        Assert.Same(crate.Instance, enqueueAdds[2].Arguments[0]);
        IMapPosition cratePosition = Assert.IsAssignableFrom<IMapPosition>(enqueueAdds[2].Arguments[1]);
        Assert.Equal(ExpectedMondoCrateSpawn.Position, cratePosition.Position);
        Assert.Equal(2980u, cratePosition.Info.Entry.Id);
    }

    private static void AssertPrototentiaryContentAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IReadOnlyList<CreatedSimple> consoles,
        CreatedBoss deputy,
        CreatedBoss warden)
    {
        IReadOnlyList<RecordingDispatchProxy<IMapInstance>.Invocation> enqueueAdds = mapProxy
            .GetInvocations(nameof(IMap.EnqueueAdd));
        Assert.Equal(ExpectedPrototentiaryConsoleSpawns.Length + 2, enqueueAdds.Count);

        for (int i = 0; i < ExpectedPrototentiaryConsoleSpawns.Length; i++)
        {
            Assert.Same(consoles[i].Instance, enqueueAdds[i].Arguments[0]);
            IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdds[i].Arguments[1]);
            Assert.Equal(ExpectedPrototentiaryConsoleSpawns[i].Position, position.Position);
            Assert.Equal(2980u, position.Info.Entry.Id);
        }

        RecordingDispatchProxy<IMapInstance>.Invocation deputyAdd = enqueueAdds[^2];
        Assert.Same(deputy.Instance, deputyAdd.Arguments[0]);
        IMapPosition deputyPosition = Assert.IsAssignableFrom<IMapPosition>(deputyAdd.Arguments[1]);
        Assert.Equal(ExpectedDeputySpawn.Position, deputyPosition.Position);
        Assert.Equal(2980u, deputyPosition.Info.Entry.Id);

        RecordingDispatchProxy<IMapInstance>.Invocation wardenAdd = enqueueAdds[^1];
        Assert.Same(warden.Instance, wardenAdd.Arguments[0]);
        IMapPosition wardenPosition = Assert.IsAssignableFrom<IMapPosition>(wardenAdd.Arguments[1]);
        Assert.Equal(ExpectedWardenSpawn.Position, wardenPosition.Position);
        Assert.Equal(2980u, wardenPosition.Info.Entry.Id);
    }

    private sealed record CreatedBoss(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimple(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);

    private sealed record CreatedTank(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record ExpectedTankRoomSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo);

    private sealed record ExpectedPrototentiaryConsoleSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedPrototentiaryDeputySpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedPrototentiaryWardenSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedMondosMonstrositySpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedMondosCrateSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedRufflesSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedGildedFowlSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);

    private sealed record ExpectedHutHutSpawn(
        uint EntityId,
        uint CreatureId,
        Vector3 Position,
        uint DisplayInfo,
        ushort FactionId,
        string ScriptName);
}
