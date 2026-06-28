using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class RuinsOfKelVorethEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialBloodPitPhase()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FightInBloodPit, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_BloodPit_ActivatesBranchMappedCount()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FightInBloodPit);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.FightYourWayThroughTheBloodPit, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_SlaveMaster_ActivatesBranchPrimaryAndChallengeObjectives()
    {
        var script = new NoOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SlaveMasterDrokk);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatSlavemasterDrokk);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatDarkwitchGurka);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DodgingTheDefense);
    }

    [Fact]
    public void OnPublicEventPhase_SlaveMaster_ActivatesBranchOptionalObjectives()
    {
        var script = new AllOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SlaveMasterDrokk);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillMechanoSlaversAndEldanConstructs);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.PutTheKelVorethSlavesOutOfTheirMisery);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.AccessTheHiddenEldanDataStorageDevices);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DisableTheExoLabDefenses);
    }

    [Fact]
    public void OnPublicEventPhase_ForgeMaster_ActivatesBranchOptionalObjectives()
    {
        var script = new AllOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ForgeMasterTrogun);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatForgemasterTrogun);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyKelVorethForges);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillBattleswornAndDarkwitchOsun);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.BurnKelVorethWarSupplies);
    }

    [Theory]
    [MemberData(nameof(BossCreditCreatureFilterCases))]
    public void BossEntityScripts_UseMappedCreatureFilters(
        Type scriptType,
        uint[] creatureIds,
        uint targetGroupId)
    {
        _ = targetGroupId;

        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(creatureIds, attribute.CreatureId);
    }

    [Theory]
    [InlineData(typeof(EldanSchematicDataStorageEntityScript), 33155u, 4422u)]
    [InlineData(typeof(KelVorethWarSuppliesEntityScript), 33334u, 3916u)]
    [InlineData(typeof(EldanPhaseMonitorEntityScript), 33768u, 4022u)]
    [InlineData(typeof(KelVorethForgeEntityScript), 33302u, 3903u)]
    public void OptionalChecklistEntityScripts_UseReviewedCreatureFilters(
        Type scriptType,
        uint creatureId,
        uint targetGroupId)
    {
        _ = targetGroupId;

        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { creatureId }, attribute.CreatureId);
    }

    [Theory]
    [InlineData(typeof(EldanSchematicDataStorageEntityScript), 33155u, 4422u)]
    [InlineData(typeof(KelVorethWarSuppliesEntityScript), 33334u, 3916u)]
    [InlineData(typeof(EldanPhaseMonitorEntityScript), 33768u, 4022u)]
    [InlineData(typeof(KelVorethForgeEntityScript), 33302u, 3903u)]
    public void OptionalChecklistEntityScripts_OnActivateSuccess_UpdateMappedTargetGroupOnce(
        Type scriptType,
        uint creatureId,
        uint targetGroupId)
    {
        var script = (RuinsOfKelVorethObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(creatureId, out IWorldEntity entity);

        script.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(targetGroupId, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Theory]
    [InlineData(
        PublicEventPhase.GrondTheCorpsemaker,
        PublicEventObjective.DefeatGrondTheCorpsemaker,
        1100300045u,
        32534u,
        1u,
        195.41f,
        -899.36f,
        225.65f,
        27715u,
        "GrondTheCorpsemakerEntityScript")]
    [InlineData(
        PublicEventPhase.SlaveMasterDrokk,
        PublicEventObjective.DefeatSlavemasterDrokk,
        1100300046u,
        32536u,
        2u,
        596.36f,
        -882.84f,
        976.53f,
        27104u,
        "SlavemasterDrokkEntityScript")]
    [InlineData(
        PublicEventPhase.ForgeMasterTrogun,
        PublicEventObjective.DefeatForgemasterTrogun,
        1100300044u,
        32531u,
        3u,
        -24.77f,
        -737.08f,
        990.32f,
        29203u,
        "ForgemasterTrogunEntityScript")]
    public void OnPublicEventPhase_BossPhases_SpawnReviewedPlacement(
        PublicEventPhase phase,
        PublicEventObjective objective,
        uint entityId,
        uint creatureId,
        uint publicEventPhase,
        float x,
        float y,
        float z,
        uint displayInfo,
        string scriptName)
    {
        var script = new NoOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);

        CreatedNpc boss = Assert.Single(createdNpcs);
        Vector3 position = new(x, y, z);
        AssertReviewedBossModel(boss, entityId, creatureId, publicEventPhase, position, displayInfo, scriptName);
        AssertGridEntityAddedToMap(mapProxy, boss.Instance, position);
    }

    [Theory]
    [InlineData(PublicEventPhase.GrondTheCorpsemaker)]
    [InlineData(PublicEventPhase.SlaveMasterDrokk)]
    [InlineData(PublicEventPhase.ForgeMasterTrogun)]
    public void OnPublicEventPhase_BossPhases_DoNotDuplicateReviewedPlacement(PublicEventPhase phase)
    {
        var script = new NoOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);
        script.OnPublicEventPhase((uint)phase);

        CreatedNpc boss = Assert.Single(createdNpcs);
        Assert.Single(boss.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)),
            i => ReferenceEquals(boss.Instance, i.Arguments[0]));
    }

    [Theory]
    [InlineData(PublicEventObjective.FightYourWayThroughTheBloodPit, PublicEventPhase.GrondTheCorpsemaker)]
    [InlineData(PublicEventObjective.DefeatGrondTheCorpsemaker, PublicEventPhase.SlaveMasterDrokk)]
    [InlineData(PublicEventObjective.DefeatSlavemasterDrokk, PublicEventPhase.ForgeMasterTrogun)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesMainBossChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatForgemasterTrogun, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FightYourWayThroughTheBloodPit, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 1336u });

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

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(
        uint creatureId,
        out IWorldEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        return publicEventManagerProxy;
    }

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static void AssertReviewedBossModel(
        CreatedNpc npc,
        uint entityId,
        uint creatureId,
        uint publicEventPhase,
        Vector3 position,
        uint displayInfo,
        string scriptName)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(entityId, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(creatureId, model.Creature);
        Assert.Equal((ushort)1336u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(position.X, model.X);
        Assert.Equal(position.Y, model.Y);
        Assert.Equal(position.Z, model.Z);
        Assert.Equal(Vector3.Zero.X, model.Rx);
        Assert.Equal(Vector3.Zero.Y, model.Ry);
        Assert.Equal(Vector3.Zero.Z, model.Rz);
        Assert.Equal(displayInfo, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)691u, model.Faction1);
        Assert.Equal((ushort)691u, model.Faction2);
        Assert.Equal(161u, model.EntityEvent.EventId);
        Assert.Equal(publicEventPhase, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal(scriptName, entityScript.ScriptName));
        EntityStatModel stat = Assert.Single(model.EntityStat);
        Assert.Equal((byte)Stat.Level, stat.Stat);
        Assert.Equal(25f, stat.Value);
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
        Assert.Equal(1336u, position.Info.Entry.Id);
    }

    public static IEnumerable<object[]> BossCreditCreatureFilterCases()
    {
        yield return
        [
            typeof(GrondTheCorpsemakerEntityScript),
            new uint[] { 32534u, 32535u },
            3841u
        ];
        yield return
        [
            typeof(SlavemasterDrokkEntityScript),
            new uint[] { 32536u, 32539u },
            3842u
        ];
        yield return
        [
            typeof(DarkwitchGurkaEntityScript),
            new uint[] { 33049u, 33050u },
            3850u
        ];
        yield return
        [
            typeof(VorethBattleswornDarkwitchEntityScript),
            new uint[] { 32555u, 32556u, 32618u, 32619u },
            3909u
        ];
    }

    private sealed class NoOptionalRuinsOfKelVorethEventScript : RuinsOfKelVorethEventScript
    {
        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return false;
        }
    }

    private sealed class AllOptionalRuinsOfKelVorethEventScript : RuinsOfKelVorethEventScript
    {
        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return true;
        }
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
