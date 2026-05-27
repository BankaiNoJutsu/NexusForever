using System.Numerics;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Expedition.Infestation;

namespace NexusForever.Game.Tests.Instances;

public class InfestationEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialProceedOntoCargoShipPhase()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ProceedOntoTheCargoShip, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_ProceedOntoCargoShip_UsesCurrentPlayerCount()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(4u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.ProceedOntoTheCargoShip, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_ProceedOntoCargoShip_CreatesWipGuessedTurnstileTrigger()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            4u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTurnstileTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);

        CreatedTurnstileTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 1005u, 50f, 1005u);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(-0.2391071f, -499.99f, 87.62592f));
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalBay_ActivatesMainAndSideObjectives()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheMedicalBay);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindTheMedicalBay
            && (uint)i.Arguments[1] == 3u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.TagValuableCargo);
    }

    [Fact]
    public void OnPublicEventPhase_SealHullBreaches_ResetsMedicalBayAndActivatesHullObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SealHullBreaches);

        RecordingDispatchProxy<IPublicEvent>.Invocation reset = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ResetObjective)));
        Assert.Equal(PublicEventObjective.FindTheMedicalBay, reset.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.SealHullBreaches, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_HealContaminatedShiphands_ActivatesHealObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HealContaminatedShiphands);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.HealContaminatedShiphand, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.CloseTheShipVents, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.FindTheMedicalBay);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_ParasiteKill_FinishesEvent()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillCyclopeanParasite, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ProceedOntoTheCargoShip, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTurnstileTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1232u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTurnstileTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTurnstileTrigger trigger = CreateTurnstileTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
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

    private static CreatedTurnstileTrigger CreateTurnstileTrigger()
    {
        ITurnstileGridTriggerEntity trigger = RecordingDispatchProxy<ITurnstileGridTriggerEntity>.Create(
            out RecordingDispatchProxy<ITurnstileGridTriggerEntity> triggerProxy);
        return new CreatedTurnstileTrigger(trigger, triggerProxy);
    }

    private static void AssertTriggerInitialised(CreatedTurnstileTrigger trigger, uint triggerId, float range, uint objectId)
    {
        RecordingDispatchProxy<ITurnstileGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(ITurnstileGridTriggerEntity.Initialise)));
        Assert.Equal(triggerId, initialise.Arguments[0]);
        Assert.Equal(range, initialise.Arguments[1]);
        Assert.Equal(objectId, initialise.Arguments[2]);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        CreatedTurnstileTrigger trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger.Instance, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(1232u, position.Info.Entry.Id);
    }

    private sealed record CreatedTurnstileTrigger(
        ITurnstileGridTriggerEntity Instance,
        RecordingDispatchProxy<ITurnstileGridTriggerEntity> Proxy);
}
