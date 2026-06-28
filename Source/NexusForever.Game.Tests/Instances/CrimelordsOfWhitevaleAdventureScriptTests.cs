using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale;
using NexusForever.Script.Template.Filter;
using CrimelordsEventScript = NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale.CrimelordsOfWhitevaleEventScript;
using CrimelordsObjective = NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale.PublicEventObjective;
using CrimelordsPhase = NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class CrimelordsOfWhitevaleAdventureScriptTests
{
    [Fact]
    public void CrimelordsMapScript_OnLoad_CreatesParentPublicEvent()
    {
        CrimelordsOfWhitevaleMapScript script = CreateMapScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out _,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation =
            Assert.Single(managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent)));
        Assert.Equal(146u, invocation.Arguments[0]);
    }

    [Fact]
    public void CrimelordsMapScript_OnAddToMap_JoinsPlayersToParentPublicEvent()
    {
        CrimelordsOfWhitevaleMapScript script = CreateMapScript(
            out _,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out IContentMapInstance contentMap);
        script.OnLoad(contentMap);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[1]);
    }

    [Theory]
    [InlineData(typeof(CrimelordsOfWhitevaleMapScript), 1323u)]
    [InlineData(typeof(CrimelordsEventScript), 146u)]
    public void CrimelordsScripts_AreOwnedByMappedClientRows(Type scriptType, uint ownerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { ownerId }, attribute.Id);
    }

    [Fact]
    public void Main_OnLoad_SetsHoverbikePhase()
    {
        _ = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(CrimelordsPhase.GetOnYourHoverbike, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(CrimelordsPhase.GetOnYourHoverbike, CrimelordsObjective.GetOnYourHoverbike)]
    [InlineData(CrimelordsPhase.FindOutWhoKilledTheBloodScions, CrimelordsObjective.FindOutWhoKilledTheBloodScions)]
    [InlineData(CrimelordsPhase.BecomeTheBiggestGangstersInThermock, CrimelordsObjective.BecomeTheBiggestGangstersInThermock)]
    [InlineData(CrimelordsPhase.ReturnToTheBloodScionsClubhouse, CrimelordsObjective.ReturnToTheBloodScionsClubhouse)]
    [InlineData(CrimelordsPhase.SurviveTheFinalOnslaught, CrimelordsObjective.SurviveTheFinalOnslaught)]
    [InlineData(CrimelordsPhase.AvengeTheBloodScions, CrimelordsObjective.AvengeTheBloodScions)]
    public void Main_Phase_ActivatesMappedObjective(CrimelordsPhase phase, CrimelordsObjective objective)
    {
        CrimelordsEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(CrimelordsObjective.GetOnYourHoverbike, CrimelordsPhase.FindOutWhoKilledTheBloodScions)]
    [InlineData(CrimelordsObjective.FindOutWhoKilledTheBloodScions, CrimelordsPhase.BecomeTheBiggestGangstersInThermock)]
    [InlineData(CrimelordsObjective.BecomeTheBiggestGangstersInThermock, CrimelordsPhase.ReturnToTheBloodScionsClubhouse)]
    [InlineData(CrimelordsObjective.ReturnToTheBloodScionsClubhouse, CrimelordsPhase.SurviveTheFinalOnslaught)]
    [InlineData(CrimelordsObjective.SurviveTheFinalOnslaught, CrimelordsPhase.AvengeTheBloodScions)]
    public void Main_ObjectiveSuccess_StartsNextPhase(CrimelordsObjective objective, CrimelordsPhase nextPhase)
    {
        CrimelordsEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(nextPhase, invocation.Arguments[0]);
    }

    [Fact]
    public void Main_AvengeSuccess_FinishesPublicEvent()
    {
        CrimelordsEventScript script = CreateMainScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(CrimelordsObjective.AvengeTheBloodScions, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    private static CrimelordsOfWhitevaleMapScript CreateMapScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out IContentMapInstance contentMap)
    {
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            return (uint)args[0] == 146u ? publicEvent : null;
        });

        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);

        return new CrimelordsOfWhitevaleMapScript();
    }

    private static CrimelordsEventScript CreateMainScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new CrimelordsEventScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static IPublicEventObjective CreateObjective(CrimelordsObjective objective, PublicEventStatus status)
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
