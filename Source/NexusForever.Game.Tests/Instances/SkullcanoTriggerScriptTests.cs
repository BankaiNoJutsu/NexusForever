using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.Dungeon.Skullcano;
using NexusForever.Script.Instance.Dungeon.Skullcano.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class SkullcanoTriggerScriptTests
{
    [Theory]
    [InlineData(typeof(ChasmGridTriggerEntityScript), PublicEventObjective.CrossTheLavaFilledChasm)]
    [InlineData(typeof(FindChiefGridTriggerEntityScript), PublicEventObjective.FindChiefKaskalak)]
    [InlineData(typeof(PlatformTriggerGuidEntityScript), PublicEventObjective.GatherOnThePlatform)]
    public void ScriptObjectiveTriggers_PlayerEnter_UpdatesMappedDirectObjective(Type scriptType, PublicEventObjective objective)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IPlayer player = CreatePlayer(1ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(objective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Theory]
    [InlineData(typeof(ChasmGridTriggerEntityScript), PublicEventObjective.CrossTheLavaFilledChasm)]
    [InlineData(typeof(FindChiefGridTriggerEntityScript), PublicEventObjective.FindChiefKaskalak)]
    [InlineData(typeof(PlatformTriggerGuidEntityScript), PublicEventObjective.GatherOnThePlatform)]
    public void ScriptObjectiveTriggers_SamePlayerReenters_UpdatesMappedDirectObjectiveOnce(Type scriptType, PublicEventObjective objective)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IPlayer player = CreatePlayer(1ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(objective, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Theory]
    [InlineData(typeof(ChasmGridTriggerEntityScript), PublicEventObjective.CrossTheLavaFilledChasm)]
    [InlineData(typeof(FindChiefGridTriggerEntityScript), PublicEventObjective.FindChiefKaskalak)]
    [InlineData(typeof(PlatformTriggerGuidEntityScript), PublicEventObjective.GatherOnThePlatform)]
    public void ScriptObjectiveTriggers_DifferentPlayersEnter_UpdateMappedDirectObjectivePerPlayer(Type scriptType, PublicEventObjective objective)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IPlayer firstPlayer = CreatePlayer(1ul);
        IPlayer secondPlayer = CreatePlayer(2ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(firstPlayer);
        script.OnEnterRange(secondPlayer);

        List<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .ToList();
        Assert.Equal(2, updates.Count);
        Assert.All(updates, update =>
        {
            Assert.Equal(objective, update.Arguments[0]);
            Assert.Equal(1, update.Arguments[1]);
        });
    }

    [Fact]
    public void TerraformerTrigger_PlayerEnter_UpdatesReachTerraformerObjective()
    {
        var script = new TerraformerGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.ReachTheEldanTerraformer, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Theory]
    [MemberData(nameof(TargetGroupObjectiveCreditCases))]
    public void TargetGroupObjectiveScripts_OnActivateSuccess_UpdateMappedTargetGroupOnce(
        Type scriptType,
        PublicEventObjectiveType expectedObjectiveType,
        uint expectedTargetGroupId)
    {
        var script = Assert.IsAssignableFrom<IWorldEntityScript>(Activator.CreateInstance(scriptType));
        var ownedScript = Assert.IsAssignableFrom<IOwnedScript<IWorldEntity>>(script);
        IPlayer player = CreatePlayer(1ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntity(out IWorldEntity entity, out RecordingDispatchProxy<IWorldEntity> entityProxy);

        ownedScript.OnLoad(entity);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(expectedObjectiveType, update.Arguments[0]);
        Assert.Equal(expectedTargetGroupId, update.Arguments[1]);
        int expectedProgress = expectedObjectiveType == PublicEventObjectiveType.ActivateTargetGroupChecklist ? 0 : 1;
        Assert.Equal(expectedProgress, update.Arguments[2]);
        Assert.Empty(entityProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [MemberData(nameof(TargetGroupObjectiveCreatureFilterCases))]
    public void TargetGroupObjectiveScripts_UseCreatureFilterForMappedCreatureRow(
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
    public void Trigger_NonPlayerEnter_DoesNotUpdateObjective()
    {
        var script = new ChasmGridTriggerEntityScript();
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
    }

    public static IEnumerable<object[]> TargetGroupObjectiveCreditCases()
    {
        yield return [typeof(GrimGrimFoeSackEntityScript), PublicEventObjectiveType.ActivateTargetGroupChecklist, 2627u];
        yield return [typeof(MoltenChasmMonitoringStationEntityScript), PublicEventObjectiveType.ActivateTargetGroup, 2673u];
        yield return [typeof(RedmoonClusterMissileLaunchPanelEntityScript), PublicEventObjectiveType.ActivateTargetGroupChecklist, 2674u];
    }

    public static IEnumerable<object[]> TargetGroupObjectiveCreatureFilterCases()
    {
        yield return [typeof(GrimGrimFoeSackEntityScript), 24679u];
        yield return [typeof(MoltenChasmMonitoringStationEntityScript), 25168u];
        yield return [typeof(RedmoonClusterMissileLaunchPanelEntityScript), 25230u];
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

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntity(
        out IWorldEntity entity,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }

    private static IPlayer CreatePlayer(ulong characterId)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        return player;
    }
}
