using System.Numerics;
using System.Reflection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.GameTable;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.ColdbloodCitadel;
using NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Instances;

public class ColdbloodCitadelEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialEnterPhase()
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_CreatesBranchGatherTrigger()
    {
        ColdbloodCitadelEventScript script = CreateScriptWithGatherRing(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedTrigger trigger,
            out _);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertTriggerInitialised(trigger, 53206u, 8656u);
        AssertTriggerAddedToMap(mapProxy, trigger.Instance, new Vector3(604.33f, -475.452f, -322.957f));
    }

    [Fact]
    public void OnPublicEventPhase_Enter_SpawnsReviewedGatherRingPlacement()
    {
        ColdbloodCitadelEventScript script = CreateScriptWithGatherRing(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            out CreatedSimple gatherRing);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertGatherRingModel(gatherRing);
        AssertGridEntityAddedToMap(mapProxy, gatherRing.Instance, new Vector3(604.33f, -475.452f, -322.957f), 3522u);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_WhenRepeated_SpawnsReviewedGatherRingPlacementOnce()
    {
        ColdbloodCitadelEventScript script = CreateScriptWithGatherRing(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedTrigger trigger,
            out CreatedSimple gatherRing);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);
        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        Assert.Single(trigger.Proxy.GetInvocations(nameof(IWorldLocationVolumeGridTriggerEntity.Initialise)));
        Assert.Single(gatherRing.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(2, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count);
    }

    [Fact]
    public void EntryTrigger_PlayerEnter_UpdatesFindPellObjective()
    {
        var script = new GridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.FindThePellAttackingColdbloodCitadel, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void EntryTrigger_PlayerReenters_UpdatesFindPellObjectiveOnce()
    {
        var script = new GridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.FindThePellAttackingColdbloodCitadel, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void EntryTrigger_DifferentPlayers_UpdatesFindPellObjectiveOnce()
    {
        var script = new GridTriggerEntityScript();
        IPlayer firstPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer secondPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(firstPlayer);
        script.OnEnterRange(secondPlayer);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.FindThePellAttackingColdbloodCitadel, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void EntryTrigger_NonPlayerEnter_DoesNotUpdateFindPellObjective()
    {
        var script = new GridTriggerEntityScript();
        IGridEntity nonPlayer = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayer);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void HailstoneGatecrasher_OnDeath_UpdatesDefeatHailstoneObjective()
    {
        var script = new HailstoneGatecrasherEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(
            out RecordingDispatchProxy<ICreatureEntity> creatureProxy,
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DefeatHailStoneGatecrasher, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Single(creatureProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void HailstoneGatecrasher_OnDeath_WhenRepeated_UpdatesDefeatHailstoneObjectiveAndRemovesOnce()
    {
        var script = new HailstoneGatecrasherEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(
            out RecordingDispatchProxy<ICreatureEntity> creatureProxy,
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DefeatHailStoneGatecrasher, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Single(creatureProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void IcebloodCoven_OnDeaths_UpdatesCovenObjectiveAfterAllThreeBossesDie()
    {
        IBaseMap map = CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IcebloodCovenEntityScript golagScript = CreateIcebloodCovenScript(75472u, map);
        IcebloodCovenEntityScript katlaScript = CreateIcebloodCovenScript(75473u, map);
        IcebloodCovenEntityScript ulfridScript = CreateIcebloodCovenScript(75474u, map);

        golagScript.OnDeath();
        katlaScript.OnDeath();

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));

        ulfridScript.OnDeath();
        ulfridScript.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DefeatTheIcebloodCoven, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void HarizogColdblood_OnDeath_UpdatesDefeatRisenHarizogObjective()
    {
        var script = new HarizogColdbloodEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = CreateCreatureWithPublicEventManager(
            out _,
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        script.OnLoad(creature);
        script.OnDeath();
        script.OnDeath();

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.DefeatTheRisenHarizog, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void HarizogColdblood_OnLoad_UsesMappedAutoAttacks()
    {
        var script = new HarizogColdbloodEntityScript(
            RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        ICreatureEntity creature = CreateCreature(
            75459u,
            CreateMapWithPublicEventManager(out _),
            out _);

        script.OnLoad(creature);

        Assert.Equal(new[] { 87944u, 87945u }, GetConfiguredAutoAttacks(script));
    }

    [Fact]
    public void RallyPellDrum_UsesCreatureFilterForMappedCreatureRow()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(RallyPellDrumEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 75706u }, attribute.CreatureId);
    }

    [Fact]
    public void RallyPellDrum_OnActivateSuccess_UpdatesRallyTargetGroupChecklistOnce()
    {
        var script = new RallyPellDrumEntityScript();
        IWorldEntity drum = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IWorldEntity> drumProxy,
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(drum);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(14469u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);

        Assert.Empty(drumProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    public static IEnumerable<object[]> OptionalTargetGroupChecklistObjectiveScripts()
    {
        yield return [typeof(SoulfrostShardsEntityScript), new[] { 75731u, 75732u, 75733u }, 14473u];
        yield return [typeof(SoulrotCanisterEntityScript), new[] { 75708u }, 14471u];
        yield return [typeof(WinterfuryPrisonerCageEntityScript), new[] { 75737u }, 14474u];
        yield return [typeof(KrovakTrapEntityScript), new[] { 75747u }, 14475u];
        yield return [typeof(LiquidSoulfrostSampleEntityScript), new[] { 75736u }, 14476u];
    }

    [Theory]
    [MemberData(nameof(OptionalTargetGroupChecklistObjectiveScripts))]
    public void OptionalTargetGroupChecklistObjectiveScripts_UseCreatureFilters(
        Type scriptType,
        uint[] matchingCreatureIds,
        uint targetGroupId)
    {
        _ = targetGroupId;

        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(scriptType);
        var match = new ScriptFilterMatch();

        foreach (uint creatureId in matchingCreatureIds)
        {
            IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
                .FilterByScriptType<IOwnedScript<IWorldEntity>>()
                .FilterByCreatureId(creatureId);

            Assert.True(match.Match(matchingSearch, parameters));
        }

        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(75706u);

        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Theory]
    [MemberData(nameof(OptionalTargetGroupChecklistObjectiveScripts))]
    public void OptionalTargetGroupChecklistObjectiveScripts_OnActivateSuccess_UpdatesTargetGroupChecklistOnce(
        Type scriptType,
        uint[] matchingCreatureIds,
        uint targetGroupId)
    {
        _ = matchingCreatureIds;

        var script = Assert.IsAssignableFrom<IWorldEntityScript>(
            Assert.IsAssignableFrom<object>(Activator.CreateInstance(scriptType)));
        IWorldEntity entity = CreateWorldEntityWithPublicEventManager(
            out RecordingDispatchProxy<IWorldEntity> entityProxy,
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        ((IOwnedScript<IWorldEntity>)script).OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)),
            i => i.Arguments.Length == 3);
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(targetGroupId, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);

        Assert.Empty(entityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void OnPublicEventPhase_HailStoneGatecrasher_ActivatesMainAndOptionalBranchObjectives()
    {
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        ColdbloodCitadelEventScript script = CreateScriptWithHailstone(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            true,
            (CommunicatorMessage.TowerEngineerRenhakul1, message));
        IWorldEntity gatherRing = CreateWorldEntity(PublicEventCreature.GatherRing, 701u, out RecordingDispatchProxy<IWorldEntity> gatherRingProxy);
        IWorldLocationVolumeGridTriggerEntity trigger = CreateWorldLocationTrigger(75624u, 702u, out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), args => (uint)args[0] switch
        {
            701u => gatherRing,
            702u => trigger,
            _    => null
        });

        script.OnAddToMap(gatherRing);
        script.OnAddToMap(trigger);
        script.OnPublicEventPhase((uint)PublicEventPhase.HailStoneGatecrasher);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHailStoneGatecrasher);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.SavePellFightingTheOsun);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.StealSampleOfLiquidSoulfrost);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherSoulfrostShards);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RallyTheWinterfuryPell);
        Assert.Single(gatherRingProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Single(triggerProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Fact]
    public void OnPublicEventPhase_HailStoneGatecrasher_SpawnsReviewedPlacement()
    {
        ColdbloodCitadelEventScript script = CreateScriptWithHailstone(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedHailstone createdHailstone);

        script.OnPublicEventPhase((uint)PublicEventPhase.HailStoneGatecrasher);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatHailStoneGatecrasher);

        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            createdHailstone.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300001u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(75508u, model.Creature);
        Assert.Equal((ushort)3522u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(459.544f, model.X);
        Assert.Equal(-467.2018f, model.Y);
        Assert.Equal(-596.769f, model.Z);
        Assert.Equal(24808u, model.DisplayInfo);
        Assert.Equal((ushort)691u, model.Faction1);
        Assert.Equal((ushort)691u, model.Faction2);
        Assert.Equal(907u, model.EntityEvent.EventId);
        Assert.Equal(1u, model.EntityEvent.Phase);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("HailstoneGatecrasherEntityScript", entityScript.ScriptName));
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Level && stat.Value == 50f);
        AssertGridEntityAddedToMap(mapProxy, createdHailstone.Instance, new Vector3(459.544f, -467.2018f, -596.769f), 3522u);
    }

    [Fact]
    public void OnPublicEventPhase_IceBloodCoven_ActivatesBranchTrapOptionalObjective()
    {
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        ColdbloodCitadelEventScript script = CreateScriptWithColdbloodGate(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            true,
            (CommunicatorMessage.TowerEngineerRenhakul11, message));

        script.OnPublicEventPhase((uint)PublicEventPhase.IceBloodCoven);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheIcebloodCoven);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RescueThePellArchitect);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillKrovakSummonersAndTheirFrostguards);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ConcurrentCovenCollapse);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.RescueWinterfuryPrisoners);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroySoulrotCanisters);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DisableSoulfrostTraps);
        Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Fact]
    public void OnPublicEventPhase_IceBloodCoven_SpawnsReviewedGatePlacement()
    {
        ColdbloodCitadelEventScript script = CreateScriptWithColdbloodGate(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedDoor coldbloodGate);

        script.OnPublicEventPhase((uint)PublicEventPhase.IceBloodCoven);

        AssertColdbloodGateModel(coldbloodGate);
        AssertGridEntityAddedToMap(mapProxy, coldbloodGate.Instance, new Vector3(371f, -456.92f, -592.31f), 3522u);
    }

    [Fact]
    public void OnPublicEventPhase_IceBloodCoven_WhenRepeated_SpawnsReviewedGatePlacementOnce()
    {
        ColdbloodCitadelEventScript script = CreateScriptWithColdbloodGate(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out CreatedDoor coldbloodGate);

        script.OnPublicEventPhase((uint)PublicEventPhase.IceBloodCoven);
        script.OnPublicEventPhase((uint)PublicEventPhase.IceBloodCoven);

        Assert.Single(coldbloodGate.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_RisenHarizog_ActivatesFinalBranchObjectives()
    {
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        ColdbloodCitadelEventScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out _,
            out _,
            false,
            (CommunicatorMessage.TowerEngineerRenhakul4, message));

        script.OnPublicEventPhase((uint)PublicEventPhase.RisenHarizog);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatTheRisenHarizog);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.InfusionInterdiction);
        Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
    }

    [Theory]
    [InlineData(PublicEventObjective.FindThePellAttackingColdbloodCitadel, PublicEventPhase.HailStoneGatecrasher)]
    [InlineData(PublicEventObjective.DefeatHailStoneGatecrasher, PublicEventPhase.IceBloodCoven)]
    [InlineData(PublicEventObjective.DefeatTheIcebloodCoven, PublicEventPhase.RisenHarizog)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheRisenHarizog, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        ColdbloodCitadelEventScript script = CreateScript(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FindThePellAttackingColdbloodCitadel, PublicEventStatus.Active));

        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static ColdbloodCitadelEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        bool preserveLoadInvocations = false,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(out eventProxy, out _, out _, false, preserveLoadInvocations, messages);
    }

    private static ColdbloodCitadelEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool alwaysActivateOptionalObjectives = false,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(out eventProxy, out mapProxy, out createdTriggers, alwaysActivateOptionalObjectives, false, messages);
    }

    private static ColdbloodCitadelEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers,
        bool alwaysActivateOptionalObjectives,
        bool preserveLoadInvocations,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(messages);
        ColdbloodCitadelEventScript script = alwaysActivateOptionalObjectives
            ? new AlwaysOptionalColdbloodCitadelEventScript(globalQuestManager)
            : new ColdbloodCitadelEventScript(globalQuestManager);
        IPublicEvent publicEvent = CreatePublicEvent(out eventProxy, out mapProxy, out createdTriggers);
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdbloodCitadelEventScript CreateScriptWithHailstone(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedHailstone createdHailstone,
        bool alwaysActivateOptionalObjectives = false,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(messages);
        ColdbloodCitadelEventScript script = alwaysActivateOptionalObjectives
            ? new AlwaysOptionalColdbloodCitadelEventScript(globalQuestManager)
            : new ColdbloodCitadelEventScript(globalQuestManager);
        IPublicEvent publicEvent = CreatePublicEventWithHailstone(out eventProxy, out mapProxy, out createdHailstone);
        script.OnLoad(publicEvent);
        eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdbloodCitadelEventScript CreateScriptWithGatherRing(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedTrigger createdTrigger,
        out CreatedSimple createdGatherRing)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager();
        var script = new ColdbloodCitadelEventScript(globalQuestManager);
        IPublicEvent publicEvent = CreatePublicEventWithGatherRing(out eventProxy, out mapProxy, out createdTrigger, out createdGatherRing);
        script.OnLoad(publicEvent);
        eventProxy.Invocations.Clear();

        return script;
    }

    private static ColdbloodCitadelEventScript CreateScriptWithColdbloodGate(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedDoor createdGate,
        bool alwaysActivateOptionalObjectives = false,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IGlobalQuestManager globalQuestManager = CreateGlobalQuestManager(messages);
        ColdbloodCitadelEventScript script = alwaysActivateOptionalObjectives
            ? new AlwaysOptionalColdbloodCitadelEventScript(globalQuestManager)
            : new ColdbloodCitadelEventScript(globalQuestManager);
        IPublicEvent publicEvent = CreatePublicEventWithColdbloodGate(out eventProxy, out mapProxy, out createdGate);
        script.OnLoad(publicEvent);
        eventProxy.Invocations.Clear();

        return script;
    }

    private static IPublicEvent CreatePublicEvent(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedTrigger> createdTriggers)
    {
        IPlayer player = CreatePlayer(out _);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 2058u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });

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

    private static IPublicEvent CreatePublicEventWithHailstone(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedHailstone createdHailstone)
    {
        IPlayer player = CreatePlayer(out _);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3522u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedHailstone hailstone = CreateCreatedHailstone();
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => hailstone.Instance);

        createdHailstone = hailstone;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithGatherRing(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedTrigger createdTrigger,
        out CreatedSimple createdGatherRing)
    {
        IPlayer player = CreatePlayer(out _);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3522u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedTrigger trigger = CreateTrigger();
        CreatedSimple gatherRing = CreateCreatedSimple();
        Queue<object> createdEntities = new([trigger.Instance, gatherRing.Instance]);
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => createdEntities.Dequeue());

        createdTrigger = trigger;
        createdGatherRing = gatherRing;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithColdbloodGate(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out CreatedDoor createdGate)
    {
        IPlayer player = CreatePlayer(out _);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3522u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), new[] { player });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        CreatedDoor gate = CreateCreatedDoor();
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () => gate.Instance);

        createdGate = gate;
        return publicEvent;
    }

    private static IGlobalQuestManager CreateGlobalQuestManager(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return globalQuestManager;
    }

    private static CreatedTrigger CreateTrigger()
    {
        IWorldLocationVolumeGridTriggerEntity trigger = RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity>.Create(
            out RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> triggerProxy);
        return new CreatedTrigger(trigger, triggerProxy);
    }

    private static CreatedHailstone CreateCreatedHailstone()
    {
        INonPlayerEntity hailstone = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> hailstoneProxy);
        return new CreatedHailstone(hailstone, hailstoneProxy);
    }

    private static CreatedSimple CreateCreatedSimple()
    {
        ISimpleEntity simple = RecordingDispatchProxy<ISimpleEntity>.Create(
            out RecordingDispatchProxy<ISimpleEntity> simpleProxy);
        return new CreatedSimple(simple, simpleProxy);
    }

    private static CreatedDoor CreateCreatedDoor()
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(
            out RecordingDispatchProxy<IDoorEntity> doorProxy);
        return new CreatedDoor(door, doorProxy);
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateTrigger(out IGridTriggerEntity trigger)
    {
        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IcebloodCovenEntityScript CreateIcebloodCovenScript(uint creatureId, IBaseMap map)
    {
        var script = new IcebloodCovenEntityScript();
        script.OnLoad(CreateCreature(creatureId, map, out _));
        return script;
    }

    private static ICreatureEntity CreateCreatureWithPublicEventManager(
        out RecordingDispatchProxy<ICreatureEntity> creatureProxy,
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        return CreateCreature(0u, CreateMapWithPublicEventManager(out publicEventManagerProxy), out creatureProxy);
    }

    private static IWorldEntity CreateWorldEntityWithPublicEventManager(
        out RecordingDispatchProxy<IWorldEntity> entityProxy,
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        IBaseMap map = CreateMapWithPublicEventManager(out publicEventManagerProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return entity;
    }

    private static IBaseMap CreateMapWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        return map;
    }

    private static ICreatureEntity CreateCreature(
        uint creatureId,
        IBaseMap map,
        out RecordingDispatchProxy<ICreatureEntity> creatureProxy)
    {
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out creatureProxy);
        creatureProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);

        return creature;
    }

    private static IReadOnlyList<uint> GetConfiguredAutoAttacks(CombatAI script)
    {
        FieldInfo field = typeof(CombatAI).GetField("autoAttacks", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsAssignableFrom<IReadOnlyList<uint>>(field.GetValue(script));
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

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
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
        IGridEntity trigger,
        Vector3 expectedPosition)
    {
        AssertGridEntityAddedToMap(mapProxy, trigger, expectedPosition, 3522u);
    }

    private static void AssertGatherRingModel(CreatedSimple gatherRing)
    {
        RecordingDispatchProxy<ISimpleEntity>.Invocation initialise = Assert.Single(
            gatherRing.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300002u, model.Id);
        Assert.Equal(EntityType.Simple, model.Type);
        Assert.Equal(75624u, model.Creature);
        Assert.Equal((ushort)3522u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(604.33f, model.X);
        Assert.Equal(-475.452f, model.Y);
        Assert.Equal(-322.957f, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(30327u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(907u, model.EntityEvent.EventId);
        Assert.Equal(0u, model.EntityEvent.Phase);
    }

    private static void AssertColdbloodGateModel(CreatedDoor coldbloodGate)
    {
        RecordingDispatchProxy<IDoorEntity>.Invocation initialise = Assert.Single(
            coldbloodGate.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300003u, model.Id);
        Assert.Equal(EntityType.Door, model.Type);
        Assert.Equal(75698u, model.Creature);
        Assert.Equal((ushort)3522u, model.World);
        Assert.Equal((ushort)0u, model.Area);
        Assert.Equal(371f, model.X);
        Assert.Equal(-456.92f, model.Y);
        Assert.Equal(-592.31f, model.Z);
        Assert.Equal(1.570796327f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(36619u, model.DisplayInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Equal(907u, model.EntityEvent.EventId);
        Assert.Equal(2u, model.EntityEvent.Phase);
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition,
        uint expectedWorldId)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)),
            invocation => ReferenceEquals(invocation.Arguments[0], entity));
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(expectedWorldId, position.Info.Entry.Id);
    }

    private sealed class AlwaysOptionalColdbloodCitadelEventScript : ColdbloodCitadelEventScript
    {
        public AlwaysOptionalColdbloodCitadelEventScript(IGlobalQuestManager globalQuestManager)
            : base(globalQuestManager)
        {
        }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return true;
        }
    }

    private sealed record CreatedTrigger(
        IWorldLocationVolumeGridTriggerEntity Instance,
        RecordingDispatchProxy<IWorldLocationVolumeGridTriggerEntity> Proxy);

    private sealed record CreatedHailstone(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed record CreatedSimple(
        ISimpleEntity Instance,
        RecordingDispatchProxy<ISimpleEntity> Proxy);

    private sealed record CreatedDoor(
        IDoorEntity Instance,
        RecordingDispatchProxy<IDoorEntity> Proxy);
}
