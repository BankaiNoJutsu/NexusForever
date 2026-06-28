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
using NexusForever.Script.Instance.Expedition.SpaceMadness;
using NexusForever.Script.Instance.Expedition.SpaceMadness.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class SpaceMadnessEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialTalkToCaptainTeroPhase()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.TalkToCaptainTero, invocation.Arguments[0]);
    }

    [Fact]
    public void OnLoad_ActivatesGoldMedalTimerObjective()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToCaptainTero_SpawnsReviewedCaptainTeroPlacement()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedTalkNpcs(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainTero);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.TalkToCaptainTero);
        Assert.Equal(PublicEventObjective.TalkToCaptainTero, activation.Arguments[0]);

        CreatedNpc captainTero = Assert.Single(createdNpcs);
        Vector3 position = new(-14.70037f, 4.950619f, 337.0874f);
        AssertSpaceMadnessTalkNpcModel(captainTero, 1100300005u, 45900u, 2483, 0u, position, 28578u, 0, 466);
        AssertGridEntityAddedToMap(mapProxy, captainTero.Instance, position);
    }

    [Fact]
    public void OnPublicEventPhase_TalkToMajorLeeBarmy_SpawnsReviewedMajorLeePlacement()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedTalkNpcs(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToMajorLeeBarmy);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.TalkToMajorLeeBarmy);
        Assert.Equal(PublicEventObjective.TalkToMajorLeeBarmy, activation.Arguments[0]);

        CreatedNpc majorLeeBarmy = Assert.Single(createdNpcs);
        Vector3 position = new(-40.0482f, 4.858062f, 310.5947f);
        AssertSpaceMadnessTalkNpcModel(majorLeeBarmy, 1100300006u, 45812u, 2482, 1u, position, 29020u, 9128, 219);
        AssertGridEntityAddedToMap(mapProxy, majorLeeBarmy.Instance, position);
    }

    [Fact]
    public void OnPublicEventPhase_TalkNpcPhases_DoNotDuplicateReviewedSpawns()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedTalkNpcs(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainTero);
        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToCaptainTero);
        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToMajorLeeBarmy);
        script.OnPublicEventPhase((uint)PublicEventPhase.TalkToMajorLeeBarmy);

        Assert.Equal(2, createdNpcs.Count);
        Assert.Equal(2, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void OnPublicEventPhase_AccessObservationDeck_ActivatesMappedBranchObjectives()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithQueuedEntityPlacements(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _,
            AccessObservationDeckEntityOrder());
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.AccessTheObservationDeckComputer);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.AccessTheObservationDeckComputer);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.SavePanickedWorkers);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.AccessCrewDatapads);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectAnExperimentalSlank);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectAnExperimentalRockmite);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectAPartyDowngrazer);
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectEscapedCreatures
            && (uint)i.Arguments[1] == 3u);
    }

    [Fact]
    public void OnPublicEventPhase_AccessObservationDeck_SpawnsReviewedNightmaresDatapadsAndEscapedExperiments()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithQueuedEntityPlacements(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdSimpleEntities,
            out List<CreatedNpc> createdNpcs,
            AccessObservationDeckEntityOrder());
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.AccessTheObservationDeckComputer);

        Assert.Equal(9, createdSimpleEntities.Count);
        Assert.Equal(15, createdNpcs.Count);
        Assert.Equal(24, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());

        Vector3 observationDeckComputerPosition = new(-39.72298f, 7.399887f, 158.1838f);
        AssertSpaceMadnessSimpleModel(
            createdSimpleEntities[0],
            1100300007u,
            45972u,
            2415,
            2u,
            observationDeckComputerPosition,
            Vector3.Zero,
            23951u,
            0,
            219);
        AssertGridEntityAddedToMap(mapProxy, createdSimpleEntities[0].Instance, observationDeckComputerPosition);

        Vector3[] datapadPositions =
        [
            new(-83f, 0f, 121f),
            new(-61f, 4f, 190f),
            new(-9f, 4f, 231f),
            new(-23f, 7f, 166f),
            new(-62f, 0f, 126f),
            new(-65f, 6f, 249f),
            new(22f, -1f, 101f),
            new(-11f, -2f, 134f)
        ];
        for (int i = 0; i < datapadPositions.Length; i++)
        {
            CreatedSimple datapad = createdSimpleEntities[i + 1];
            AssertSpaceMadnessSimpleModel(
                datapad,
                1100300014u + (uint)i,
                45993u,
                2416,
                2u,
                datapadPositions[i],
                Vector3.Zero,
                27128u,
                0,
                219);
            AssertGridEntityAddedToMap(mapProxy, datapad.Instance, datapadPositions[i]);
        }

        (uint EntityId, uint CreatureId, ushort AreaId, Vector3 Position, uint DisplayInfo, float Health)[] nightmareSpawns =
        [
            (1100300026u, 46123u, 2413, new Vector3(-68f, 4f, 231f), 21629u, 13621f),
            (1100300027u, 46124u, 2413, new Vector3(-14f, 4f, 233f), 28166u, 10925f),
            (1100300028u, 46126u, 2414, new Vector3(-47f, 4f, 205f), 26363u, 10925f),
            (1100300029u, 46127u, 2414, new Vector3(-50f, 4f, 206f), 22875u, 13382f),
            (1100300030u, 46128u, 2414, new Vector3(-92f, 4f, 199f), 22961u, 13621f),
            (1100300031u, 46130u, 2414, new Vector3(-87f, 0f, 124f), 28257u, 16213f),
            (1100300032u, 46132u, 2416, new Vector3(-62f, 0f, 133f), 26120u, 16213f),
            (1100300033u, 46133u, 2415, new Vector3(-29f, 8f, 161f), 28775u, 10925f),
            (1100300034u, 46135u, 2416, new Vector3(-62f, 0f, 145f), 21537u, 15929f),
            (1100300035u, 46721u, 2416, new Vector3(9f, -2f, 121f), 28257u, 10925f),
            (1100300036u, 58783u, 2414, new Vector3(-95f, 0f, 149f), 21688u, 13621f),
            (1100300037u, 58798u, 2416, new Vector3(-44f, -1f, 124f), 23468u, 8630f)
        ];
        for (int i = 0; i < nightmareSpawns.Length; i++)
        {
            var spawn = nightmareSpawns[i];
            AssertSpaceMadnessNpcModel(
                createdNpcs[i],
                spawn.EntityId,
                spawn.CreatureId,
                spawn.AreaId,
                2u,
                spawn.Position,
                Vector3.Zero,
                spawn.DisplayInfo,
                0,
                218,
                new ExpectedStat(Stat.Health, spawn.Health),
                new ExpectedStat(Stat.Level, 32f));
            AssertGridEntityAddedToMap(mapProxy, createdNpcs[i].Instance, spawn.Position);
        }

        AssertSpaceMadnessNpcModel(
            createdNpcs[12],
            1100300023u,
            69899u,
            2413,
            2u,
            new Vector3(-6f, 4f, 252f),
            Vector3.Zero,
            21689u,
            0,
            219,
            new ExpectedStat(Stat.Health, 1f),
            new ExpectedStat(Stat.Level, 1f));
        AssertSpaceMadnessNpcModel(
            createdNpcs[13],
            1100300024u,
            69900u,
            2414,
            2u,
            new Vector3(-37f, 4f, 211f),
            Vector3.Zero,
            24917u,
            0,
            219,
            new ExpectedStat(Stat.Health, 1f),
            new ExpectedStat(Stat.Level, 1f));
        AssertSpaceMadnessNpcModel(
            createdNpcs[14],
            1100300025u,
            69901u,
            2414,
            2u,
            new Vector3(-75f, 4f, 193f),
            Vector3.Zero,
            22787u,
            0,
            219,
            new ExpectedStat(Stat.Health, 1f),
            new ExpectedStat(Stat.Level, 1f));
    }

    [Fact]
    public void OnPublicEventPhase_ReviewedSimplePlacementPhases_SpawnReviewedRows()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithQueuedEntityPlacements(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdSimpleEntities,
            out List<CreatedNpc> createdNpcs,
            AccessObservationDeckEntityOrder()
                .Concat([CreatedEntityKind.Simple, CreatedEntityKind.Simple])
                .ToArray());
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.AccessTheObservationDeckComputer);
        script.OnPublicEventPhase((uint)PublicEventPhase.OpenHazmatStorageCloset);
        script.OnPublicEventPhase((uint)PublicEventPhase.EquipAHazmatSuit);

        Assert.Equal(11, createdSimpleEntities.Count);
        Assert.Equal(15, createdNpcs.Count);

        Vector3 observationDeckComputerPosition = new(-39.72298f, 7.399887f, 158.1838f);
        AssertSpaceMadnessSimpleModel(
            createdSimpleEntities[0],
            1100300007u,
            45972u,
            2415,
            2u,
            observationDeckComputerPosition,
            Vector3.Zero,
            23951u,
            0,
            219);
        AssertGridEntityAddedToMap(mapProxy, createdSimpleEntities[0].Instance, observationDeckComputerPosition);

        Vector3 hazmatControlPanelPosition = new(7.77f, 7.399887f, 84.36f);
        AssertSpaceMadnessSimpleModel(
            createdSimpleEntities[9],
            1100300008u,
            46092u,
            2415,
            3u,
            hazmatControlPanelPosition,
            Vector3.Zero,
            23951u,
            0,
            219);
        AssertGridEntityAddedToMap(mapProxy, createdSimpleEntities[9].Instance, hazmatControlPanelPosition);

        Vector3 hazmatSuitPosition = new(4.41f, 0f, 72.89f);
        AssertSpaceMadnessSimpleModel(
            createdSimpleEntities[10],
            1100300012u,
            45981u,
            2415,
            5u,
            hazmatSuitPosition,
            Vector3.Zero,
            23951u,
            0,
            219);
        AssertGridEntityAddedToMap(mapProxy, createdSimpleEntities[10].Instance, hazmatSuitPosition);
    }

    [Fact]
    public void OnPublicEventPhase_ReviewedSimplePlacementPhases_WhenRepeated_SpawnReviewedRowsOnce()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithQueuedEntityPlacements(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdSimpleEntities,
            out List<CreatedNpc> createdNpcs,
            AccessObservationDeckEntityOrder()
                .Concat([CreatedEntityKind.Simple, CreatedEntityKind.Simple])
                .ToArray());
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.AccessTheObservationDeckComputer);
        script.OnPublicEventPhase((uint)PublicEventPhase.AccessTheObservationDeckComputer);
        script.OnPublicEventPhase((uint)PublicEventPhase.OpenHazmatStorageCloset);
        script.OnPublicEventPhase((uint)PublicEventPhase.OpenHazmatStorageCloset);
        script.OnPublicEventPhase((uint)PublicEventPhase.EquipAHazmatSuit);
        script.OnPublicEventPhase((uint)PublicEventPhase.EquipAHazmatSuit);

        Assert.Equal(11, createdSimpleEntities.Count);
        Assert.Equal(15, createdNpcs.Count);
        Assert.Equal(26, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void OnPublicEventPhase_SendAllClearSignal_ActivatesObjectiveAndSpawnsEngineeringComputer()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SendTheAllClearSignal);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.SendTheAllClearSignal);
        Assert.Equal(PublicEventObjective.SendTheAllClearSignal, activation.Arguments[0]);

        CreatedSimple engineeringComputer = Assert.Single(createdSimpleEntities);
        Vector3 position = new(241.969f, 3.34274f, 372.253f);
        AssertSpaceMadnessSimpleModel(
            engineeringComputer,
            1100300013u,
            45973u,
            2421,
            (uint)PublicEventPhase.SendTheAllClearSignal,
            position,
            Vector3.Zero,
            23951u,
            0,
            219);
        AssertGridEntityAddedToMap(mapProxy, engineeringComputer.Instance, position);
    }

    [Fact]
    public void OnPublicEventPhase_SendAllClearSignal_WhenRepeated_SpawnsEngineeringComputerOnce()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SendTheAllClearSignal);
        script.OnPublicEventPhase((uint)PublicEventPhase.SendTheAllClearSignal);

        Assert.Single(createdSimpleEntities);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_ActivateAirScrubberControls_ActivatesObjectiveAndSpawnsReviewedControlOnce()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSimplePlacements(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdSimpleEntities);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ActivateAirScrubberControls);
        script.OnPublicEventPhase((uint)PublicEventPhase.ActivateAirScrubberControls);

        Assert.Equal(2, eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .Count(i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ActivateAirScrubberControls));

        CreatedSimple airScrubberControls = Assert.Single(createdSimpleEntities);
        Vector3 position = new(211f, -1f, 342f);
        AssertSpaceMadnessSimpleModel(
            airScrubberControls,
            1100300022u,
            46437u,
            2421,
            (uint)PublicEventPhase.ActivateAirScrubberControls,
            position,
            Vector3.Zero,
            28014u,
            0,
            219);
        AssertGridEntityAddedToMap(mapProxy, airScrubberControls.Instance, position);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_KillHallucinatingLivestock_ActivatesMappedBranchObjectives()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedTalkNpcs(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillHallucinatingLivestock);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillHallucinatingLivestock);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GiveAirHelmsToWorkers);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.AvoidExplodingRowsdowers);
    }

    [Fact]
    public void OnPublicEventPhase_KillHallucinatingLivestock_SpawnsReviewedPhaseFourPlacements()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedTalkNpcs(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillHallucinatingLivestock);

        Assert.Equal(3, createdNpcs.Count);

        Vector3 exactChangePosition = new(25.23f, -2.91f, 120.42f);
        AssertSpaceMadnessNpcModel(
            createdNpcs[0],
            1100300009u,
            46483u,
            2415,
            4u,
            exactChangePosition,
            new Vector3(1.57079f, 0f, 0f),
            27827u,
            0,
            218,
            new ExpectedStat(Stat.Health, 1f),
            new ExpectedStat(Stat.Level, 32f));
        AssertGridEntityAddedToMap(mapProxy, createdNpcs[0].Instance, exactChangePosition);

        Vector3 blazingCrewmanAPosition = new(7.12621f, -2.9093f, 112.0714f);
        AssertSpaceMadnessNpcModel(
            createdNpcs[1],
            1100300010u,
            46714u,
            2415,
            4u,
            blazingCrewmanAPosition,
            new Vector3(2.64731f, 0f, 0f),
            26022u,
            9347,
            218,
            new ExpectedStat(Stat.Health, 1f),
            new ExpectedStat(Stat.Level, 32f));
        AssertGridEntityAddedToMap(mapProxy, createdNpcs[1].Instance, blazingCrewmanAPosition);

        Vector3 blazingCrewmanBPosition = new(12.919f, -2.91f, 124.5722f);
        AssertSpaceMadnessNpcModel(
            createdNpcs[2],
            1100300011u,
            46714u,
            2415,
            4u,
            blazingCrewmanBPosition,
            new Vector3(2.64731f, 0f, 0f),
            26022u,
            9347,
            218,
            new ExpectedStat(Stat.Health, 1f),
            new ExpectedStat(Stat.Level, 32f));
        AssertGridEntityAddedToMap(mapProxy, createdNpcs[2].Instance, blazingCrewmanBPosition);
    }

    [Fact]
    public void OnPublicEventPhase_KillHallucinatingLivestock_WhenRepeated_SpawnsReviewedPhaseFourPlacementsOnce()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedTalkNpcs(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.KillHallucinatingLivestock);
        script.OnPublicEventPhase((uint)PublicEventPhase.KillHallucinatingLivestock);

        Assert.Equal(3, createdNpcs.Count);
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void OnPublicEventPhase_EnterAirlock_UsesCurrentPlayerCount()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(4u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EnterTheAirlock);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EnterTheAirlock);
        Assert.Equal(PublicEventObjective.EnterTheAirlock, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_EnterAirlock_CreatesWipGuessedGatherTrigger()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            4u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EnterTheAirlock);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 37702u, 5512u);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(-35.28216f, 4.942017f, 270.0408f));
    }

    [Fact]
    public void OnPublicEventPhase_EnterResearchLaboratory_CreatesWipGuessedGatherTrigger()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            3u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EnterTheResearchLaboratory);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 37621u, 5531u);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(-55.08974f, -0.05234623f, 137.5821f));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainTero, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.TalkToMajorLeeBarmy);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalSignal_CreditsGoldTimerAndFinishesEvent()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SendTheAllClearSignal, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_LivestockKillSuccess_AdvancesToAirScrubberControls()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.KillHallucinatingLivestock, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.ActivateAirScrubberControls);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = new SpaceMadnessEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.TalkToCaptainTero, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void CaptainTeroScript_IsBoundToCaptainTeroCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CaptainTeroEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 45900u }, attribute.CreatureId);
    }

    [Fact]
    public void MajorLeeBarmyScript_IsBoundToMajorLeeBarmyCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(MajorLeeBarmyEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 45812u }, attribute.CreatureId);
    }

    [Fact]
    public void ObservationDeckComputerScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(ObservationDeckComputerEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 45972u }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(SpaceMadnessTargetGroupObjectiveFilterCases))]
    public void TargetGroupObjectiveScripts_AreBoundToMappedCreatureRows(
        Type scriptType,
        uint[] expectedCreatureIds)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(expectedCreatureIds, attribute.CreatureId);
    }

    [Fact]
    public void HazmatSuitScript_IsBoundToReviewedHazmatSuitCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(HazmatSuitEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 45981u }, attribute.CreatureId);
    }

    [Fact]
    public void CaptainTero_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayer()
    {
        var script = new CaptainTeroEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity captainTero);

        script.OnLoad(captainTero);
        script.OnActivateSuccess(player);

        AssertTalkUpdate(publicEventManagerProxy, player, 6342u);
    }

    [Fact]
    public void MajorLeeBarmy_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayer()
    {
        var script = new MajorLeeBarmyEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity majorLeeBarmy);

        script.OnLoad(majorLeeBarmy);
        script.OnActivateSuccess(player);

        AssertTalkUpdate(publicEventManagerProxy, player, 6358u);
    }

    [Fact]
    public void HallucinatingVentureWorker_OnActivateSuccess_UpdatesActiveTalkObjectiveByTargetGroupForPlayerOnce()
    {
        var script = new HallucinatingVentureWorkerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity hallucinatingWorker);

        script.OnLoad(hallucinatingWorker);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        AssertTalkUpdate(publicEventManagerProxy, player, 6370u);
    }

    [Fact]
    public void ObservationDeckComputer_OnActivateSuccess_UpdatesActiveTargetGroupObjectiveOnce()
    {
        var script = new ObservationDeckComputerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity computer);

        script.OnLoad(computer);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(6356u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void EngineeringComputer_OnActivateSuccess_UpdatesFinalSignalTargetGroupOnce()
    {
        var script = new EngineeringComputerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity computer);

        script.OnLoad(computer);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(6372u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Theory]
    [MemberData(nameof(SpaceMadnessTargetGroupObjectiveCreditCases))]
    public void TargetGroupObjectiveScripts_OnActivateSuccess_UpdateMappedTargetGroupOnce(
        Type scriptType,
        PublicEventObjectiveType expectedObjectiveType,
        uint expectedTargetGroupId)
    {
        var script = Assert.IsAssignableFrom<IWorldEntityScript>(Activator.CreateInstance(scriptType));
        var ownedScript = Assert.IsAssignableFrom<IOwnedScript<IWorldEntity>>(script);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity entity);

        ownedScript.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(expectedObjectiveType, update.Arguments[0]);
        Assert.Equal(expectedTargetGroupId, update.Arguments[1]);
        Assert.Equal(expectedObjectiveType == PublicEventObjectiveType.ActivateTargetGroupChecklist ? 0 : 1, update.Arguments[2]);
    }

    [Fact]
    public void HazmatSuit_OnActivateSuccess_UpdatesEquipHazmatSuitObjectiveOnce()
    {
        var script = new HazmatSuitEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity hazmatSuit);

        script.OnLoad(hazmatSuit);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.EquipAHazmatSuit, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Theory]
    [MemberData(nameof(SpaceMadnessEscapedExperimentObjectiveCreditCases))]
    public void EscapedExperimentScripts_OnActivateSuccess_UpdateChildTargetGroupAndAggregateOnce(
        Type scriptType,
        uint expectedTargetGroupId)
    {
        var script = Assert.IsAssignableFrom<IWorldEntityScript>(Activator.CreateInstance(scriptType));
        var ownedScript = Assert.IsAssignableFrom<IOwnedScript<IWorldEntity>>(script);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity entity);

        ownedScript.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective))
                .ToList();
        Assert.Equal(2, updates.Count);
        Assert.Contains(updates, i =>
            i.Arguments.Length == 3 &&
            (PublicEventObjectiveType)i.Arguments[0] == PublicEventObjectiveType.ActivateTargetGroup &&
            (uint)i.Arguments[1] == expectedTargetGroupId &&
            (int)i.Arguments[2] == 1);
        Assert.Contains(updates, i =>
            i.Arguments.Length == 2 &&
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectEscapedCreatures &&
            (int)i.Arguments[1] == 1);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2149u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTrigger trigger = CreateTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithReviewedTalkNpcs(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 1u);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2149u });

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

    private static IPublicEvent CreatePublicEventWithReviewedSimplePlacements(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedSimple> createdSimpleEntities)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 1u);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2149u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedSimple> simpleEntities = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedSimple simple = CreateSimple();
            simpleEntities.Add(simple);
            return simple.Instance;
        });

        createdSimpleEntities = simpleEntities;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithQueuedEntityPlacements(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedSimple> createdSimpleEntities,
        out List<CreatedNpc> createdNpcs,
        params CreatedEntityKind[] entityOrder)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 1u);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2149u });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedSimple> simpleEntities = [];
        List<CreatedNpc> npcs = [];
        Queue<CreatedEntityKind> queuedKinds = new(entityOrder);
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedEntityKind kind = queuedKinds.Dequeue();
            if (kind == CreatedEntityKind.Simple)
            {
                CreatedSimple simple = CreateSimple();
                simpleEntities.Add(simple);
                return simple.Instance;
            }

            CreatedNpc npc = CreateNpc();
            npcs.Add(npc);
            return npc.Instance;
        });

        createdSimpleEntities = simpleEntities;
        createdNpcs = npcs;
        return publicEvent;
    }

    private static CreatedEntityKind[] AccessObservationDeckEntityOrder()
    {
        return Enumerable.Repeat(CreatedEntityKind.Simple, 1)
            .Concat(Enumerable.Repeat(CreatedEntityKind.Npc, 12))
            .Concat(Enumerable.Repeat(CreatedEntityKind.Simple, 8))
            .Concat(Enumerable.Repeat(CreatedEntityKind.Npc, 3))
            .ToArray();
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

    private static CreatedTrigger CreateTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static CreatedSimple CreateSimple()
    {
        ISimpleEntity simple = RecordingDispatchProxy<ISimpleEntity>.Create(
            out RecordingDispatchProxy<ISimpleEntity> simpleProxy);
        return new CreatedSimple(simple, simpleProxy);
    }

    private static void AssertSpaceMadnessTalkNpcModel(
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
        AssertSpaceMadnessNpcModel(
            npc,
            entityId,
            creatureId,
            areaId,
            phase,
            position,
            Vector3.Zero,
            displayInfo,
            outfitInfo,
            factionId,
            new ExpectedStat(Stat.Health, 1f));
    }

    private static void AssertSpaceMadnessNpcModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        Vector3 rotation,
        uint displayInfo,
        ushort outfitInfo,
        ushort factionId,
        params ExpectedStat[] expectedStats)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)2149u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(rotation.X, model.Rx);
        Assert.Equal(rotation.Y, model.Ry);
        Assert.Equal(rotation.Z, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal(outfitInfo, model.OutfitInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal(390u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Equal(expectedStats.Length, model.EntityStat.Count);
        foreach (ExpectedStat expectedStat in expectedStats)
            Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)expectedStat.Stat && stat.Value == expectedStat.Value);
    }

    private static void AssertSpaceMadnessSimpleModel(
        CreatedSimple simple,
        uint entityId,
        uint creatureId,
        ushort areaId,
        uint phase,
        Vector3 position,
        Vector3 rotation,
        uint displayInfo,
        ushort outfitInfo,
        ushort factionId)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            simple.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)2149u, model.World);
        Assert.Equal(areaId, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(rotation.X, model.Rx);
        Assert.Equal(rotation.Y, model.Ry);
        Assert.Equal(rotation.Z, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal(outfitInfo, model.OutfitInfo);
        Assert.Equal(factionId, model.Faction1);
        Assert.Equal(factionId, model.Faction2);
        Assert.Equal((byte)0, model.QuestChecklistIdx);
        Assert.Equal(390u, model.EntityEvent.EventId);
        Assert.Equal(phase, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertTriggerInitialised(CreatedTrigger trigger, uint worldLocationId, uint objectId)
    {
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Invocation initialise = Assert.Single(
            trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Equal(worldLocationId, initialise.Arguments[0]);
        Assert.Equal(objectId, initialise.Arguments[1]);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        CreatedTrigger trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger.Instance, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(2149u, position.Info.Entry.Id);
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)),
            i => ReferenceEquals(entity, i.Arguments[0]));
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(2149u, position.Info.Entry.Id);
    }

    private static void AssertTalkUpdate(
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        IPlayer player,
        uint targetGroupId)
    {
        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(targetGroupId, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimple(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);

    private enum CreatedEntityKind
    {
        Simple,
        Npc
    }

    private sealed record ExpectedStat(
        Stat Stat,
        float Value);

    public static IEnumerable<object[]> SpaceMadnessTargetGroupObjectiveFilterCases()
    {
        yield return [typeof(CrewDatapadEntityScript), new uint[] { 45993u }];
        yield return [typeof(HazmatControlPanelEntityScript), new uint[] { 46092u }];
        yield return [typeof(AirScrubberControlsEntityScript), new uint[] { 45861u, 46437u }];
        yield return [typeof(EngineeringComputerEntityScript), new uint[] { 45973u }];
        yield return [typeof(HallucinatingVentureWorkerEntityScript), new uint[] { 45903u, 45904u }];
        yield return [typeof(ExperimentalSlankEntityScript), new uint[] { 69899u }];
        yield return [typeof(ExperimentalRockmiteEntityScript), new uint[] { 69900u }];
        yield return [typeof(PartyDowngrazerEntityScript), new uint[] { 69901u }];
    }

    public static IEnumerable<object[]> SpaceMadnessTargetGroupObjectiveCreditCases()
    {
        yield return [typeof(CrewDatapadEntityScript), PublicEventObjectiveType.ActivateTargetGroupChecklist, 6384u];
        yield return [typeof(HazmatControlPanelEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 12651u];
        yield return [typeof(AirScrubberControlsEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 6371u];
        yield return [typeof(EngineeringComputerEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 6372u];
    }

    public static IEnumerable<object[]> SpaceMadnessEscapedExperimentObjectiveCreditCases()
    {
        yield return [typeof(ExperimentalSlankEntityScript), 12595u];
        yield return [typeof(ExperimentalRockmiteEntityScript), 12596u];
        yield return [typeof(PartyDowngrazerEntityScript), 12597u];
    }
}
