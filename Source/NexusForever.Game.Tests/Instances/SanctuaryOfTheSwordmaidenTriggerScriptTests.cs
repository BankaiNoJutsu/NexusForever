using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class SanctuaryOfTheSwordmaidenTriggerScriptTests
{
    [Theory]
    [InlineData(typeof(TheTempleOfTheLifeSpeakerGridTriggerScript), PublicEventObjective.EnterTheTempleOfTheLifeSpeaker)]
    [InlineData(typeof(MoldwoodCorruptionGridTriggerScript), PublicEventObjective.ReachTheMoldwoodCorruption)]
    [InlineData(typeof(LifeweaverTerraceGridTriggerEntityScript), PublicEventObjective.EnterLifeweaverTerrace)]
    public void ScriptObjectiveTriggers_PlayerEnter_UpdatesMappedDirectObjective(Type scriptType, PublicEventObjective objective)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(objective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void Trigger_NonPlayerEnter_DoesNotUpdateObjective()
    {
        var script = new TheTempleOfTheLifeSpeakerGridTriggerScript();
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    [Fact]
    public void FlameMinibossTrigger_PlayerEnter_BroadcastsSeleneMessage()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> mapPlayerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        mapPlayerProxy.SetProperty(nameof(IPlayer.Session), session);

        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        globalQuestManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        var script = new FlameMinibossMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation lookup = Assert.Single(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.SpiritMotherSelene8, lookup.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void FlameMinibossTrigger_NonPlayerEnter_DoesNotBroadcast()
    {
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        var script = new FlameMinibossMessageTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([]);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
    }

    [Theory]
    [MemberData(nameof(SanctuaryTargetGroupObjectiveScriptCredits))]
    public void SanctuaryTargetGroupObjectiveScripts_OnActivateSuccess_UpdatesChecklistTargetGroupObjectiveOnce(
        Type scriptType,
        uint targetGroupId)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IWorldEntity entity = CreateWorldEntity(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy, out RecordingDispatchProxy<IWorldEntity> entityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(targetGroupId, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
        Assert.Empty(entityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [MemberData(nameof(SanctuaryTargetGroupObjectiveScriptFilters))]
    public void SanctuaryTargetGroupObjectiveScripts_UseCreatureFiltersForMappedCreatureRows(
        Type scriptType,
        uint[] mappedCreatureIds,
        uint unrelatedCreatureId)
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(scriptType);
        var match = new ScriptFilterMatch();

        foreach (uint mappedCreatureId in mappedCreatureIds)
        {
            IScriptFilterSearch matchingSearch = new ScriptFilterSearch()
                .FilterByScriptType<IOwnedScript<IWorldEntity>>()
                .FilterByCreatureId(mappedCreatureId);
            Assert.True(match.Match(matchingSearch, parameters));
        }

        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(unrelatedCreatureId);

        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    [Fact]
    public void SoulSporeEntityScript_OnActivateSuccess_UpdatesMappedScriptObjectiveOnce()
    {
        var script = new SoulSporeEntityScript();
        IWorldEntity entity = CreateWorldEntity(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy, out RecordingDispatchProxy<IWorldEntity> entityProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        script.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.UseTheSoulSporeOnMoldwoodGorgers, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Empty(entityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void SoulSporeEntityScript_UsesCreatureFilterForSpiritBombUnit()
    {
        ScriptFilterParameters parameters = new(RecordingDispatchProxy<IServiceProvider>.Create(out _));
        parameters.Initialise(typeof(SoulSporeEntityScript));

        IScriptFilterSearch spiritBombSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(70947u);
        IScriptFilterSearch unrelatedSearch = new ScriptFilterSearch()
            .FilterByScriptType<IOwnedScript<IWorldEntity>>()
            .FilterByCreatureId(43139u);

        var match = new ScriptFilterMatch();
        Assert.True(match.Match(spiritBombSearch, parameters));
        Assert.False(match.Match(unrelatedSearch, parameters));
    }

    public static TheoryData<Type, uint> SanctuaryTargetGroupObjectiveScriptCredits()
    {
        return new TheoryData<Type, uint>
        {
            { typeof(TorineSpiritRelicEntityScript), 3193u },
            { typeof(TorineSpiritRelicHolderEntityScript), 3164u },
            { typeof(LifeweaverTechClusterEntityScript), 5755u },
            { typeof(TorineTotemOfFlameEntityScript), 5756u },
        };
    }

    public static TheoryData<Type, uint[], uint> SanctuaryTargetGroupObjectiveScriptFilters()
    {
        return new TheoryData<Type, uint[], uint>
        {
            { typeof(TorineSpiritRelicEntityScript), [28638u, 28643u, 28652u, 28644u], 43173u },
            { typeof(TorineSpiritRelicHolderEntityScript), [28459u], 28638u },
            { typeof(LifeweaverTechClusterEntityScript), [43171u], 43173u },
            { typeof(TorineTotemOfFlameEntityScript), [43173u], 43171u },
        };
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

    private static IWorldEntity CreateWorldEntity(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return entity;
    }

    private static IGridTriggerEntity CreateTriggerWithMapInstance(IReadOnlyList<IPlayer> players)
    {
        IGridTriggerEntity trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IMapInstance map = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);
        return trigger;
    }
}
