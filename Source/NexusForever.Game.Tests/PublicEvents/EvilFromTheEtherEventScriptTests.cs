using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther;

namespace NexusForever.Game.Tests.PublicEvents;

public class EvilFromTheEtherEventScriptTests
{
    [Fact]
    public void GoToPrimaryPowerPlant_SeedsPlayersAlreadyInsideDoorRange()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IDoorEntity door = CreateDoor(7059788ul, 321u, [11ul, 12ul]);

        script.OnAddToMap(door);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), 3u);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToPrimaryPowerPlant);

        RecordingDispatchProxy<IPublicEvent>.Invocation activateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, activateInvocation.Arguments[0]);
        Assert.Equal(3u, activateInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation updateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, updateInvocation.Arguments[0]);
        Assert.Equal(2, updateInvocation.Arguments[1]);
    }

    [Fact]
    public void GoToPrimaryPowerPlant2_ReSeedsPlayersAfterObjectiveReset()
    {
        EvilFromTheEtherEventScript script = CreateScript(out var publicEventProxy, out var mapProxy);
        IDoorEntity door = CreateDoor(7024518ul, 654u, [21ul]);

        script.OnAddToMap(door);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetEntity), door);

        script.OnPublicEventPhase((uint)PublicEventPhase.GoToPrimaryPowerPlant2);

        RecordingDispatchProxy<IPublicEvent>.Invocation resetInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ResetObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, resetInvocation.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation activateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, activateInvocation.Arguments[0]);
        Assert.Equal(0u, activateInvocation.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation updateInvocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoToPrimaryPowerPlant, updateInvocation.Arguments[0]);
        Assert.Equal(1, updateInvocation.Arguments[1]);
    }

    private static EvilFromTheEtherEventScript CreateScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IGlobalQuestManager questManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);

        publicEventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        var script = new EvilFromTheEtherEventScript(cinematicFactory, questManager);
        script.OnLoad(publicEvent);

        publicEventProxy.Invocations.Clear();
        return script;
    }

    private static IDoorEntity CreateDoor(ulong activePropId, uint guid, ulong[] playerCharacterIds)
    {
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out var doorProxy);
        doorProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        doorProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)PublicEventCreature.Door);
        doorProxy.SetProperty(nameof(IWorldEntity.ActivePropId), activePropId);
        doorProxy.SetMethodReturn(nameof(IGridEntity.GetInRange), playerCharacterIds.Select(CreatePlayer).ToArray());
        return door;
    }

    private static IPlayer CreatePlayer(ulong characterId)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        return player;
    }
}
