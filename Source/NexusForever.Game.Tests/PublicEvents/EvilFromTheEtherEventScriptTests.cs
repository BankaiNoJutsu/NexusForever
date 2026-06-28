using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.PublicEvents;

public class EvilFromTheEtherEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialTalkToCaptainWeirPhase()
    {
        _ = CreateScript(out var publicEventProxy, out _, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.TalkToCaptainWeir, invocation.Arguments[0]);
    }

    [Fact]
    public void OnLoad_ActivatesGoldMedalTimerObjective()
    {
        _ = CreateScript(out var publicEventProxy, out _, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.CompleteWithinGoldTimer, activation.Arguments[0]);
    }

    [Fact]
    public void TalkToCaptainWeir_ActivatesInitialTalkObjective()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainWeir);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TalkToCaptainWeir, activation.Arguments[0]);
    }

    [Fact]
    public void GoToAirlock_ActivatesGroupObjectiveAndCreatesWipGuessedGatherTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedWorldLocationTrigger trigger);

        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 4u);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToAirlock);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToAirlock, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
        AssertWorldLocationTrigger(trigger, 50278u, 8242u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-396.43555f, -840.7188f, 119.74138f));
    }

    [Fact]
    public void OpenMedbay_ActivatesObjectivesTeleportsPlayersAndCleansGatherEntities()
    {
        RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy;
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy, out questManagerProxy);
        IPlayer player = CreatePlayer(42ul, out var playerProxy);
        IWorldEntity gatherRing = CreateWorldEntity(PublicEventCreature.GatherRing, 501u, out var gatherRingProxy);
        IWorldLocationVolumeGridTriggerEntity gatherTrigger = CreateWorldLocationTrigger(50278u, 502u, out var gatherTriggerProxy);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out var communicatorMessageProxy);

        script.OnAddToMap(gatherRing);
        script.OnAddToMap(gatherTrigger);

        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args =>
        {
            uint guid = (uint)args[0];
            return guid switch
            {
                501u => gatherRing,
                502u => gatherTrigger,
                _    => null
            };
        });
        questManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        script.OnPublicEventPhase((uint)PublicEventPhase.OpenMedbay);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.OpenMedbay);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DownloadCrewLogs);

        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)));
        Assert.Equal(new Vector3(53.180374f, -852.86273f, -91.41684f), teleport.Arguments[0]);

        Assert.Single(gatherRingProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Single(gatherTriggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation messageLookup = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.CaptainWeir2, messageLookup.Arguments[0]);
        Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Fact]
    public void RepairDoor_ActivatesObjectiveAndCleansMedbayDoorControl()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IWorldEntity control = CreateWorldEntity(PublicEventCreature.MedbayDoorControl, 601u, out var controlProxy);

        script.OnAddToMap(control);
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args => (uint)args[0] == 601u ? control : null);

        script.OnPublicEventPhase((uint)PublicEventPhase.RepairDoor);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.RepairDoor, activation.Arguments[0]);
        Assert.Single(controlProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(PublicEventPhase.ActivateMedbayGenerator, PublicEventObjective.ActivateMedbayGenerator)]
    [InlineData(PublicEventPhase.KillSecurityChiefKondovich, PublicEventObjective.KillSecurityChiefKondovich)]
    [InlineData(PublicEventPhase.ActivateSelfDestruct, PublicEventObjective.ActivateSelfDestruct)]
    public void SimpleObjectivePhases_ActivateMappedObjective(
        PublicEventPhase phase,
        PublicEventObjective objective)
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
        Assert.Equal(objective, activation.Arguments[0]);
    }

    [Fact]
    public void GoToPrimaryPowerPlant_SeedsPlayersAlreadyInsideDoorRange()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IDoorEntity door = CreateDoor(7059788ul, 321u, [11ul, 12ul]);

        script.OnAddToMap(door);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 3u);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToPrimaryPowerPlant);

        RecordingDispatchProxy<IPublicEvent>.Invocation activateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, activateInvocation.Arguments[0]);
        Assert.Equal(3u, activateInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation updateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, updateInvocation.Arguments[0]);
        Assert.Equal(2, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void GoToPrimaryPowerPlant2_ReSeedsPlayersAfterObjectiveReset()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IDoorEntity door = CreateDoor(7024518ul, 654u, [21ul]);

        script.OnAddToMap(door);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToPrimaryPowerPlant2);

        RecordingDispatchProxy<IPublicEvent>.Invocation resetInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ResetObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, resetInvocation.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation activateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, activateInvocation.Arguments[0]);
        Assert.Equal(0u, activateInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation updateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void RestartMainGenerators_ActivatesAggregateAndChildObjectivesAndOpensDoor()
    {
        RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy;
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy, out questManagerProxy);
        IDoorEntity door = CreateDoor(7024518ul, 765u, [], out var doorProxy);

        script.OnAddToMap(door);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.RestartMainGenerators);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.RestartMainGenerators);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.RestoreGeneratorAlphaPower);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.RestoreGeneratorBetaPower);

        Assert.Single(doorProxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));

        IReadOnlyList<RecordingDispatchProxy<IGlobalQuestManager>.Invocation> messageLookups =
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage));
        Assert.Contains(messageLookups, i => (CommunicatorMessage)i.Arguments[0] == CommunicatorMessage.InsaneCrewChief);
        Assert.Contains(messageLookups, i => (CommunicatorMessage)i.Arguments[0] == CommunicatorMessage.CaptainWeir10);
    }

    [Fact]
    public void DefeatEthericOrganisms_ActivatesObjectiveAndQueuesCinematic()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IPlayer player = CreatePlayer(42ul, out var playerProxy);
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(
            out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatEthericOrganisms);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatEthericOrganisms, activation.Arguments[0]);
        Assert.Single(cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
    }

    [Fact]
    public void DefeatEthericOrganisms2_ActivatesWaveObjective()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatEthericOrganisms2);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatEthericOrganisms2, activation.Arguments[0]);
    }

    [Fact]
    public void DefeatKatjaZarkhov_ActivatesKatjaAndEtherChargedRavenousObjectives()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatKatjaZarkhov);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatKatjaZarkhov);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillEtherChargedRavenous);
    }

    [Fact]
    public void GatherAroundTeleporter_ActivatesObjectiveAndCreatesRetailGatherTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedWorldLocationTrigger trigger);

        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 3u);

        script.OnPublicEventPhase((uint)PublicEventPhase.GatherAroundTeleporter);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GatherAroundTeleporter, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
        AssertWorldLocationTrigger(trigger, 50632u, 8312u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(24.1576f, -840.142f, 173.326f));
    }

    [Fact]
    public void FindTeleporter_ActivatesObjectiveAndCreatesTurnstileTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedTurnstileTrigger trigger);

        script.OnPublicEventPhase((uint)PublicEventPhase.FindTeleporter);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.FindTeleporter, activation.Arguments[0]);
        AssertTurnstileTriggerInitialised(trigger, 8307u, 15f, 8307u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-15.32f, -840.73f, 150.96f));
    }

    [Fact]
    public void TeleportToUpperDeck_ActivatesObjectiveAndCreatesUpperDeckTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedGridTrigger trigger);

        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 2u);

        script.OnPublicEventPhase((uint)PublicEventPhase.TeleportToUpperDeck);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TeleportToUpperDeck, activation.Arguments[0]);
        Assert.Equal(2u, activation.Arguments[1]);
        AssertGridTriggerInitialised(trigger, 8242u, 3f);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(37.27052f, -840.065f, 173.36299f));
    }

    [Fact]
    public void GatherInBridgeAccessHall_OpensDoorAndCreatesGatherTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedWorldLocationTrigger trigger);
        IDoorEntity upperDeckDoor = CreateUpperDeckDoor(PublicEventCreature.UpperDeckDoor1, 701u, out var doorProxy);

        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 3u);
        script.OnAddToMap(upperDeckDoor);
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args => (uint)args[0] == 701u ? upperDeckDoor : null);

        script.OnPublicEventPhase((uint)PublicEventPhase.GatherInBridgeAccessHall);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GatherInBridgeAccessHall, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
        Assert.Single(doorProxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));
        AssertWorldLocationTrigger(trigger, 50348u, 8260u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-53.3373f, -845.091f, 215.584f));
    }

    [Fact]
    public void DefeatTetheredOrganisms_ActivatesObjectiveAndCleansFirstGatherMarker()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IWorldEntity gatherMarker = CreateWorldEntity(PublicEventCreature.GatherMarker1, 801u, out var markerProxy);

        script.OnAddToMap(gatherMarker);
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args => (uint)args[0] == 801u ? gatherMarker : null);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTetheredOrganisms);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatTetheredOrganisms, activation.Arguments[0]);
        Assert.Single(markerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void GatherOnTheShadesBridge_OpensDoorsAndCreatesGatherTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedWorldLocationTrigger trigger);
        IDoorEntity door2 = CreateUpperDeckDoor(PublicEventCreature.UpperDeckDoor2, 802u, out var door2Proxy);
        IDoorEntity door3 = CreateUpperDeckDoor(PublicEventCreature.UpperDeckDoor3, 803u, out var door3Proxy);

        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 4u);
        script.OnAddToMap(door2);
        script.OnAddToMap(door3);
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args =>
        {
            uint guid = (uint)args[0];
            return guid switch
            {
                802u => door2,
                803u => door3,
                _    => null
            };
        });

        script.OnPublicEventPhase((uint)PublicEventPhase.GatherOnTheShadesBridge);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GatherOnTheShadesBridge, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
        Assert.Single(door2Proxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));
        Assert.Single(door3Proxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));
        AssertWorldLocationTrigger(trigger, 50349u, 8261u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-53.3725f, -842.341f, 282.618f));
    }

    [Fact]
    public void DefeatTetheredOrganisms2_ActivatesObjectiveAndCleansSecondGatherMarker()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IWorldEntity gatherMarker = CreateWorldEntity(PublicEventCreature.GatherMarker2, 901u, out var markerProxy);

        script.OnAddToMap(gatherMarker);
        mapProxy.SetMethodHandler(nameof(IMapInstance.GetEntity), args => (uint)args[0] == 901u ? gatherMarker : null);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTetheredOrganisms2);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatTetheredOrganisms2, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
        Assert.Single(markerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void EscapeToTheTeleporter_ActivatesObjectiveAndCreatesCaptainWeirTrigger()
    {
        EvilFromTheEtherEventScript script = CreateScript(
            out var publicEventProxy,
            out var mapProxy,
            out CreatedGridTrigger trigger);

        script.OnPublicEventPhase((uint)PublicEventPhase.EscapeToTheTeleporter);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.EscapeToTheTeleporter, activation.Arguments[0]);
        AssertGridTriggerInitialised(trigger, 8243u, 3f);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(-53.353714f, -845.00726f, 164.51099f));
    }

    [Fact]
    public void ObjectiveStatus_WaveFightAdvancesThroughTeleporterGatherStep()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatEthericOrganisms2, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.GatherAroundTeleporter, PublicEventStatus.Succeeded));

        IReadOnlyList<RecordingDispatchProxy<IPublicEvent>.Invocation> phaseChanges =
            publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase));
        Assert.Contains(phaseChanges, i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.GatherAroundTeleporter);
        Assert.Contains(phaseChanges, i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.TeleportToUpperDeck);
    }

    [Fact]
    public void PickUpDriveSchematics_ActivatesObjectivesAndSpawnsReviewedDriveSparks()
    {
        EvilFromTheEtherEventScript script = CreateScriptWithReviewedDriveSparks(
            out var publicEventProxy,
            out var mapProxy,
            out List<CreatedNpc> createdNpcs);

        script.OnPublicEventPhase((uint)PublicEventPhase.PickUpDriveSchematics);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DriveDiagnostics);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.PickUpDriveSchematics);

        Assert.Equal(DriveSparkExpectations.Length, createdNpcs.Count);
        for (int i = 0; i < DriveSparkExpectations.Length; i++)
        {
            (uint entityId, Vector3 position) = DriveSparkExpectations[i];
            AssertDriveSparkModel(createdNpcs[i], entityId, position);
            AssertEntityAddedToMap(mapProxy, createdNpcs[i].Instance, position, i);
        }
    }

    [Fact]
    public void PickUpDriveSchematics_DoesNotDuplicateReviewedDriveSparkSpawns()
    {
        EvilFromTheEtherEventScript script = CreateScriptWithReviewedDriveSparks(
            out _,
            out var mapProxy,
            out List<CreatedNpc> createdNpcs);

        script.OnPublicEventPhase((uint)PublicEventPhase.PickUpDriveSchematics);
        script.OnPublicEventPhase((uint)PublicEventPhase.PickUpDriveSchematics);

        Assert.Equal(DriveSparkExpectations.Length, createdNpcs.Count);
        Assert.Equal(DriveSparkExpectations.Length, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void ObjectiveStatus_EndPhasePickupWaitsForSchematicsAndDriveDiagnostics()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.PickUpDriveSchematics, PublicEventStatus.Succeeded));

        Assert.DoesNotContain(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.EscapeToTheTeleporter);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DriveDiagnostics, PublicEventStatus.Succeeded));

        Assert.Contains(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.EscapeToTheTeleporter);
    }

    [Fact]
    public void ObjectiveStatus_KatjaFightWaitsForKatjaAndEtherChargedRavenous()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatKatjaZarkhov, PublicEventStatus.Succeeded));

        Assert.DoesNotContain(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.PickUpDriveSchematics);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillEtherChargedRavenous, PublicEventStatus.Succeeded));

        Assert.Contains(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.PickUpDriveSchematics);
    }

    [Fact]
    public void ObjectiveStatus_KatjaFightWaitsForEtherChargedRavenousAndKatja()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillEtherChargedRavenous, PublicEventStatus.Succeeded));

        Assert.DoesNotContain(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.PickUpDriveSchematics);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatKatjaZarkhov, PublicEventStatus.Succeeded));

        Assert.Contains(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.PickUpDriveSchematics);
    }

    [Fact]
    public void ObjectiveStatus_SuccessAdvancesBranchPhaseChainAndFinishesFinalStep()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainWeir, PublicEventStatus.Succeeded));
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainWeir2, PublicEventStatus.Succeeded));

        Assert.Contains(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.GoToAirlock);

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation goldTimerCredit = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.CompleteWithinGoldTimer, goldTimerCredit.Arguments[0]);
        Assert.Equal(0, goldTimerCredit.Arguments[1]);
    }

    [Fact]
    public void ObjectiveStatus_IncompleteDoesNotAdvance()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainWeir, PublicEventStatus.Active));

        Assert.Empty(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(publicEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void TalkToCaptainWeir2_ActivatesFinalTalkObjective()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainWeir2);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.TalkToCaptainWeir2, activation.Arguments[0]);
    }

    [Fact]
    public void CaptainWeirScript_IsBoundToCaptainWeirCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CaptainWeirEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 70999u }, attribute.CreatureId);
    }

    [Fact]
    public void CaptainWeir_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayer()
    {
        var script = new CaptainWeirEntityScript();
        IPlayer player = CreatePlayer(77ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity captainWeir);

        script.OnLoad(captainWeir);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(12996u, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    [Fact]
    public void CaptainWeir_OnActivateSuccess_DoesNotSuppressLaterTalkPhaseReuse()
    {
        var script = new CaptainWeirEntityScript();
        IPlayer player = CreatePlayer(77ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity captainWeir);

        script.OnLoad(captainWeir);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        Assert.Equal(2, publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)).Count());
    }

    [Theory]
    [InlineData(PublicEventPhase.TalkToCaptainWeir, CommunicatorMessage.CaptainWeir1)]
    [InlineData(PublicEventPhase.ScavengeSpareParts, CommunicatorMessage.CaptainWeir4)]
    [InlineData(PublicEventPhase.DefeatEthericOrganisms, CommunicatorMessage.CaptainWeir12)]
    public void CinematicFinish_SendsWipGuessedPhaseMessage(PublicEventPhase phase, CommunicatorMessage expectedMessage)
    {
        RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy;
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out _, out questManagerProxy);
        IPlayer player = CreatePlayer(99ul, out _);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out var communicatorMessageProxy);

        publicEventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)phase);
        questManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        script.OnCinematicFinish(player, 1234u);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation messageLookup = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(expectedMessage, messageLookup.Arguments[0]);
        Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        bool preserveLoadInvocations = false)
    {
        return CreateScript(out publicEventProxy, out mapProxy, out _, preserveLoadInvocations);
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out RecordingDispatchProxy<IGlobalQuestManager> questManagerProxy,
        bool preserveLoadInvocations = false)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IGlobalQuestManager questManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out questManagerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);

        publicEventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3404u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        var script = new EvilFromTheEtherEventScript(cinematicFactory, questManager);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            publicEventProxy.Invocations.Clear();
        return script;
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedWorldLocationTrigger createdTrigger)
    {
        EvilFromTheEtherEventScript script = CreateScript(out publicEventProxy, out mapProxy);
        CreatedWorldLocationTrigger trigger = CreateWorldLocationTrigger();
        publicEventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => trigger.Instance);
        createdTrigger = trigger;
        return script;
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedGridTrigger createdTrigger)
    {
        EvilFromTheEtherEventScript script = CreateScript(out publicEventProxy, out mapProxy);
        CreatedGridTrigger trigger = CreateGridTrigger();
        publicEventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => trigger.Instance);
        createdTrigger = trigger;
        return script;
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedTurnstileTrigger createdTrigger)
    {
        EvilFromTheEtherEventScript script = CreateScript(out publicEventProxy, out mapProxy);
        CreatedTurnstileTrigger trigger = CreateTurnstileTrigger();
        publicEventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => trigger.Instance);
        createdTrigger = trigger;
        return script;
    }

    private static EvilFromTheEtherEventScript CreateScriptWithReviewedDriveSparks(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        EvilFromTheEtherEventScript script = CreateScript(out publicEventProxy, out mapProxy);

        List<CreatedNpc> npcs = [];
        publicEventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedNpc npc = CreateNpc();
            npcs.Add(npc);
            return npc.Instance;
        });

        createdNpcs = npcs;
        return script;
    }

    private static IDoorEntity CreateUpperDeckDoor(
        PublicEventCreature creature,
        uint guid,
        out RecordingDispatchProxy<IDoorEntity> doorProxy)
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out doorProxy);
        doorProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        doorProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)creature);
        return door;
    }

    private static IDoorEntity CreateDoor(ulong activePropId, uint guid, ulong[] playerCharacterIds)
    {
        return CreateDoor(activePropId, guid, playerCharacterIds, out _);
    }

    private static IDoorEntity CreateDoor(
        ulong activePropId,
        uint guid,
        ulong[] playerCharacterIds,
        out RecordingDispatchProxy<IDoorEntity> doorProxy)
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out doorProxy);
        doorProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        doorProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)PublicEventCreature.Door);
        doorProxy.SetProperty(nameof(IWorldEntity.ActivePropId), activePropId);
        doorProxy.SetMethodReturn(nameof(IGridEntity.GetInRange), playerCharacterIds.Select(CreatePlayer).ToArray());
        return door;
    }

    private static IWorldEntity CreateWorldEntity(
        PublicEventCreature creature,
        uint guid,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)creature);
        return entity;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity worldEntity)
    {
        worldEntity = RecordingDispatchProxy<IWorldEntity>.Create(out var worldEntityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        worldEntityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IWorldLocationVolumeGridTriggerEntity CreateWorldLocationTrigger(
        uint worldLocationId,
        uint guid,
        out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy)
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(out triggerProxy);
        triggerProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        triggerProxy.SetProperty(nameof(IWorldLocationVolumeGridTriggerEntity.Entry), new WorldLocation2Entry
        {
            Id = worldLocationId
        });
        return trigger;
    }

    private static CreatedWorldLocationTrigger CreateWorldLocationTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedWorldLocationTrigger(trigger, triggerProxy);
    }

    private static CreatedGridTrigger CreateGridTrigger()
    {
        IGridTriggerEntity trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        return new CreatedGridTrigger(trigger, triggerProxy);
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

    private static IPlayer CreatePlayer(ulong characterId)
    {
        return CreatePlayer(characterId, out _);
    }

    private static IPlayer CreatePlayer(ulong characterId, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }

    private static void AssertWorldLocationTrigger(CreatedWorldLocationTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
    }

    private static void AssertGridTriggerInitialised(CreatedGridTrigger trigger, uint triggerId, float range)
    {
        RecordingDispatchProxy<IGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IGridTriggerEntity.Initialise)));
        Assert.Equal(triggerId, initialise.Arguments[0]);
        Assert.Equal(range, initialise.Arguments[1]);
    }

    private static void AssertTurnstileTriggerInitialised(CreatedTurnstileTrigger trigger, uint triggerId, float range, uint objectId)
    {
        RecordingDispatchProxy<ITurnstileGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(ITurnstileGridTriggerEntity.Initialise)));
        Assert.Equal(triggerId, initialise.Arguments[0]);
        Assert.Equal(range, initialise.Arguments[1]);
        Assert.Equal(objectId, initialise.Arguments[2]);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3404u, position.Info.Entry.Id);
    }

    private static void AssertDriveSparkModel(CreatedNpc npc, uint entityId, Vector3 position)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(71847u, model.Creature);
        Assert.Equal((ushort)3404u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(-3.1415925f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(24324u, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(781u, model.EntityEvent.EventId);
        Assert.Equal(23u, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition,
        int invocationIndex)
    {
        List<RecordingDispatchProxy<IMapInstance>.Invocation> enqueueAdds =
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).ToList();
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = enqueueAdds[invocationIndex];
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3404u, position.Info.Entry.Id);
    }

    private sealed record CreatedWorldLocationTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedGridTrigger(
        IGridTriggerEntity Instance,
        RecordingDispatchProxy<IGridTriggerEntity> Proxy);

    private sealed record CreatedTurnstileTrigger(
        ITurnstileGridTriggerEntity Instance,
        RecordingDispatchProxy<ITurnstileGridTriggerEntity> Proxy);

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private static readonly (uint EntityId, Vector3 Position)[] DriveSparkExpectations =
    [
        (1100300018u, new Vector3(-62.29986f, -826.7423f, 315.39798f)),
        (1100300019u, new Vector3(-42.11708f, -819.4943f, 316.413f)),
        (1100300020u, new Vector3(-50.01624f, -826.74396f, 309.8f)),
        (1100300021u, new Vector3(-63.714478f, -817.7603f, 312.456f)),
        (1100300022u, new Vector3(-43.03138f, -826.57556f, 313.40698f)),
        (1100300023u, new Vector3(-51.01272f, -819.90674f, 320.367f)),
        (1100300024u, new Vector3(-59.60385f, -818.0261f, 320.757f)),
        (1100300025u, new Vector3(-47.63262f, -813.7216f, 320.81198f)),
        (1100300026u, new Vector3(-55.35622f, -815.50745f, 306.914f)),
        (1100300027u, new Vector3(-58.10405f, -818.83215f, 310.94598f)),
        (1100300028u, new Vector3(-51.504288f, -820.1912f, 304.748f))
    ];
}
