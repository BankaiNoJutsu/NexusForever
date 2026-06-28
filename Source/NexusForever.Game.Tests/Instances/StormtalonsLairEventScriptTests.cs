using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class StormtalonsLairEventScriptTests
{
    private static readonly (uint EntityId, Vector3 Position)[] TaintedFlowerStemPlacements =
    [
        (1100038210u, new Vector3(-141f, -32f, 239f)),
        (1100038211u, new Vector3(39f, 12f, 89f)),
        (1100038212u, new Vector3(-128f, -33f, 183f)),
        (1100038213u, new Vector3(35f, 0f, 149f)),
        (1100038214u, new Vector3(-130f, -32f, 183f)),
        (1100038215u, new Vector3(49f, 1f, 131f)),
        (1100038216u, new Vector3(-58f, -39f, 261f)),
        (1100038217u, new Vector3(-26f, -45f, 246f)),
        (1100038218u, new Vector3(-120f, -7f, 159f)),
        (1100038219u, new Vector3(-116f, -7f, 162f)),
        (1100038220u, new Vector3(-40f, 13f, 17f)),
        (1100038221u, new Vector3(-43f, 14f, 20f)),
        (1100038222u, new Vector3(-15f, -47f, 235f)),
        (1100038223u, new Vector3(-24f, -45f, 243f)),
        (1100038224u, new Vector3(-120f, -7f, 156f)),
        (1100038225u, new Vector3(-140f, -33f, 196f)),
        (1100038226u, new Vector3(-139f, -35f, 234f)),
        (1100038227u, new Vector3(-136f, -36f, 234f)),
        (1100038228u, new Vector3(29f, -47f, 150f)),
        (1100038229u, new Vector3(44f, 1f, 135f)),
        (1100038230u, new Vector3(-119f, -7f, 139f)),
        (1100038231u, new Vector3(-117f, -9f, 137f)),
        (1100038232u, new Vector3(-52f, -43f, 254f)),
        (1100038233u, new Vector3(218f, -29f, 221f)),
        (1100038234u, new Vector3(84f, -45f, 159f)),
        (1100038235u, new Vector3(85f, -45f, 155f)),
        (1100038236u, new Vector3(27f, -48f, 151f)),
        (1100038237u, new Vector3(24f, 10f, 48f)),
        (1100038238u, new Vector3(39f, 16f, 49f)),
        (1100038239u, new Vector3(41f, 12f, 89f)),
        (1100038240u, new Vector3(212f, -31f, 218f)),
        (1100038241u, new Vector3(76f, -45f, 161f))
    ];

    private static readonly (uint EntityId, Vector3 Position)[] ImprovementConstructionPlatformPlacements =
    [
        (1100038242u, new Vector3(-304f, 10f, 248f))
    ];

    private static readonly (uint EntityId, Vector3 Position)[] ThundercallDataAltarPlacements =
    [
        (1100038243u, new Vector3(-95f, -9f, 158f))
    ];

    private static readonly (uint EntityId, Vector3 Position)[] ThundercallStormTotemPlacements =
    [
        (1100038244u, new Vector3(-15f, 12f, 64f)),
        (1100038245u, new Vector3(-42f, -8f, 155f)),
        (1100038246u, new Vector3(73f, -46f, 190f)),
        (1100038247u, new Vector3(-321f, 7f, 200f)),
        (1100038248u, new Vector3(126f, -18f, 338f)),
        (1100038249u, new Vector3(-155f, -14f, 118f)),
        (1100038250u, new Vector3(-86f, -43f, 185f)),
        (1100038251u, new Vector3(-336f, -4f, 57f))
    ];

    private static readonly (uint EntityId, Vector3 Position)[] LaunchPadPlacements =
    [
        (1100038252u, new Vector3(-233f, -30f, 153f)),
        (1100038253u, new Vector3(-232f, -28f, 74f))
    ];

    [Fact]
    public void OnLoad_SetsInitialEnterPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.Enter, PublicEventObjective.SurviveTheThundercallPellZealots)]
    [InlineData(PublicEventPhase.DefeatBladeWindTheInvoker, PublicEventObjective.DefeatBladeWindTheInvoker)]
    [InlineData(PublicEventPhase.EliminateAethros, PublicEventObjective.EliminateAethros)]
    [InlineData(PublicEventPhase.DestroyStormtalon, PublicEventObjective.DestroyStormtalon)]
    public void OnPublicEventPhase_MainPhases_ActivateMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = new NoOptionalStormtalonsLairEventScript();
        RecordingDispatchProxy<IPublicEvent> eventProxy;
        IPublicEvent publicEvent = phase switch
        {
            PublicEventPhase.DefeatBladeWindTheInvoker => CreatePublicEventWithBladeWind(2u, out eventProxy, out _, out _),
            PublicEventPhase.EliminateAethros          => CreatePublicEventWithAethros(2u, out eventProxy, out _, out _),
            _                                          => CreatePublicEvent(2u, out eventProxy)
        };
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesBranchOptionalGatherObjective()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTaintedFlowerStems(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SurviveTheThundercallPellZealots);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.EliminateThundercallPell);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillThundercallPellBeforeTimeExpires);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherTaintedStemSamples);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ActivateImprovementConstructionPlatform);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.EnableInvokersHolocrypt);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_WhenOptionalProducerObjectivesActive_SpawnsReviewedStemAndPlatformPlacements()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTaintedFlowerStems(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdStems);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherTaintedStemSamples);
        Assert.Equal(TaintedFlowerStemPlacements.Length + ImprovementConstructionPlatformPlacements.Length, createdStems.Count);
        for (int i = 0; i < TaintedFlowerStemPlacements.Length; i++)
        {
            (uint entityId, Vector3 position) = TaintedFlowerStemPlacements[i];
            AssertTaintedFlowerStemModel(createdStems[i], entityId, position);
            AssertGridEntityAddedToMap(mapProxy, createdStems[i].Instance, position);
        }

        CreatedSimple platform = createdStems[^1];
        (uint platformEntityId, Vector3 platformPosition) = ImprovementConstructionPlatformPlacements[0];
        AssertStormtalonSimpleModel(platform, platformEntityId, platformPosition, 27244u, 23885u, 0u);
        AssertGridEntityAddedToMap(mapProxy, platform.Instance, platformPosition);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_WhenOptionalProducerObjectivesActive_DoesNotDuplicateReviewedPlacements()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithTaintedFlowerStems(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedSimple> createdStems);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);
        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        int expectedPlacements = TaintedFlowerStemPlacements.Length + ImprovementConstructionPlatformPlacements.Length;
        Assert.Equal(expectedPlacements, createdStems.Count);
        Assert.Equal(expectedPlacements, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesPellKillTargetGroupObjective()
    {
        var script = new NoOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SurviveTheThundercallPellZealots);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.EliminateThundercallPell);
        Assert.DoesNotContain(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.KillThundercallPellBeforeTimeExpires);
        Assert.DoesNotContain(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EnableInvokersHolocrypt);
    }

    [Fact]
    public void OnPublicEventPhase_BladeWind_ActivatesBranchOptionalSideObjectives()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithBladeWindAndOptionalInteractables(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            out _,
            out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatBladeWindTheInvoker);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatBladeWindTheInvoker);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ObtainDataFromTheThundercallDataAltar);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.HijackPowerFromTheThundercallStormTotems);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ActivateLaunchPads);
    }

    [Fact]
    public void OnPublicEventPhase_BladeWind_SpawnsReviewedVeteranPlacement()
    {
        var script = new NoOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithBladeWind(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedBladeWind createdBladeWind);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatBladeWindTheInvoker);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatBladeWindTheInvoker);

        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            createdBladeWind.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300048u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(33405u, model.Creature);
        Assert.Equal((ushort)382u, model.World);
        Assert.Equal((ushort)271u, model.Area);
        Assert.Equal(-88.49673f, model.X);
        Assert.Equal(-11.51191f, model.Y);
        Assert.Equal(123.5562f, model.Z);
        Assert.Equal(23629u, model.DisplayInfo);
        Assert.Equal((ushort)586u, model.Faction1);
        Assert.Equal((ushort)586u, model.Faction2);
        Assert.Equal(145u, model.EntityEvent.EventId);
        Assert.Equal(1u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("BladeWindTheInvokerVeteranEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        AssertGridEntityAddedToMap(mapProxy, createdBladeWind.Instance, new Vector3(-88.49673f, -11.51191f, 123.5562f));
    }

    [Fact]
    public void OnPublicEventPhase_BladeWind_WhenOptionalProducerObjectivesActive_SpawnsReviewedProducerPlacements()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithBladeWindAndOptionalInteractables(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            out List<CreatedSimple> createdSimpleInteractables,
            out List<CreatedNonPlayer> createdStormTotems);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatBladeWindTheInvoker);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.ObtainDataFromTheThundercallDataAltar);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.HijackPowerFromTheThundercallStormTotems);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ActivateLaunchPads);

        Assert.Equal(ThundercallDataAltarPlacements.Length + LaunchPadPlacements.Length, createdSimpleInteractables.Count);
        Assert.Equal(ThundercallStormTotemPlacements.Length, createdStormTotems.Count);

        (uint altarEntityId, Vector3 altarPosition) = ThundercallDataAltarPlacements[0];
        AssertStormtalonSimpleModel(createdSimpleInteractables[0], altarEntityId, altarPosition, 24314u, 22896u, 1u);
        AssertGridEntityAddedToMap(mapProxy, createdSimpleInteractables[0].Instance, altarPosition);

        for (int i = 0; i < ThundercallStormTotemPlacements.Length; i++)
        {
            (uint entityId, Vector3 position) = ThundercallStormTotemPlacements[i];
            AssertStormTotemModel(createdStormTotems[i], entityId, position);
            AssertGridEntityAddedToMap(mapProxy, createdStormTotems[i].Instance, position);
        }

        for (int i = 0; i < LaunchPadPlacements.Length; i++)
        {
            (uint entityId, Vector3 position) = LaunchPadPlacements[i];
            CreatedSimple launchPad = createdSimpleInteractables[i + ThundercallDataAltarPlacements.Length];
            AssertStormtalonSimpleModel(launchPad, entityId, position, 31587u, 26896u, 1u);
            AssertGridEntityAddedToMap(mapProxy, launchPad.Instance, position);
        }
    }

    [Fact]
    public void OnPublicEventPhase_Aethros_ActivatesBranchOptionalArcanistRoute()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithAethros(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EliminateAethros);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.EliminateAethros);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.FreeTheThundercallSacrificialPrisoners);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.UseYourGrenadesToDisableTheThundercallPell);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatArcanistBreezeBinderForTheEncryptionKey);
    }

    [Fact]
    public void OnPublicEventPhase_Aethros_CanActivateBranchOverseerRoute()
    {
        var script = new OverseerOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithAethros(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EliminateAethros);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillOverseerDriftCatcher);
    }

    [Fact]
    public void OnPublicEventPhase_Aethros_SpawnsReviewedVeteranPlacement()
    {
        var script = new NoOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithAethros(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedAethros createdAethros);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EliminateAethros);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.EliminateAethros);

        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            createdAethros.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300047u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(32703u, model.Creature);
        Assert.Equal((ushort)382u, model.World);
        Assert.Equal((ushort)271u, model.Area);
        Assert.Equal(-52.71065f, model.X);
        Assert.Equal(-48.68514f, model.Y);
        Assert.Equal(220.7862f, model.Z);
        Assert.Equal(27874u, model.DisplayInfo);
        Assert.Equal((ushort)586u, model.Faction1);
        Assert.Equal((ushort)586u, model.Faction2);
        Assert.Equal(145u, model.EntityEvent.EventId);
        Assert.Equal(2u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("AethrosVeteranEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        AssertGridEntityAddedToMap(mapProxy, createdAethros.Instance, new Vector3(-52.71065f, -48.68514f, 220.7862f));
    }

    [Fact]
    public void OnPublicEventPhase_Aethros_WhenCageObjectiveActive_SpawnsReviewedCagePlacements()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEventWithAethrosAndCages(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            out List<CreatedSimpleCollidable> createdCages);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EliminateAethros);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.FreeTheThundercallSacrificialPrisoners);

        Assert.Equal(5, createdCages.Count);
        AssertStormtalonCageModel(createdCages[0], 1100038201u, new Vector3(-230f, -31f, 130f));
        AssertStormtalonCageModel(createdCages[1], 1100038202u, new Vector3(-149f, -32f, 177f));
        AssertStormtalonCageModel(createdCages[2], 1100038203u, new Vector3(-203f, -32f, 194f));
        AssertStormtalonCageModel(createdCages[3], 1100038204u, new Vector3(-180f, -32f, 105f));
        AssertStormtalonCageModel(createdCages[4], 1100038205u, new Vector3(-226f, -30f, 86f));

        AssertGridEntityAddedToMap(mapProxy, createdCages[0].Instance, new Vector3(-230f, -31f, 130f));
        AssertGridEntityAddedToMap(mapProxy, createdCages[1].Instance, new Vector3(-149f, -32f, 177f));
        AssertGridEntityAddedToMap(mapProxy, createdCages[2].Instance, new Vector3(-203f, -32f, 194f));
        AssertGridEntityAddedToMap(mapProxy, createdCages[3].Instance, new Vector3(-180f, -32f, 105f));
        AssertGridEntityAddedToMap(mapProxy, createdCages[4].Instance, new Vector3(-226f, -30f, 86f));
    }

    [Fact]
    public void OnPublicEventPhase_StopHighPriest_UsesCurrentPlayerCount()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            4u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.StopTheThundercallHighPriest);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.StopTheThundercallHighPriest, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);

        CreatedTrigger trigger = Assert.Single(createdTriggers);
        AssertTriggerInitialised(trigger, 12736u, 1831u);
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(54.2672f, -11.5711f, 257.232f));
    }

    [Theory]
    [InlineData(PublicEventObjective.SurviveTheThundercallPellZealots, PublicEventPhase.DefeatBladeWindTheInvoker)]
    [InlineData(PublicEventObjective.DefeatBladeWindTheInvoker, PublicEventPhase.EliminateAethros)]
    [InlineData(PublicEventObjective.EliminateAethros, PublicEventPhase.StopTheThundercallHighPriest)]
    [InlineData(PublicEventObjective.StopTheThundercallHighPriest, PublicEventPhase.DestroyStormtalon)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesMainChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DestroyStormtalon, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SurviveTheThundercallPellZealots, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_StopHighPriestSucceeded_RemovesHighPriestTrigger()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            1u,
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(
            12736u,
            701u,
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] == 701u ? trigger : null);
        script.OnLoad(publicEvent);

        script.OnAddToMap(trigger);
        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.StopTheThundercallHighPriest, PublicEventStatus.Succeeded));

        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DestroyStormtalon);
    }

    [Fact]
    public void OnPublicEventPhase_DestroyStormtalon_QueuesWipRebornCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematic);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DestroyStormtalon);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    [Fact]
    public void ThundercallDataAltar_OnActivateSuccess_UpdatesObjective()
    {
        var script = new ThundercallDataAltarEntityScript();
        IWorldEntity entity = CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(entity);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.ObtainDataFromTheThundercallDataAltar, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void ThundercallDataAltar_UsesCreatureFilterForMappedCreatureRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(ThundercallDataAltarEntityScript));

        IScriptFilterSearch altarSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(24314u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(24315u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(altarSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void ThundercallStormTotem_OnActivateSuccess_UpdatesObjective()
    {
        var script = new ThundercallStormTotemEntityScript();
        IWorldEntity entity = CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(entity);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.HijackPowerFromTheThundercallStormTotems, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void ThundercallStormTotem_UsesCreatureFilterForMappedCreatureRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(ThundercallStormTotemEntityScript));

        IScriptFilterSearch stormTotemSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(27262u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(27263u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(stormTotemSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void LaunchPad_OnActivateSuccess_UpdatesActiveChecklistTargetGroupObjectiveOnce()
    {
        var script = new LaunchPadEntityScript();
        IWorldEntity entity = CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(entity);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(3679u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Fact]
    public void ImprovementConstructionPlatform_OnActivateSuccess_UpdatesMappedObjectivesOnce()
    {
        var script = new ImprovementConstructionPlatformEntityScript();
        IWorldEntity entity = CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(entity);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        Assert.Collection(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            update =>
            {
                Assert.Equal(PublicEventObjective.ActivateImprovementConstructionPlatform, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            },
            update =>
            {
                Assert.Equal(PublicEventObjective.EnableInvokersHolocrypt, update.Arguments[0]);
                Assert.Equal(1, update.Arguments[1]);
            });
    }

    [Theory]
    [MemberData(nameof(StormtalonOptionalObjectiveActivationCases))]
    public void OptionalObjectiveScripts_OnActivateSuccess_UpdateMappedObjective(
        Type scriptType,
        PublicEventObjective expectedObjective)
    {
        var script = Assert.IsAssignableFrom<IWorldEntityScript>(Activator.CreateInstance(scriptType));
        var ownedScript = Assert.IsAssignableFrom<IOwnedScript<IWorldEntity>>(script);
        IWorldEntity entity = CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        ownedScript.OnLoad(entity);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(expectedObjective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void ThundercallSacrificialCage_OnActivateSuccess_WithSimpleCollidableOwner_UpdatesMappedObjective()
    {
        var script = new ThundercallSacrificialCageEntityScript();
        ISimpleCollidableEntity entity = CreateSimpleCollidableWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        ((IOwnedScript<ISimpleCollidableEntity>)script).OnLoad(entity);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.FreeTheThundercallSacrificialPrisoners, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Theory]
    [MemberData(nameof(StormtalonOptionalObjectiveFilterCases))]
    public void OptionalObjectiveScripts_UseCreatureFilterForMappedCreatureRow(
        Type scriptType,
        uint expectedCreatureId)
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(scriptType);

        IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(expectedCreatureId);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(expectedCreatureId + 1u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(matchingSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void ThundercallSacrificialCage_UsesSimpleCollidableCreatureFilterForMappedCreatureRow()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(ThundercallSacrificialCageEntityScript));

        IScriptFilterSearch cageSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ISimpleCollidableEntity>>()
            .FilterByCreatureId(17191u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<ISimpleCollidableEntity>>()
            .FilterByCreatureId(17192u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(cageSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, players, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out mapProxy, out createdTriggers);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 382u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedTrigger trigger = CreateCreatedTrigger();
            triggers.Add(trigger);
            return trigger.Instance;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithBladeWind(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBladeWind createdBladeWind)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 382u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedBladeWind bladeWind = CreateCreatedBladeWind();
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => bladeWind.Instance);

        createdBladeWind = bladeWind;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithBladeWindAndOptionalInteractables(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedBladeWind createdBladeWind,
        out List<CreatedSimple> createdSimpleInteractables,
        out List<CreatedNonPlayer> createdStormTotems)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 382u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedBladeWind bladeWind = CreateCreatedBladeWind();
        List<CreatedSimple> simpleInteractables = [];
        List<CreatedNonPlayer> stormTotems = [];
        int createEntityCallCount = 0;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            createEntityCallCount++;
            if (createEntityCallCount == 1)
                return bladeWind.Instance;

            if (createEntityCallCount == 2 || createEntityCallCount >= 11)
            {
                CreatedSimple simple = CreateCreatedSimple();
                simpleInteractables.Add(simple);
                return simple.Instance;
            }

            CreatedNonPlayer stormTotem = CreateCreatedNonPlayer();
            stormTotems.Add(stormTotem);
            return stormTotem.Instance;
        });

        createdBladeWind = bladeWind;
        createdSimpleInteractables = simpleInteractables;
        createdStormTotems = stormTotems;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithTaintedFlowerStems(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedSimple> createdStems)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 382u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedSimple> stems = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedSimple stem = CreateCreatedSimple();
            stems.Add(stem);
            return stem.Instance;
        });

        createdStems = stems;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithAethros(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedAethros createdAethros)
    {
        return CreatePublicEventWithAethrosAndCages(
            playerCount,
            out eventProxy,
            out mapProxy,
            out createdAethros,
            out _);
    }

    private static IPublicEvent CreatePublicEventWithAethrosAndCages(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedAethros createdAethros,
        out List<CreatedSimpleCollidable> createdCages)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 382u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedAethros aethros = CreateCreatedAethros();
        List<CreatedSimpleCollidable> cages = [];
        bool aethrosCreated = false;
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            if (!aethrosCreated)
            {
                aethrosCreated = true;
                return aethros.Instance;
            }

            CreatedSimpleCollidable cage = CreateCreatedSimpleCollidable();
            cages.Add(cage);
            return cage.Instance;
        });

        createdAethros = aethros;
        createdCages = cages;
        return publicEvent;
    }

    private static CreatedTrigger CreateCreatedTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
    }

    private static CreatedBladeWind CreateCreatedBladeWind()
    {
        INonPlayerEntity bladeWind = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> bladeWindProxy);
        return new CreatedBladeWind(bladeWind, bladeWindProxy);
    }

    private static CreatedAethros CreateCreatedAethros()
    {
        INonPlayerEntity aethros = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> aethrosProxy);
        return new CreatedAethros(aethros, aethrosProxy);
    }

    private static CreatedNonPlayer CreateCreatedNonPlayer()
    {
        INonPlayerEntity nonPlayer = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> nonPlayerProxy);
        return new CreatedNonPlayer(nonPlayer, nonPlayerProxy);
    }

    private static CreatedSimple CreateCreatedSimple()
    {
        ISimpleEntity stem = RecordingDispatchProxy<ISimpleEntity>.Create(
            out RecordingDispatchProxy<ISimpleEntity> stemProxy);
        return new CreatedSimple(stem, stemProxy);
    }

    private static CreatedSimpleCollidable CreateCreatedSimpleCollidable()
    {
        ISimpleCollidableEntity cage = RecordingDispatchProxy<ISimpleCollidableEntity>.Create(
            out RecordingDispatchProxy<ISimpleCollidableEntity> cageProxy);
        return new CreatedSimpleCollidable(cage, cageProxy);
    }

    private static StormtalonsLairEventScript CreateScript()
    {
        return CreateScript(RecordingDispatchProxy<ICinematicBase>.Create(out _));
    }

    private static StormtalonsLairEventScript CreateScript(ICinematicBase cinematic)
    {
        return new StormtalonsLairEventScript(CreateCinematicFactory(cinematic));
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
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

    private static IWorldEntity CreateWorldEntityWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), 24314u);
        return entity;
    }

    private static ISimpleCollidableEntity CreateSimpleCollidableWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ISimpleCollidableEntity entity = RecordingDispatchProxy<ISimpleCollidableEntity>.Create(out RecordingDispatchProxy<ISimpleCollidableEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return entity;
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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
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
        AssertGridEntityAddedToMap(mapProxy, trigger.Instance, expectedPosition);
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
        Assert.Equal(382u, position.Info.Entry.Id);
    }

    private static void AssertStormtalonCageModel(CreatedSimpleCollidable cage, uint entityId, Vector3 position)
    {
        RecordingDispatchProxy<ISimpleCollidableEntity>.Invocation initialise = Assert.Single(
            cage.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.SimpleCollidable, model.Type);
        Assert.Equal(17191u, model.Creature);
        Assert.Equal((ushort)382u, model.World);
        Assert.Equal((ushort)271u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(21397u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(145u, model.EntityEvent.EventId);
        Assert.Equal(2u, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertTaintedFlowerStemModel(CreatedSimple stem, uint entityId, Vector3 position)
    {
        AssertStormtalonSimpleModel(stem, entityId, position, 24307u, 36934u, 0u);
    }

    private static void AssertStormtalonSimpleModel(
        CreatedSimple simple,
        uint entityId,
        Vector3 position,
        uint creatureId,
        uint displayInfo,
        uint eventPhase)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            simple.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)382u, model.World);
        Assert.Equal((ushort)271u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(145u, model.EntityEvent.EventId);
        Assert.Equal(eventPhase, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        Assert.Empty(model.EntityStat);
    }

    private static void AssertStormTotemModel(CreatedNonPlayer totem, uint entityId, Vector3 position)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            totem.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(27262u, model.Creature);
        Assert.Equal((ushort)382u, model.World);
        Assert.Equal((ushort)271u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(27702u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(145u, model.EntityEvent.EventId);
        Assert.Equal(1u, model.EntityEvent.Phase);
        Assert.Empty(model.EntityScript);
        EntityStatModel stat = Assert.Single(model.EntityStat);
        Assert.Equal((byte)Stat.InterruptArmour, stat.Stat);
        Assert.Equal(2f, stat.Value);
    }

    public static IEnumerable<object[]> StormtalonOptionalObjectiveActivationCases()
    {
        yield return [typeof(TaintedFlowerStemEntityScript), PublicEventObjective.GatherTaintedStemSamples];
        yield return [typeof(ThundercallSacrificialCageEntityScript), PublicEventObjective.FreeTheThundercallSacrificialPrisoners];
    }

    public static IEnumerable<object[]> StormtalonOptionalObjectiveFilterCases()
    {
        yield return [typeof(TaintedFlowerStemEntityScript), 24307u];
        yield return [typeof(ThundercallSacrificialCageEntityScript), 17191u];
        yield return [typeof(ImprovementConstructionPlatformEntityScript), 27244u];
        yield return [typeof(LaunchPadEntityScript), 31587u];
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedBladeWind(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedAethros(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedNonPlayer(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimple(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);

    private sealed record CreatedSimpleCollidable(
        ISimpleCollidableEntity Instance,
        RecordingDispatchProxy<ISimpleCollidableEntity> Proxy);

    private class NoOptionalStormtalonsLairEventScript : StormtalonsLairEventScript
    {
        public NoOptionalStormtalonsLairEventScript()
            : base(CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _)))
        {
        }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return false;
        }

        protected override bool ShouldUseWipArcanistVariant()
        {
            return false;
        }
    }

    private class AllOptionalStormtalonsLairEventScript : StormtalonsLairEventScript
    {
        public AllOptionalStormtalonsLairEventScript()
            : base(CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _)))
        {
        }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return true;
        }

        protected override bool ShouldUseWipArcanistVariant()
        {
            return true;
        }
    }

    private sealed class OverseerOptionalStormtalonsLairEventScript : AllOptionalStormtalonsLairEventScript
    {
        protected override bool ShouldUseWipArcanistVariant()
        {
            return false;
        }
    }
}
