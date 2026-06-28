using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.TheMalgraveTrail;
using NexusForever.Script.Instance.Adventure.TheMalgraveTrail.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using MalgraveExodusScript = NexusForever.Script.Instance.Adventure.TheMalgraveTrail.TheMalgraveTrailExodusEventScript;
using MalgraveEventScript = NexusForever.Script.Instance.Adventure.TheMalgraveTrail.TheMalgraveTrailEventScript;
using MalgraveObjective = NexusForever.Script.Instance.Adventure.TheMalgraveTrail.PublicEventObjective;
using MalgravePhase = NexusForever.Script.Instance.Adventure.TheMalgraveTrail.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class TheMalgraveTrailAdventureScriptTests
{
    [Fact]
    public void MalgraveMapScript_OnLoad_CreatesParentAndExodusPublicEvents()
    {
        TheMalgraveTrailMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        uint[] createdEvents = managerProxy
            .GetInvocations(nameof(IPublicEventManager.CreateEvent))
            .Select(i => (uint)i.Arguments[0])
            .ToArray();

        Assert.Equal(new[] { 53u, 56u }, createdEvents);
    }

    [Fact]
    public void MalgraveMapScript_OnAddToMap_JoinsPlayersToParentAndExodusPublicEvents()
    {
        TheMalgraveTrailMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> parentEventProxy,
            out RecordingDispatchProxy<IPublicEvent> exodusEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoined(parentEventProxy, player);
        AssertJoined(exodusEventProxy, player);
    }

    [Theory]
    [InlineData(typeof(TheMalgraveTrailMapScript), 1181u)]
    [InlineData(typeof(MalgraveEventScript), 53u)]
    [InlineData(typeof(MalgraveExodusScript), 56u)]
    public void MalgraveScripts_AreOwnedByMappedClientRows(Type scriptType, uint ownerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { ownerId }, attribute.Id);
    }

    [Fact]
    public void Exodus_OnLoad_SetsTownCenterRallyPhase()
    {
        _ = CreateExodusScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(MalgravePhase.RallySurvivorsAtTownCenter, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(MalgravePhase.RallySurvivorsAtTownCenter, MalgraveObjective.RallySurvivorsAtTownCenter)]
    [InlineData(MalgravePhase.TalkToBraithwaitInThirstyCreek, MalgraveObjective.TalkToBraithwaitInThirstyCreek)]
    [InlineData(MalgravePhase.TalkToBraithwaitNearKurg, MalgraveObjective.TalkToBraithwaitNearKurg)]
    [InlineData(MalgravePhase.ListenToBraithwaitStory, MalgraveObjective.ListenToBraithwaitStory)]
    [InlineData(MalgravePhase.RallySurvivorsAroundInn, MalgraveObjective.RallySurvivorsAroundInn)]
    [InlineData(MalgravePhase.RallySurvivorsNearWreckedShip, MalgraveObjective.RallySurvivorsNearWreckedShip)]
    [InlineData(MalgravePhase.TalkToBraithwaitInTownCenter, MalgraveObjective.TalkToBraithwaitInTownCenter)]
    [InlineData(MalgravePhase.CollectFeedSacksWaterBarrelsAndFoodCrates, MalgraveObjective.CollectFeedSacksWaterBarrelsAndFoodCrates)]
    public void Exodus_Phases_ActivateMappedObjectives(MalgravePhase phase, MalgraveObjective objective)
    {
        MalgraveExodusScript script = CreateExodusScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(MalgraveObjective.RallySurvivorsAtTownCenter, MalgravePhase.TalkToBraithwaitInThirstyCreek)]
    [InlineData(MalgraveObjective.TalkToBraithwaitInThirstyCreek, MalgravePhase.TalkToBraithwaitNearKurg)]
    [InlineData(MalgraveObjective.TalkToBraithwaitNearKurg, MalgravePhase.ListenToBraithwaitStory)]
    [InlineData(MalgraveObjective.ListenToBraithwaitStory, MalgravePhase.RallySurvivorsAroundInn)]
    [InlineData(MalgraveObjective.RallySurvivorsAroundInn, MalgravePhase.RallySurvivorsNearWreckedShip)]
    [InlineData(MalgraveObjective.RallySurvivorsNearWreckedShip, MalgravePhase.TalkToBraithwaitInTownCenter)]
    [InlineData(MalgraveObjective.TalkToBraithwaitInTownCenter, MalgravePhase.CollectFeedSacksWaterBarrelsAndFoodCrates)]
    public void Exodus_ObjectiveSuccess_AdvancesOpeningRouteOnce(MalgraveObjective objective, MalgravePhase nextPhase)
    {
        MalgraveExodusScript script = CreateExodusScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(nextPhase, invocation.Arguments[0]);
    }

    [Fact]
    public void Exodus_ObjectiveStatus_DoesNotAdvanceUntilSucceeded()
    {
        MalgraveExodusScript script = CreateExodusScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(MalgraveObjective.RallySurvivorsAtTownCenter, PublicEventStatus.Active));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    [Theory]
    [InlineData(MalgravePhase.RallySurvivorsAroundInn, MalgraveObjective.RallySurvivorsAroundInn)]
    [InlineData(MalgravePhase.RallySurvivorsNearWreckedShip, MalgraveObjective.RallySurvivorsNearWreckedShip)]
    public void Exodus_ScriptRallyPhases_DirectCreditCountOneBridge(MalgravePhase phase, MalgraveObjective objective)
    {
        MalgraveExodusScript script = CreateExodusScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(objective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void Exodus_CollectSuppliesPhase_DoesNotDirectCreditSupplyCollection()
    {
        MalgraveExodusScript script = CreateExodusScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)MalgravePhase.CollectFeedSacksWaterBarrelsAndFoodCrates);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(MalgraveObjective.CollectFeedSacksWaterBarrelsAndFoodCrates, activation.Arguments[0]);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void SupplyObject_OnActivateSuccess_UpdatesActiveTargetGroupObjectiveOnce()
    {
        var script = new MalgraveSupplyEntityScript();
        IWorldEntity supply = CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(supply);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(2199u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Theory]
    [InlineData(19918u)]
    [InlineData(19919u)]
    [InlineData(19920u)]
    public void SupplyObjectScript_IsBoundToMappedSupplyCreatures(uint creatureId)
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(MalgraveSupplyEntityScript));

        IScriptFilterSearch supplySearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(creatureId);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(19921u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(supplySearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void Main_OnLoad_SetsLeadCaravanPhase()
    {
        _ = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(MalgravePhase.LeadTheCaravanToFortWestwatchSafely, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_LeadCaravanPhase_ActivatesParentObjective()
    {
        MalgraveEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)MalgravePhase.LeadTheCaravanToFortWestwatchSafely);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(MalgraveObjective.LeadTheCaravanToFortWestwatchSafely, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_LeadCaravanSuccess_FinishesPublicEvent()
    {
        MalgraveEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(MalgraveObjective.LeadTheCaravanToFortWestwatchSafely, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    private static TheMalgraveTrailMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> parentEventProxy,
        out RecordingDispatchProxy<IPublicEvent> exodusEventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        IPublicEvent parentEvent = RecordingDispatchProxy<IPublicEvent>.Create(out parentEventProxy);
        IPublicEvent exodusEvent = RecordingDispatchProxy<IPublicEvent>.Create(out exodusEventProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            return (uint)args[0] switch
            {
                53u => parentEvent,
                56u => exodusEvent,
                _ => null
            };
        });

        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);

        return new TheMalgraveTrailMapScript();
    }

    private static MalgraveExodusScript CreateExodusScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new MalgraveExodusScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static MalgraveEventScript CreateMainScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new MalgraveEventScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static void AssertJoined(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[1]);
    }

    private static IWorldEntity CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return entity;
    }

    private static IPublicEventObjective CreateObjective(MalgraveObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = Convert.ToUInt32(objective)
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }
}
