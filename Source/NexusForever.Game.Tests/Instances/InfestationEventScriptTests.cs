using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Expedition.Infestation;
using NexusForever.Script.Instance.Expedition.Infestation.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

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
    public void OnLoad_ActivatesGoldMedalTimerObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_ProceedOntoCargoShip_UsesCurrentPlayerCount()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedCaptainTolben(4u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ProceedOntoTheCargoShip);
        Assert.Equal(PublicEventObjective.ProceedOntoTheCargoShip, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_ProceedOntoCargoShip_CreatesWipGuessedTurnstileTrigger()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedCaptainTolben(
            4u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTurnstileTrigger> createdTriggers,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);

        CreatedTurnstileTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 1005u, 50f, 1005u);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(-0.2391071f, -499.99f, 87.62592f), 1);
    }

    [Fact]
    public void OnPublicEventPhase_ProceedOntoCargoShip_SpawnsReviewedCaptainTolbenPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedCaptainTolben(
            4u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);

        CreatedNpc captainTolben = Assert.Single(createdNpcs);
        AssertCaptainTolbenModel(captainTolben);
        AssertEntityAddedToMap(mapProxy, captainTolben.Instance, new Vector3(4.41241f, -500f, 14.0114f), 0);
    }

    [Fact]
    public void OnPublicEventPhase_ProceedOntoCargoShip_DoesNotDuplicateReviewedCaptainTolbenSpawn()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedCaptainTolben(
            4u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTurnstileTrigger> createdTriggers,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);
        script.OnPublicEventPhase((uint)PublicEventPhase.ProceedOntoTheCargoShip);

        Assert.Single(createdNpcs);
        Assert.Equal(2, createdTriggers.Count);
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_CloseTheShipVents_ActivatesVentObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.CloseTheShipVents);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CloseTheShipVents);
        Assert.Equal(PublicEventObjective.CloseTheShipVents, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_CloseTheShipVents_SpawnsReviewedVentPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.CloseTheShipVents);

        Assert.Equal(3, createdSimpleEntities.Count);
        AssertInfestationSimpleModel(
            createdSimpleEntities[0],
            1100123001u,
            22493u,
            1158,
            (uint)PublicEventPhase.CloseTheShipVents,
            new Vector3(11f, -500f, 67f),
            27574u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[1],
            1100123002u,
            22493u,
            1158,
            (uint)PublicEventPhase.CloseTheShipVents,
            new Vector3(-5f, -500f, 30f),
            27574u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[2],
            1100123003u,
            22493u,
            1158,
            (uint)PublicEventPhase.CloseTheShipVents,
            new Vector3(-10f, -500f, 78f),
            27574u,
            219);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[0].Instance, new Vector3(11f, -500f, 67f), 0);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[1].Instance, new Vector3(-5f, -500f, 30f), 1);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[2].Instance, new Vector3(-10f, -500f, 78f), 2);
    }

    [Fact]
    public void OnPublicEventPhase_CloseTheShipVents_DoesNotDuplicateReviewedVentPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.CloseTheShipVents);
        script.OnPublicEventPhase((uint)PublicEventPhase.CloseTheShipVents);

        Assert.Equal(3, createdSimpleEntities.Count);
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalBay_ActivatesMainAndSideObjectives()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            3u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
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
    public void OnPublicEventPhase_FindMedicalBay_SpawnsReviewedCargoContainerPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            3u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheMedicalBay);

        Assert.Equal(6, createdSimpleEntities.Count);
        AssertInfestationSimpleModel(
            createdSimpleEntities[0],
            1100123004u,
            69877u,
            2262,
            (uint)PublicEventPhase.FindTheMedicalBay,
            new Vector3(-120f, -498f, 180f),
            27097u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[1],
            1100123005u,
            69877u,
            2262,
            (uint)PublicEventPhase.FindTheMedicalBay,
            new Vector3(-11f, -497f, 113f),
            27097u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[2],
            1100123006u,
            69877u,
            2262,
            (uint)PublicEventPhase.FindTheMedicalBay,
            new Vector3(-89f, -494f, 134f),
            27097u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[3],
            1100123007u,
            69877u,
            2262,
            (uint)PublicEventPhase.FindTheMedicalBay,
            new Vector3(13f, -497f, 138f),
            27097u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[4],
            1100123008u,
            69877u,
            2262,
            (uint)PublicEventPhase.FindTheMedicalBay,
            new Vector3(-88f, -497f, 101f),
            27097u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[5],
            1100123009u,
            69877u,
            2262,
            (uint)PublicEventPhase.FindTheMedicalBay,
            new Vector3(-22f, -515f, 131f),
            27097u,
            219);

        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[0].Instance, new Vector3(-120f, -498f, 180f), 0);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[1].Instance, new Vector3(-11f, -497f, 113f), 1);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[2].Instance, new Vector3(-89f, -494f, 134f), 2);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[3].Instance, new Vector3(13f, -497f, 138f), 3);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[4].Instance, new Vector3(-88f, -497f, 101f), 4);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[5].Instance, new Vector3(-22f, -515f, 131f), 5);
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalBay_DoesNotDuplicateReviewedCargoContainerPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            3u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheMedicalBay);
        script.OnPublicEventPhase((uint)PublicEventPhase.FindTheMedicalBay);

        Assert.Equal(6, createdSimpleEntities.Count);
        Assert.Equal(6, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_SealHullBreaches_ResetsMedicalBayAndActivatesHullObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SealHullBreaches);

        RecordingDispatchProxy<IPublicEvent>.Invocation reset = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ResetObjective)));
        Assert.Equal(PublicEventObjective.FindTheMedicalBay, reset.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.SealHullBreaches);
        Assert.Equal(PublicEventObjective.SealHullBreaches, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SealHullBreaches_SpawnsReviewedHullBreachPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SealHullBreaches);

        Assert.Equal(6, createdSimpleEntities.Count);
        AssertInfestationSimpleModel(
            createdSimpleEntities[0],
            1100123010u,
            36844u,
            2262,
            (uint)PublicEventPhase.SealHullBreaches,
            new Vector3(-141f, -510f, 119f),
            29666u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[1],
            1100123011u,
            36844u,
            2262,
            (uint)PublicEventPhase.SealHullBreaches,
            new Vector3(-166f, -493f, 139f),
            29666u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[2],
            1100123012u,
            36844u,
            2262,
            (uint)PublicEventPhase.SealHullBreaches,
            new Vector3(-132f, -504f, 90f),
            29666u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[3],
            1100123013u,
            36844u,
            2262,
            (uint)PublicEventPhase.SealHullBreaches,
            new Vector3(-132f, -504f, 186f),
            29666u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[4],
            1100123014u,
            36844u,
            2262,
            (uint)PublicEventPhase.SealHullBreaches,
            new Vector3(-83f, -508f, 160f),
            29666u,
            219);
        AssertInfestationSimpleModel(
            createdSimpleEntities[5],
            1100123015u,
            36844u,
            2262,
            (uint)PublicEventPhase.SealHullBreaches,
            new Vector3(-123f, -509f, 105f),
            29666u,
            219);

        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[0].Instance, new Vector3(-141f, -510f, 119f), 0);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[1].Instance, new Vector3(-166f, -493f, 139f), 1);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[2].Instance, new Vector3(-132f, -504f, 90f), 2);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[3].Instance, new Vector3(-132f, -504f, 186f), 3);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[4].Instance, new Vector3(-83f, -508f, 160f), 4);
        AssertEntityAddedToMap(mapProxy, createdSimpleEntities[5].Instance, new Vector3(-123f, -509f, 105f), 5);
    }

    [Fact]
    public void OnPublicEventPhase_SealHullBreaches_DoesNotDuplicateReviewedHullBreachPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SealHullBreaches);
        script.OnPublicEventPhase((uint)PublicEventPhase.SealHullBreaches);

        Assert.Equal(6, createdSimpleEntities.Count);
        Assert.Equal(6, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalSupplies_ActivatesMedicalSuppliesObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindMedicalSupplies);
        Assert.Equal(PublicEventObjective.FindMedicalSupplies, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalSupplies_SpawnsReviewedMedicalSuppliesPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies);

        CreatedSimpleEntity supplies = Assert.Single(createdSimpleEntities);
        AssertInfestationSimpleModel(
            supplies,
            1100123016u,
            22489u,
            1172,
            (uint)PublicEventPhase.FindMedicalSupplies,
            new Vector3(-78f, -525f, 51f),
            27097u,
            219);
        AssertEntityAddedToMap(mapProxy, supplies.Instance, new Vector3(-78f, -525f, 51f), 0);
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalSupplies_DoesNotDuplicateReviewedMedicalSuppliesPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies);
        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies);

        Assert.Single(createdSimpleEntities);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalSupplies2_ActivatesAndSpawnsReviewedMedicalSuppliesPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies2);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindMedicalSupplies);
        Assert.Equal(PublicEventObjective.FindMedicalSupplies, activation.Arguments[0]);

        CreatedSimpleEntity supplies = Assert.Single(createdSimpleEntities);
        AssertInfestationSimpleModel(
            supplies,
            1100123017u,
            22489u,
            1172,
            (uint)PublicEventPhase.FindMedicalSupplies2,
            new Vector3(-78f, -525f, 51f),
            27097u,
            219);
        AssertEntityAddedToMap(mapProxy, supplies.Instance, new Vector3(-78f, -525f, 51f), 0);
    }

    [Fact]
    public void OnPublicEventPhase_FindMedicalSupplies2_DoesNotDuplicateReviewedMedicalSuppliesPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimpleEntity> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies2);
        script.OnPublicEventPhase((uint)PublicEventPhase.FindMedicalSupplies2);

        Assert.Single(createdSimpleEntities);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_DefeatTheAttackOnMedbay_ResetsMedicalSuppliesAndActivatesMedbayAttackObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedNonPlayerPlacements(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTheAttackOnMedbay);

        RecordingDispatchProxy<IPublicEvent>.Invocation reset = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ResetObjective)));
        Assert.Equal(PublicEventObjective.FindMedicalSupplies, reset.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheAttackOnMedbay);
        Assert.Equal(PublicEventObjective.DefeatTheAttackOnMedbay, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatTheAttackOnMedbay_SpawnsReviewedLashingFiendPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedNonPlayerPlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTheAttackOnMedbay);

        CreatedNpc lashingFiend = Assert.Single(createdNpcs);
        AssertInfestationNonPlayerModel(
            lashingFiend,
            1100123022u,
            69871u,
            1172,
            (uint)PublicEventPhase.DefeatTheAttackOnMedbay,
            new Vector3(-58f, -525f, 28f),
            21629u,
            0,
            218);
        AssertEntityAddedToMap(mapProxy, lashingFiend.Instance, new Vector3(-58f, -525f, 28f), 0);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatTheAttackOnMedbay_DoesNotDuplicateReviewedLashingFiendPlacement()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedNonPlayerPlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTheAttackOnMedbay);
        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTheAttackOnMedbay);

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_HealContaminatedShiphands_ActivatesHealObjective()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedNonPlayerPlacements(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HealContaminatedShiphands);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.HealContaminatedShiphand);
        Assert.Equal(PublicEventObjective.HealContaminatedShiphand, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_HealContaminatedShiphands_SpawnsReviewedContaminatedShiphandPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedNonPlayerPlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HealContaminatedShiphands);

        Assert.Equal(4, createdNpcs.Count);
        AssertInfestationNonPlayerModel(
            createdNpcs[0],
            1100123018u,
            23837u,
            2262,
            (uint)PublicEventPhase.HealContaminatedShiphands,
            new Vector3(-71f, -497f, 114f),
            24148u,
            8186,
            843);
        AssertInfestationNonPlayerModel(
            createdNpcs[1],
            1100123019u,
            23837u,
            2262,
            (uint)PublicEventPhase.HealContaminatedShiphands,
            new Vector3(-100f, -512f, 147f),
            24148u,
            8186,
            843);
        AssertInfestationNonPlayerModel(
            createdNpcs[2],
            1100123020u,
            23837u,
            2262,
            (uint)PublicEventPhase.HealContaminatedShiphands,
            new Vector3(-53f, -514f, 146f),
            24148u,
            8186,
            843);
        AssertInfestationNonPlayerModel(
            createdNpcs[3],
            1100123021u,
            23837u,
            2262,
            (uint)PublicEventPhase.HealContaminatedShiphands,
            new Vector3(3f, -497f, 111f),
            24148u,
            8186,
            843);

        AssertEntityAddedToMap(mapProxy, createdNpcs[0].Instance, new Vector3(-71f, -497f, 114f), 0);
        AssertEntityAddedToMap(mapProxy, createdNpcs[1].Instance, new Vector3(-100f, -512f, 147f), 1);
        AssertEntityAddedToMap(mapProxy, createdNpcs[2].Instance, new Vector3(-53f, -514f, 146f), 2);
        AssertEntityAddedToMap(mapProxy, createdNpcs[3].Instance, new Vector3(3f, -497f, 111f), 3);
    }

    [Fact]
    public void OnPublicEventPhase_HealContaminatedShiphands_DoesNotDuplicateReviewedContaminatedShiphandPlacements()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedNonPlayerPlacements(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.HealContaminatedShiphands);
        script.OnPublicEventPhase((uint)PublicEventPhase.HealContaminatedShiphands);

        Assert.Equal(4, createdNpcs.Count);
        Assert.Equal(4, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_KillParasites_ActivatesParasiteObjectives()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillParasites);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillLumberingParasites);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillCyclopeanParasite);
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
    public void OnPublicEventObjectiveStatus_SealHullBreachesSuccess_AdvancesToSecondMedicalBayPhase()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SealHullBreaches, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.FindTheMedicalBay2);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MedicalSuppliesSuccess_AdvancesToHealPhase()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindMedicalSupplies, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.HealContaminatedShiphands);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_MedbayAttackSuccess_AdvancesToSecondMedicalSuppliesPhase()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheAttackOnMedbay, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.FindMedicalSupplies2);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_HealShiphandSuccess_AdvancesToKillParasitesPhase()
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.HealContaminatedShiphand, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.KillParasites);
    }

    [Theory]
    [InlineData(PublicEventObjective.KillLumberingParasites)]
    [InlineData(PublicEventObjective.KillCyclopeanParasite)]
    public void OnPublicEventObjectiveStatus_ParasiteKill_CreditsGoldTimerAndFinishesEvent(PublicEventObjective parasiteObjective)
    {
        var script = new InfestationEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(parasiteObjective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);

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

    [Fact]
    public void ShipVentScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ShipVentEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 22493u }, attribute.CreatureId);
    }

    [Fact]
    public void MedicalSuppliesScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(MedicalSuppliesEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 22489u }, attribute.CreatureId);
    }

    [Fact]
    public void HullBreachScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(HullBreachEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 36844u }, attribute.CreatureId);
    }

    [Fact]
    public void CargoContainerScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CargoContainerEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 69877u }, attribute.CreatureId);
    }

    [Theory]
    [InlineData(typeof(ShipVentEntityScript))]
    [InlineData(typeof(CargoContainerEntityScript))]
    [InlineData(typeof(HullBreachEntityScript))]
    [InlineData(typeof(MedicalSuppliesEntityScript))]
    public void ReviewedSimplePlacementScripts_CanBindToSimpleEntities(Type scriptType)
    {
        Assert.True(typeof(IOwnedScript<ISimpleEntity>).IsAssignableFrom(scriptType));
    }

    [Fact]
    public void ContaminatedShiphandScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ContaminatedShiphandEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 23837u }, attribute.CreatureId);
    }

    [Theory]
    [InlineData(typeof(LumberingParasiteEntityScript), 22669u)]
    [InlineData(typeof(CyclopeanParasiteEntityScript), 26833u)]
    [InlineData(typeof(LashingFiendEntityScript), 69871u)]
    public void DeathCreditScripts_AreBoundToMappedCreatures(Type scriptType, uint creatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { creatureId }, attribute.CreatureId);
    }

    [Fact]
    public void ShipVent_OnActivateSuccess_UpdatesActiveChecklistTargetGroupObjectiveOnce()
    {
        var script = new ShipVentEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity vent);

        script.OnLoad(vent);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(2324u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Fact]
    public void MedicalSupplies_OnActivateSuccess_UpdatesActiveTargetGroupObjectiveOnce()
    {
        var script = new MedicalSuppliesEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity medicalSupplies);

        script.OnLoad(medicalSupplies);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(2323u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void HullBreach_OnActivateSuccess_UpdatesActiveChecklistTargetGroupObjectiveOnce()
    {
        var script = new HullBreachEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity hullBreach);

        script.OnLoad(hullBreach);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(4516u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Fact]
    public void CargoContainer_OnActivateSuccess_UpdatesActiveChecklistTargetGroupObjectiveOnce()
    {
        var script = new CargoContainerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity cargoContainer);

        script.OnLoad(cargoContainer);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(12593u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Fact]
    public void ContaminatedShiphand_OnActivateSuccess_UpdatesHealObjectiveOnce()
    {
        var script = new ContaminatedShiphandEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity contaminatedShiphand);

        script.OnLoad(contaminatedShiphand);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.HealContaminatedShiphand, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
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

    private static IPublicEvent CreatePublicEventWithReviewedCaptainTolben(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTurnstileTrigger> createdTriggers,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1232u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTurnstileTrigger> triggers = [];
        List<CreatedNpc> npcs = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            if (npcs.Count == 0)
            {
                CreatedNpc npc = CreateNpc();
                npcs.Add(npc);
                return npc.Instance;
            }

            CreatedTurnstileTrigger trigger = CreateTurnstileTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        createdNpcs = npcs;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithReviewedSimplePlacements(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedSimpleEntity> createdSimpleEntities)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1232u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedSimpleEntity> simpleEntities = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedSimpleEntity simpleEntity = CreateSimpleEntity();
            simpleEntities.Add(simpleEntity);
            return simpleEntity.Instance;
        });

        createdSimpleEntities = simpleEntities;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithReviewedNonPlayerPlacements(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1232u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedNpc> npcs = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedNpc npc = CreateNpc();
            npcs.Add(npc);
            return npc.Instance;
        });

        createdNpcs = npcs;
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

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static CreatedTurnstileTrigger CreateTurnstileTrigger()
    {
        ITurnstileGridTriggerEntity trigger = RecordingDispatchProxy<ITurnstileGridTriggerEntity>.Create(
            out RecordingDispatchProxy<ITurnstileGridTriggerEntity> triggerProxy);
        return new CreatedTurnstileTrigger(trigger, triggerProxy);
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static CreatedSimpleEntity CreateSimpleEntity()
    {
        ISimpleEntity simpleEntity = RecordingDispatchProxy<ISimpleEntity>.Create(
            out RecordingDispatchProxy<ISimpleEntity> simpleEntityProxy);
        return new CreatedSimpleEntity(simpleEntity, simpleEntityProxy);
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
        AssertTriggerAddedToMap(mapProxy, trigger, expectedPosition, 0);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        CreatedTurnstileTrigger trigger,
        Vector3 expectedPosition,
        int invocationIndex)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd =
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd))[invocationIndex];
        Assert.Same(trigger.Instance, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(1232u, position.Info.Entry.Id);
    }

    private static void AssertEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition,
        int invocationIndex)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd =
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd))[invocationIndex];
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(1232u, position.Info.Entry.Id);
    }

    private static void AssertCaptainTolbenModel(CreatedNpc npc)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300016u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(71764u, model.Creature);
        Assert.Equal((ushort)1232u, model.World);
        Assert.Equal((ushort)1158u, model.Area);
        Assert.Equal(4.41241f, model.X);
        Assert.Equal(-500f, model.Y);
        Assert.Equal(14.0114f, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(28578u, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Null(model.EntityEvent);
        Assert.Empty(model.EntityScript);
        Assert.Collection(model.EntityStat,
            stat =>
            {
                Assert.Equal((byte)Stat.Health, stat.Stat);
                Assert.Equal(1f, stat.Value);
            });
    }

    private static void AssertInfestationSimpleModel(
        CreatedSimpleEntity simpleEntity,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        uint displayInfo,
        ushort factionId)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            simpleEntity.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1232u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal(95u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertInfestationNonPlayerModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        uint displayInfo,
        ushort outfitInfo,
        ushort factionId)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1232u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal(outfitInfo, model.OutfitInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal(95u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private sealed record CreatedTurnstileTrigger(
        ITurnstileGridTriggerEntity Instance,
        RecordingDispatchProxy<ITurnstileGridTriggerEntity> Proxy);

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimpleEntity(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);
}
