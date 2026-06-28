using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Expedition.RageLogic;
using NexusForever.Script.Instance.Expedition.RageLogic.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class RageLogicEventScriptTests
{
    [Fact]
    public void RageLogicMapScript_OnLoad_CreatesMainAndVehicleChoicePublicEvents()
    {
        RageLogicMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [214u, 213u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(i => (uint)i.Arguments[0])
                .ToArray());
    }

    [Fact]
    public void RageLogicMapScript_OnAddToMap_JoinsPlayersToMainAndVehicleChoicePublicEvents()
    {
        RageLogicMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
            out RecordingDispatchProxy<IPublicEvent> vehicleChoiceEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoin(mainEventProxy, player);
        AssertJoin(vehicleChoiceEventProxy, player);
    }

    [Theory]
    [InlineData(typeof(RageLogicEventScript), 214u)]
    [InlineData(typeof(RageLogicVehicleChoiceEventScript), 213u)]
    public void EventScripts_AreOwnedByMappedPublicEvents(Type scriptType, uint expectedOwnerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal([expectedOwnerId], attribute.Id);
    }

    [Fact]
    public void MainEvent_OnLoad_SetsInitialAsteroidAssaultPhase()
    {
        CreateMainEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ObliterateRagebotsDefendingTheAsteroid, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.ObliterateRagebotsDefendingTheAsteroid, PublicEventObjective.ObliterateRagebotsDefendingAsteroid)]
    [InlineData(PublicEventPhase.DestroyAsteroidEngines, PublicEventObjective.DestroyAsteroidEngines)]
    public void MainEvent_OnPublicEventPhase_ActivatesMappedObjective(
        PublicEventPhase phase,
        PublicEventObjective objective)
    {
        RageLogicEventScript script = CreateMainEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
    }

    [Fact]
    public void MainEvent_OnPublicEventObjectiveStatus_RagebotsSucceeded_StartsThrusterPhase()
    {
        RageLogicEventScript script = CreateMainEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ObliterateRagebotsDefendingAsteroid, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation setPhase = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.DestroyAsteroidEngines, setPhase.Arguments[0]);
    }

    [Fact]
    public void MainEvent_OnPublicEventObjectiveStatus_ThrustersSucceeded_StopsAtEvidenceBoundary()
    {
        RageLogicEventScript script = CreateMainEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DestroyAsteroidEngines, PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void MainEvent_OnPublicEventObjectiveStatus_NonSucceededObjectiveDoesNotAdvance()
    {
        RageLogicEventScript script = CreateMainEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ObliterateRagebotsDefendingAsteroid, PublicEventStatus.Failed));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Fact]
    public void VehicleChoiceEvent_OnLoad_SetsVehicleChoicePhase()
    {
        CreateVehicleChoiceEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ChooseAVehicle, invocation.Arguments[0]);
    }

    [Fact]
    public void VehicleChoiceEvent_OnPublicEventPhase_ActivatesVehicleObjective()
    {
        RageLogicVehicleChoiceEventScript script = CreateVehicleChoiceEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)PublicEventPhase.ChooseAVehicle);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ChooseVehicle);
    }

    [Fact]
    public void VehicleChoiceEvent_OnPublicEventObjectiveStatus_ChooseVehicleSucceeded_FinishesSideEvent()
    {
        RageLogicVehicleChoiceEventScript script = CreateVehicleChoiceEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ChooseVehicle, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void VehicleChoiceEvent_OnPublicEventObjectiveStatus_NonSucceededObjectiveDoesNotFinish()
    {
        RageLogicVehicleChoiceEventScript script = CreateVehicleChoiceEventScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ChooseVehicle, PublicEventStatus.Failed));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void VehicleChoiceEntityScript_OnActivateSuccess_UpdatesMappedScriptObjectiveOnce()
    {
        var script = new RageLogicVehicleChoiceEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.ChooseVehicle, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Empty(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void AsteroidThrusterEntityScript_OnActivateSuccess_UpdatesMappedChecklistObjectiveOnceAndRemovesEntity()
    {
        var script = new AsteroidThrusterEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity worldEntity,
            out RecordingDispatchProxy<IWorldEntity> worldEntityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(worldEntity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(3770u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
        Assert.Single(worldEntityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void VehicleChoiceEntityScript_UsesCreatureFilterForMappedVehicleRows()
    {
        AssertCreatureFilterMatches(
            typeof(RageLogicVehicleChoiceEntityScript),
            [32249u, 43809u, 43810u],
            32365u);
    }

    [Fact]
    public void AsteroidThrusterEntityScript_UsesCreatureFilterForMappedThrusterRow()
    {
        AssertCreatureFilterMatches(
            typeof(AsteroidThrusterEntityScript),
            [32365u],
            32249u);
    }

    private static RageLogicMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> vehicleChoiceEventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEvent vehicleChoiceEvent = RecordingDispatchProxy<IPublicEvent>.Create(out vehicleChoiceEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            uint id = (uint)args[0];
            return id == 214u ? mainEvent : vehicleChoiceEvent;
        });

        contentMap = CreateContentMap(publicEventManager);
        return new RageLogicMapScript();
    }

    private static IContentMapInstance CreateContentMap(IPublicEventManager publicEventManager)
    {
        IContentMapInstance contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out RecordingDispatchProxy<IContentMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        return contentMap;
    }

    private static RageLogicEventScript CreateMainEventScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        var script = new RageLogicEventScript();
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static RageLogicVehicleChoiceEventScript CreateVehicleChoiceEventScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        var script = new RageLogicVehicleChoiceEventScript();
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
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

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(
        out IWorldEntity worldEntity,
        out RecordingDispatchProxy<IWorldEntity> worldEntityProxy)
    {
        worldEntity = RecordingDispatchProxy<IWorldEntity>.Create(out worldEntityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        worldEntityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static void AssertJoin(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation join = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, join.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, join.Arguments[1]);
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static void AssertCreatureFilterMatches(Type scriptType, IReadOnlyCollection<uint> matchingCreatureIds, uint unrelatedCreatureId)
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(scriptType);

        var match = new ScriptFilterMatch();
        foreach (uint matchingCreatureId in matchingCreatureIds)
        {
            IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
                .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
                .FilterByCreatureId(matchingCreatureId);
            Assert.True(match.Match(matchingSearch, parameters));
        }

        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ICreatureEntity>>()
            .FilterByCreatureId(unrelatedCreatureId);
        Assert.False(match.Match(unrelatedSearch, parameters));
    }
}
