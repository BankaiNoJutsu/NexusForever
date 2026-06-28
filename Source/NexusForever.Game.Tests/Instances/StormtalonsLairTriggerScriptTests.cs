using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script;

namespace NexusForever.Game.Tests.Instances;

public class StormtalonsLairTriggerScriptTests
{
    [Fact]
    public void StopHighPriestTrigger_PlayerEnter_UpdatesObjective()
    {
        var script = new StopTheThundercallHighPriestGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.StopTheThundercallHighPriest, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void StopHighPriestTrigger_SamePlayerReenters_UpdatesObjectiveOnce()
    {
        var script = new StopTheThundercallHighPriestGridTriggerEntityScript();
        IPlayer player = CreatePlayer(1ul);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.StopTheThundercallHighPriest, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void StopHighPriestTrigger_DifferentPlayersEnter_UpdatesObjectivePerPlayer()
    {
        var script = new StopTheThundercallHighPriestGridTriggerEntityScript();
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
            Assert.Equal(PublicEventObjective.StopTheThundercallHighPriest, update.Arguments[0]);
            Assert.Equal(1, update.Arguments[1]);
        });
    }

    [Fact]
    public void StopHighPriestTrigger_NonPlayerEnter_DoesNotUpdateObjective()
    {
        var script = new StopTheThundercallHighPriestGridTriggerEntityScript();
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

    private static IPlayer CreatePlayer(ulong characterId)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        return player;
    }
}
