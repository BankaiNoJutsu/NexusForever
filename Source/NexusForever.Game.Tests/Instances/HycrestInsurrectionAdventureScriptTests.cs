using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.TheHycrestInsurrection;
using NexusForever.Script.Template.Filter;
using HycrestEventScript = NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.TheHycrestInsurrectionEventScript;
using HycrestIntroScript = NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.TheHycrestInsurrectionIntroEventScript;
using HycrestObjective = NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.PublicEventObjective;
using HycrestPhase = NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class HycrestInsurrectionAdventureScriptTests
{
    [Fact]
    public void HycrestMapScript_OnLoad_CreatesMainAndIntroPublicEvents()
    {
        TheHycrestInsurrectionMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [419u, 418u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(i => (uint)i.Arguments[0])
                .ToArray());
    }

    [Fact]
    public void HycrestMapScript_OnAddToMap_JoinsPlayersToMainAndIntroPublicEvents()
    {
        TheHycrestInsurrectionMapScript script = CreateMapScript(
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
    [InlineData(typeof(TheHycrestInsurrectionMapScript), 1149u)]
    [InlineData(typeof(HycrestIntroScript), 418u)]
    [InlineData(typeof(HycrestEventScript), 419u)]
    public void HycrestScripts_AreOwnedByMappedClientRows(Type scriptType, uint ownerId)
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
        Assert.Equal(HycrestPhase.ReportToDropShip, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_ReportPhase_ActivatesReportObjective()
    {
        HycrestIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)HycrestPhase.ReportToDropShip);

        AssertObjectiveActivated(eventProxy, HycrestObjective.ReportToDropShip);
    }

    [Fact]
    public void Intro_ReportSuccess_StartsBriefingPhase()
    {
        HycrestIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(HycrestObjective.ReportToDropShip, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(HycrestPhase.ListenToDropShipBriefing, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_BriefingPhase_ActivatesAndCreditsScriptObjective()
    {
        HycrestIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)HycrestPhase.ListenToDropShipBriefing);

        AssertObjectiveActivated(eventProxy, HycrestObjective.ListenToDropShipBriefing);
        AssertObjectiveUpdated(eventProxy, HycrestObjective.ListenToDropShipBriefing, 1);
    }

    [Fact]
    public void Intro_BriefingSuccess_StartsMeetAgentPhase()
    {
        HycrestIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(HycrestObjective.ListenToDropShipBriefing, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(HycrestPhase.MeetWithExileAgent, invocation.Arguments[0]);
    }

    [Fact]
    public void Intro_MeetAgentPhase_ActivatesMeetAgentObjective()
    {
        HycrestIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)HycrestPhase.MeetWithExileAgent);

        AssertObjectiveActivated(eventProxy, HycrestObjective.MeetWithExileAgent);
    }

    [Fact]
    public void Intro_MeetAgentSuccess_FinishesIntroPublicEvent()
    {
        HycrestIntroScript script = CreateIntroScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(HycrestObjective.MeetWithExileAgent, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_OnLoad_SetsLeadPhase()
    {
        _ = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(HycrestPhase.LeadTheHycrestRebelsToVictory, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_LeadPhase_ActivatesParentObjective()
    {
        HycrestEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)HycrestPhase.LeadTheHycrestRebelsToVictory);

        AssertObjectiveActivated(eventProxy, HycrestObjective.LeadTheHycrestRebelsToVictory);
    }

    [Fact]
    public void Main_LeadObjectiveSuccess_FinishesPublicEvent()
    {
        HycrestEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(HycrestObjective.LeadTheHycrestRebelsToVictory, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    private static TheHycrestInsurrectionMapScript CreateMapScript(
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
                419u => mainEvent,
                418u => introEvent,
                _    => null
            };
        });

        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);

        return new TheHycrestInsurrectionMapScript();
    }

    private static HycrestIntroScript CreateIntroScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new HycrestIntroScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static HycrestEventScript CreateMainScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new HycrestEventScript();
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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, HycrestObjective objective)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
    }

    private static void AssertObjectiveUpdated(RecordingDispatchProxy<IPublicEvent> eventProxy, HycrestObjective objective, int count)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
        Assert.Equal(count, invocation.Arguments[1]);
    }

    private static IPublicEventObjective CreateObjective(HycrestObjective objective, PublicEventStatus status)
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
