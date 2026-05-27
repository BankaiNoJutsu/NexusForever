using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.Dungeon.Skullcano;
using NexusForever.Script.Instance.Dungeon.Skullcano.Script;

namespace NexusForever.Game.Tests.Instances;

public class SkullcanoTriggerScriptTests
{
    [Theory]
    [InlineData(typeof(ChasmGridTriggerEntityScript), PublicEventObjectiveType.Script, 2821u)]
    [InlineData(typeof(FindChiefGridTriggerEntityScript), PublicEventObjectiveType.Script, 3953u)]
    [InlineData(typeof(PlatformTriggerGuidEntityScript), PublicEventObjectiveType.Script, 2909u)]
    public void ScriptObjectiveTriggers_PlayerEnter_UpdatesMappedScriptObjective(Type scriptType, PublicEventObjectiveType objectiveType, uint objectId)
    {
        dynamic script = Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(objectiveType, update.Arguments[0]);
        Assert.Equal(objectId, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void TerraformerTrigger_PlayerEnter_UpdatesReachTerraformerObjective()
    {
        var script = new TerraformerGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.ReachTheEldanTerraformer, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
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

    private static RecordingDispatchProxy<IPublicEventManager> CreateTrigger(out IGridTriggerEntity trigger)
    {
        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }
}
