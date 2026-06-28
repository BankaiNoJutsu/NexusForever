using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.WorldStory.JourneyIntoOMNICore1;

namespace NexusForever.Game.Tests.Instances;

public class JourneyIntoOMNICore1EventScriptTests
{
    [Fact]
    public void OnLoad_StartsAtBelleAndAxisSpeakPhase()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.SpeakWithBelleAndAxis, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SpeakWithBelleAndAxis_ActivatesBuild16042TalkObjectives()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SpeakWithBelleAndAxis);

        List<object> objectives = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .Select(i => i.Arguments[0])
            .ToList();
        Assert.Contains(PublicEventObjective.SpeakWithBelleWalker, objectives);
        Assert.Contains(PublicEventObjective.SpeakWithAxisPheydra, objectives);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_SpeakObjectives_AdvanceOnlyAfterBothSucceeded()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithBelleWalker, PublicEventStatus.Succeeded));

        Assert.DoesNotContain(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.ListenToBelleAndAxis);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithAxisPheydra, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.ListenToBelleAndAxis);
    }

    [Fact]
    public void OnPublicEventPhase_ListenToBelleAndAxis_ActivatesAndCreditsScriptRows()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ListenToBelleAndAxis);

        List<object> activatedObjectives = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .Select(i => i.Arguments[0])
            .ToList();
        Assert.Contains(PublicEventObjective.ListenToBelleWalker, activatedObjectives);
        Assert.Contains(PublicEventObjective.ListenToAxisPheydra, activatedObjectives);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> updates = eventProxy
            .GetInvocations(nameof(IPublicEvent.UpdateObjective))
            .ToList();
        Assert.Contains(updates, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ListenToBelleWalker
            && (int)i.Arguments[1] == 1);
        Assert.Contains(updates, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ListenToAxisPheydra
            && (int)i.Arguments[1] == 1);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ListenObjectives_AdvanceToChoosePathGateOnce()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ListenToBelleWalker, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ListenToAxisPheydra, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ListenToAxisPheydra, PublicEventStatus.Succeeded));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.ChoosePath);
    }

    [Fact]
    public void OnPublicEventPhase_ChoosePath_ActivatesUnresolvedBuild16042BranchGate()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ChoosePath);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.ChoosePathAOrB, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ActiveObjective_DoesNotAdvance()
    {
        var script = new JourneyIntoOMNICore1EventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithBelleWalker, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
    }

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective publicEventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(
            out RecordingDispatchProxy<IPublicEventObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return publicEventObjective;
    }
}
