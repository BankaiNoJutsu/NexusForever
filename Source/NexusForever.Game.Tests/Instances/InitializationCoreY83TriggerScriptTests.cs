using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Raid.InitializationCoreY83;
using NexusForever.Script.Instance.Raid.InitializationCoreY83.Script;

namespace NexusForever.Game.Tests.Instances;

public class InitializationCoreY83TriggerScriptTests
{
    [Fact]
    public void CentralAccessCorridorTrigger_PlayerEnter_BroadcastsNurtonMessage()
    {
        IPlayer enteringPlayer = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer mapPlayer = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> mapPlayerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        mapPlayerProxy.SetProperty(nameof(IPlayer.Session), session);

        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        ICommunicatorMessage communicatorMessage = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> communicatorMessageProxy);
        globalQuestManagerProxy.SetMethodReturn(nameof(IGlobalQuestManager.GetCommunicatorMessage), communicatorMessage);

        var script = new CentralAccessCorridorTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([mapPlayer]);

        script.OnLoad(trigger);
        script.OnEnterRange(enteringPlayer);

        RecordingDispatchProxy<IGlobalQuestManager>.Invocation lookup = Assert.Single(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
        Assert.Equal(CommunicatorMessage.Nurton1, lookup.Arguments[0]);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(communicatorMessageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void CentralAccessCorridorTrigger_NonPlayerEnter_DoesNotBroadcast()
    {
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        var globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        var script = new CentralAccessCorridorTriggerScript(globalQuestManager);
        IGridTriggerEntity trigger = CreateTriggerWithMapInstance([]);

        script.OnLoad(trigger);
        script.OnEnterRange(entity);

        Assert.Empty(globalQuestManagerProxy.GetInvocations(nameof(IGlobalQuestManager.GetCommunicatorMessage)));
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
