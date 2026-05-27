using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Raid.UltimateProtogames;

namespace NexusForever.Game.Tests.Instances;

public class UltimateProtogamesRaidEventScriptTests
{
    [Fact]
    public void OnLoad_ActivatesWipDownsizerObjectiveSet()
    {
        _ = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheDownsizer);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.VoltaicConversion);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.Overcharge);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ElectrostaticDynamo);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ElectromagneticInduction);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DownsizerSuccess_FinishesEvent()
    {
        UltimateProtogamesEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.Invocations.Clear();

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.DefeatTheDownsizer,
            PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ChallengeSuccess_DoesNotFinishEvent()
    {
        UltimateProtogamesEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.Invocations.Clear();

        script.OnPublicEventObjectiveStatus(CreateObjective(
            PublicEventObjective.Overcharge,
            PublicEventStatus.Succeeded));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static UltimateProtogamesEventScript CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        var script = new UltimateProtogamesEventScript();
        script.OnLoad(publicEvent);
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
}
