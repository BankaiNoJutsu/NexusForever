using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
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
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GoingGreen);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.GoingGreen, 3u);
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
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.SneakThroughThePrototentiary, 2u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.HackThecreature62987, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.HackThecreature63037, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.FastHands, 1u);
        AssertObjectiveActivatedWithMax(eventProxy, PublicEventObjective.DisableTheAlarm, 1u);
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

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out RecordingDispatchProxy<IPublicEventObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
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
