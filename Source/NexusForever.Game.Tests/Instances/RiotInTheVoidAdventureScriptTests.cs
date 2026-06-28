using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.RiotInTheVoid;
using NexusForever.Script.Template.Filter;
using RiotEventScript = NexusForever.Script.Instance.Adventure.RiotInTheVoid.RiotInTheVoidEventScript;
using RiotIntroScript = NexusForever.Script.Instance.Adventure.RiotInTheVoid.RiotInTheVoidIntroEventScript;
using RiotObjective = NexusForever.Script.Instance.Adventure.RiotInTheVoid.PublicEventObjective;
using RiotPhase = NexusForever.Script.Instance.Adventure.RiotInTheVoid.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class RiotInTheVoidAdventureScriptTests
{
    [Fact]
    public void RiotMapScript_OnLoad_CreatesMainAndIntroPublicEvents()
    {
        RiotInTheVoidMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [179u, 178u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(i => (uint)i.Arguments[0])
                .ToArray());
    }

    [Fact]
    public void RiotMapScript_OnAddToMap_JoinsPlayersToMainAndIntroPublicEvents()
    {
        RiotInTheVoidMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
            out RecordingDispatchProxy<IPublicEvent> introEventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoin(mainEventProxy, player);
        AssertJoin(introEventProxy, player);
    }

    [Theory]
    [InlineData(typeof(RiotInTheVoidMapScript), 1437u)]
    [InlineData(typeof(RiotIntroScript), 178u)]
    [InlineData(typeof(RiotEventScript), 179u)]
    public void RiotScripts_AreOwnedByMappedClientRows(Type scriptType, uint ownerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { ownerId }, attribute.Id);
    }

    [Fact]
    public void Intro_OnLoad_SetsReportPhase()
    {
        _ = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(RiotPhase.ReportToAgentTriphon, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_ReportPhase_ActivatesReportObjective()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)RiotPhase.ReportToAgentTriphon);

        AssertObjectiveActivated(eventProxy, RiotObjective.ReportToAgentTriphon);
    }

    [Fact]
    public void Intro_ReportSuccess_StartsMissionBriefingPhase()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(RiotObjective.ReportToAgentTriphon, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(RiotPhase.ReceiveMissionBriefing, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_MissionBriefingPhase_ActivatesAndCreditsBriefingObjective()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)RiotPhase.ReceiveMissionBriefing);

        AssertObjectiveActivated(eventProxy, RiotObjective.ReceiveMissionBriefing);
        AssertObjectiveUpdated(eventProxy, RiotObjective.ReceiveMissionBriefing, 1);
    }

    [Fact]
    public void Intro_MissionBriefingSuccess_StartsWardenHolostationPhase()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(RiotObjective.ReceiveMissionBriefing, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(RiotPhase.ReportToWardenViaHolostation, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_WardenHolostationPhase_ActivatesHolostationObjective()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)RiotPhase.ReportToWardenViaHolostation);

        AssertObjectiveActivated(eventProxy, RiotObjective.ReportToWardenViaHolostation);
    }

    [Fact]
    public void Intro_WardenHolostationSuccess_StartsSituationBriefingPhase()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(RiotObjective.ReportToWardenViaHolostation, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(RiotPhase.ReceiveSituationBriefing, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_SituationBriefingPhase_ActivatesAndCreditsSituationObjective()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)RiotPhase.ReceiveSituationBriefing);

        AssertObjectiveActivated(eventProxy, RiotObjective.ReceiveSituationBriefing);
        AssertObjectiveUpdated(eventProxy, RiotObjective.ReceiveSituationBriefing, 1);
    }

    [Fact]
    public void Intro_SituationBriefingSuccess_StartsCrossRiotPhase()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(RiotObjective.ReceiveSituationBriefing, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(RiotPhase.CrossRiotAndReachWardensOffice, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_CrossRiotPhase_ActivatesMovementGate()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)RiotPhase.CrossRiotAndReachWardensOffice);

        AssertObjectiveActivated(eventProxy, RiotObjective.CrossRiotAndReachWardensOffice);
    }

    [Fact]
    public void Intro_CrossRiotSuccess_FinishesIntroPublicEvent()
    {
        RiotIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(RiotObjective.CrossRiotAndReachWardensOffice, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_OnLoad_SetsQuellRiotPhase()
    {
        _ = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(RiotPhase.QuellRiotInAstrovoidPrison, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_QuellPhase_ActivatesParentObjective()
    {
        RiotEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)RiotPhase.QuellRiotInAstrovoidPrison);

        AssertObjectiveActivated(eventProxy, RiotObjective.QuellRiotInAstrovoidPrison);
    }

    [Fact]
    public void Main_QuellObjectiveSuccess_FinishesPublicEvent()
    {
        RiotEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(RiotObjective.QuellRiotInAstrovoidPrison, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    private static RiotInTheVoidMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> mainEventProxy,
        out RecordingDispatchProxy<IPublicEvent> introEventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        IPublicEvent mainEvent = RecordingDispatchProxy<IPublicEvent>.Create(out mainEventProxy);
        IPublicEvent introEvent = RecordingDispatchProxy<IPublicEvent>.Create(out introEventProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            return (uint)args[0] switch
            {
                179u => mainEvent,
                178u => introEvent,
                _    => null
            };
        });

        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);

        return new RiotInTheVoidMapScript();
    }

    private static RiotIntroScript CreateIntroScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new RiotIntroScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static RiotEventScript CreateMainScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new RiotEventScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static void AssertJoin(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[1]);
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, RiotObjective objective)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
    }

    private static void AssertObjectiveUpdated(RecordingDispatchProxy<IPublicEvent> eventProxy, RiotObjective objective, int count)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
        Assert.Equal(count, invocation.Arguments[1]);
    }

    private static IPublicEventObjective CreateObjective(RiotObjective objective, PublicEventStatus status)
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
