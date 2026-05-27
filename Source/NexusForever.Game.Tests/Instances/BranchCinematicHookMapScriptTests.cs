using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using DeepSpaceExplorationMapScript = NexusForever.Script.Instance.Expedition.DeepSpaceExploration.DeepSpaceExplorationMapScript;
using FragmentZeroMapScript = NexusForever.Script.Instance.Expedition.FragmentZero.FragmentZeroMapScript;
using InfestationMapScript = NexusForever.Script.Instance.Expedition.Infestation.InfestationMapScript;
using ProtostarsSuperMallInTheSkyMapScript = NexusForever.Script.Instance.EventInstances.ProtostarsSuperMallInTheSky.ProtostarsSuperMallInTheSkyMapScript;
using ShadesEveMapScript = NexusForever.Script.Instance.EventInstances.ShadesEve.ShadesEveMapScript;
using SpaceMadnessMapScript = NexusForever.Script.Instance.Expedition.SpaceMadness.SpaceMadnessMapScript;

namespace NexusForever.Game.Tests.Instances;

public class BranchCinematicHookMapScriptTests
{
    [Fact]
    public void DeepSpaceExploration_OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new DeepSpaceExplorationMapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        AssertPlayerJoinedAndCinematicQueued(player, cinematic, eventProxy, cinematicManagerProxy);
    }

    [Fact]
    public void ProtostarSuperMall_OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new ProtostarsSuperMallInTheSkyMapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        AssertPlayerJoinedAndCinematicQueued(player, cinematic, eventProxy, cinematicManagerProxy);
    }

    [Fact]
    public void FragmentZero_OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new FragmentZeroMapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        AssertPlayerJoinedAndCinematicQueued(player, cinematic, eventProxy, cinematicManagerProxy);
    }

    [Fact]
    public void Infestation_OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new InfestationMapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        AssertPlayerJoinedAndCinematicQueued(player, cinematic, eventProxy, cinematicManagerProxy);
    }

    [Fact]
    public void ShadesEve_OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new ShadesEveMapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        AssertPlayerJoinedAndCinematicQueued(player, cinematic, eventProxy, cinematicManagerProxy);
    }

    [Fact]
    public void SpaceMadness_OnAddToMap_Player_QueuesWipOnCreateCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        var script = new SpaceMadnessMapScript(CreateCinematicFactory(cinematic));
        script.OnLoad(CreateMap(out RecordingDispatchProxy<IPublicEvent> eventProxy));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);

        script.OnAddToMap(player);

        AssertPlayerJoinedAndCinematicQueued(player, cinematic, eventProxy, cinematicManagerProxy);
    }

    private static void AssertPlayerJoinedAndCinematicQueued(
        IPlayer player,
        ICinematicBase cinematic,
        RecordingDispatchProxy<IPublicEvent> eventProxy,
        RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
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
