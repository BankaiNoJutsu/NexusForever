using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;

namespace NexusForever.Game.Tests.Instances;

public class EventBaseContentMapScriptTests
{
    [Fact]
    public void OnLoad_CreatesJoinsAndFinishesDataBackedChildEvents()
    {
        ChildRoutingMapScript script = CreateScript(
            out RecordingDispatchProxy<IPublicEventManager> managerProxy,
            out Dictionary<uint, RecordingDispatchProxy<IPublicEvent>> eventProxies,
            out IContentMapInstance contentMap);

        script.OnLoad(contentMap);

        Assert.Equal(
            [100u, 102u, 101u, 201u],
            managerProxy.GetInvocations(nameof(IPublicEventManager.CreateEvent))
                .Select(invocation => (uint)invocation.Arguments[0])
                .ToArray());

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        script.OnAddToMap(player);

        AssertJoined(eventProxies[100u], player);
        AssertJoined(eventProxies[102u], player);
        AssertJoined(eventProxies[101u], player);
        AssertJoined(eventProxies[201u], player);

        script.OnMatchFinish();

        AssertFinished(eventProxies[100u]);
        AssertFinished(eventProxies[102u]);
        AssertFinished(eventProxies[101u]);
        AssertFinished(eventProxies[201u]);
    }

    [Fact]
    public void OnPublicEventFinish_OnlyPrimaryEventFinishesMatch()
    {
        ChildRoutingMapScript script = CreateScript(
            out _,
            out Dictionary<uint, RecordingDispatchProxy<IPublicEvent>> eventProxies,
            out IContentMapInstance contentMap,
            out RecordingDispatchProxy<IMatch> matchProxy);
        script.OnLoad(contentMap);

        script.OnPublicEventFinish((IPublicEvent)(object)eventProxies[101u], null);
        Assert.Empty(matchProxy.GetInvocations(nameof(IMatch.MatchFinish)));

        script.OnPublicEventFinish((IPublicEvent)(object)eventProxies[100u], null);

        Assert.Single(matchProxy.GetInvocations(nameof(IMatch.MatchFinish)));
    }

    private static ChildRoutingMapScript CreateScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out Dictionary<uint, RecordingDispatchProxy<IPublicEvent>> eventProxies,
        out IContentMapInstance contentMap)
    {
        return CreateScript(out managerProxy, out eventProxies, out contentMap, out _);
    }

    private static ChildRoutingMapScript CreateScript(
        out RecordingDispatchProxy<IPublicEventManager> managerProxy,
        out Dictionary<uint, RecordingDispatchProxy<IPublicEvent>> eventProxies,
        out IContentMapInstance contentMap,
        out RecordingDispatchProxy<IMatch> matchProxy)
    {
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out managerProxy);
        Dictionary<uint, IPublicEvent> events = CreateEvents(out eventProxies);

        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args =>
        {
            uint publicEventId = (uint)args[0];
            return null;
        });
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
        {
            uint publicEventId = (uint)args[0];
            return events.GetValueOrDefault(publicEventId);
        });

        IMatch match = RecordingDispatchProxy<IMatch>.Create(out matchProxy);
        contentMap = RecordingDispatchProxy<IContentMapInstance>.Create(out RecordingDispatchProxy<IContentMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);
        mapProxy.SetProperty(nameof(IContentMapInstance.Match), match);

        return new ChildRoutingMapScript();
    }

    private static Dictionary<uint, IPublicEvent> CreateEvents(
        out Dictionary<uint, RecordingDispatchProxy<IPublicEvent>> eventProxies)
    {
        eventProxies = [];
        var events = new Dictionary<uint, IPublicEvent>();

        AddEvent(events, eventProxies, 100u, [101u, 102u, 101u]);
        AddEvent(events, eventProxies, 101u, [201u, 100u]);
        AddEvent(events, eventProxies, 102u, []);
        AddEvent(events, eventProxies, 201u, []);

        return events;
    }

    private static void AddEvent(
        IDictionary<uint, IPublicEvent> events,
        IDictionary<uint, RecordingDispatchProxy<IPublicEvent>> eventProxies,
        uint publicEventId,
        IReadOnlyList<uint> childEventIds)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), publicEventId);
        eventProxy.SetProperty(nameof(IPublicEvent.ChildEventIds), childEventIds);

        events.Add(publicEventId, publicEvent);
        eventProxies.Add(publicEventId, eventProxy);
    }

    private static void AssertJoined(RecordingDispatchProxy<IPublicEvent> eventProxy, IPlayer player)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[1]);
    }

    private static void AssertFinished(RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, invocation.Arguments[0]);
    }

    private sealed class ChildRoutingMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 100u;

        protected override IEnumerable<uint> AdditionalPublicEventIds => [102u];
    }
}
