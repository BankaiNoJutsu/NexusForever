using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script;

namespace NexusForever.Game.Tests.PublicEvents;

public class EvilFromTheEtherTriggerScriptTests
{
    [Fact]
    public void CaptainWeirTeleportTrigger_UpdatesEscapeObjective()
    {
        var script = new CaptainWeirTeleportGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation teleportInvocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)));
        Assert.Equal(new Vector3(-398.65857f, -842.03436f, 119.298386f), teleportInvocation.Arguments[0]);
        Assert.Equal(true, teleportInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation updateInvocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.Script, updateInvocation.Arguments[0]);
        Assert.Equal(8243u, updateInvocation.Arguments[1]);
        Assert.Equal(1, updateInvocation.Arguments[2]);
    }

    [Fact]
    public void UpperDeckTeleportTrigger_UpdatesUpperDeckObjective()
    {
        var script = new UpperDeckTeleportGridTriggerEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateTrigger(out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation teleportInvocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportToLocal)));
        Assert.Equal(new Vector3(-53.353714f, -841.44684f, 164.51099f), teleportInvocation.Arguments[0]);
        Assert.Equal(false, teleportInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEventManager>.Invocation updateInvocation = Assert.Single(publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.Script, updateInvocation.Arguments[0]);
        Assert.Equal(8242u, updateInvocation.Arguments[1]);
        Assert.Equal(1, updateInvocation.Arguments[2]);
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateTrigger(out IGridTriggerEntity trigger)
    {
        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out var triggerProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out var publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);

        return publicEventManagerProxy;
    }
}
