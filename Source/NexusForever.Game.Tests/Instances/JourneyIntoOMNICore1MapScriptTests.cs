using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.WorldStory.JourneyIntoOMNICore1;

namespace NexusForever.Game.Tests.Instances;

public class JourneyIntoOMNICore1MapScriptTests
{
    [Fact]
    public void OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new JourneyIntoOMNICore1MapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        RecordingDispatchProxy<IPublicEvent>.Invocation join = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, join.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, join.Arguments[1]);

        RecordingDispatchProxy<ICinematicManager>.Invocation queued = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queued.Arguments[0]);
    }

    private static IContentMapInstance CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);

        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.CreateEvent), publicEvent);

        IContentMapInstance map = RecordingDispatchProxy<IContentMapInstance>.Create(out RecordingDispatchProxy<IContentMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        return map;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }
}
