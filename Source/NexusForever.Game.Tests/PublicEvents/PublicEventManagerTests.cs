using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.PublicEvents;

public class PublicEventManagerTests
{
    [Fact]
    public void CreateEvent_WhenAlreadyCreated_ReturnsExistingEventWithoutReinitialising()
    {
        PublicEventManager manager = CreateManager(
            out RecordingDispatchProxy<IPublicEventFactory> factoryProxy,
            out RecordingDispatchProxy<IPublicEvent> eventProxy);

        IPublicEvent first = manager.CreateEvent(42u);
        IPublicEvent second = manager.CreateEvent(42u);

        Assert.Same(first, second);
        Assert.Single(factoryProxy.GetInvocations(nameof(IPublicEventFactory.CreateEvent)));
        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Initialise)));
    }

    [Fact]
    public void AReplacementMapActorCannotUseOrRemoveTheOriginalRegistration()
    {
        var manager = CreateManager(out _, out var eventProxy,
            new PublicEventTestSupport.DelegateFactory<IPublicEventCharacter>(() =>
                new PublicEventCharacter(NullLogger<PublicEventCharacter>.Instance)));
        IPublicEvent publicEvent = manager.CreateEvent(42u);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), 42u);
        var map = Assert.IsAssignableFrom<IBaseMap>(Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Initialise))).Arguments[2]);
        IPlayer original = BuildPlayer();
        IPlayer replacement = BuildPlayer();
        manager.OnAddToMap(original);
        manager.AddEvent(original.CharacterId, publicEvent);
        eventProxy.Invocations.Clear();

        manager.OnCinematicFinish(replacement, 5u);
        int staleCinematicCalls = eventProxy.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)).Count;
        manager.OnRemoveFromMap(replacement);
        manager.UpdateObjective(original, PublicEventObjectiveType.Script, 9u, 1);

        Assert.Equal(0, staleCinematicCalls);
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.LeaveEvent)));
        Assert.Same(original, Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective))).Arguments[0]);
        manager.OnRemoveFromMap(original);
        Assert.Same(original, Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.LeaveEvent))).Arguments[0]);

        IPlayer BuildPlayer() => TestPlayerBuilder.Create().WithCharacterId(101ul).WithMap(map)
            .WithSession(RecordingDispatchProxy<IGameSession>.Create(out _)).Build();
    }

    [Theory]
    [InlineData(nameof(IPublicEvent.Finish))]
    [InlineData(nameof(IPublicEvent.Dispose))]
    public void Cleanup_ContinuesAfterAnEventCallbackFails(string callback)
    {
        IPublicEvent first = CreateEvent(42u, out var firstProxy);
        IPublicEvent second = CreateEvent(43u, out var secondProxy);
        var manager = CreateManager([first, second], out _);
        manager.CreateEvent(first.Id);
        manager.CreateEvent(second.Id);
        firstProxy.SetMethodHandler(callback, _ => throw new InvalidOperationException("Event teardown failed."));

        manager.Cleanup();
        manager.Cleanup();

        Assert.Empty(manager.GetEvents());
        AssertSingleCleanup(firstProxy);
        AssertSingleCleanup(secondProxy);
    }

    [Fact]
    public void Cleanup_RejectsNewRegistrationsAndIgnoresReentry()
    {
        IPublicEvent first = CreateEvent(42u, out var firstProxy);
        IPublicEvent second = CreateEvent(43u, out _);
        var manager = CreateManager([first, second], out var factoryProxy);
        manager.CreateEvent(first.Id);
        IPublicEvent lateEvent = first;
        bool reentered = false;
        firstProxy.SetMethodHandler(nameof(IPublicEvent.Finish), _ =>
        {
            if (!reentered)
            {
                reentered = true;
                lateEvent = manager.CreateEvent(second.Id);
                manager.Cleanup();
            }

            return null;
        });

        manager.Cleanup();

        Assert.True(reentered);
        Assert.Null(lateEvent);
        Assert.Empty(manager.GetEvents());
        AssertSingleCleanup(firstProxy);
        Assert.Single(factoryProxy.GetInvocations(nameof(IPublicEventFactory.CreateEvent)));
        Assert.Null(manager.CreateEvent(first.Id));
        manager.OnAddToMap(TestPlayerBuilder.Create().WithCharacterId(101ul).Build());
    }

    [Fact]
    public void Update_StopsWhenAnEventUnloadsTheMap()
    {
        IPublicEvent first = CreateEvent(42u, out var firstProxy);
        IPublicEvent second = CreateEvent(43u, out var secondProxy);
        var manager = CreateManager([first, second], out _);
        manager.CreateEvent(first.Id);
        manager.CreateEvent(second.Id);
        firstProxy.SetMethodHandler(nameof(IPublicEvent.Update), _ =>
        {
            firstProxy.SetProperty(nameof(IPublicEvent.IsFinalised), true);
            manager.Cleanup();
            return null;
        });

        manager.Update(1d);

        Assert.Empty(manager.GetEvents());
        AssertSingleCleanup(firstProxy);
        AssertSingleCleanup(secondProxy);
        Assert.Empty(secondProxy.GetInvocations(nameof(IPublicEvent.Update)));
    }

    [Fact]
    public void CreateEvent_CleansUpAnEventThatUnloadsTheMapDuringInitialisation()
    {
        IPublicEvent first = CreateEvent(42u, out var firstProxy);
        var manager = CreateManager([first], out _);
        firstProxy.SetMethodHandler(nameof(IPublicEvent.Initialise), _ =>
        {
            manager.Cleanup();
            return null;
        });

        Assert.Null(manager.CreateEvent(first.Id));
        Assert.Empty(manager.GetEvents());
        AssertSingleCleanup(firstProxy);
    }

    private static void AssertSingleCleanup(RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Dispose)));
    }

    private static IPublicEvent CreateEvent(uint id, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), id);
        return publicEvent;
    }

    private static PublicEventManager CreateManager(
        out RecordingDispatchProxy<IPublicEventFactory> factoryProxy,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        IFactory<IPublicEventCharacter> characterFactory = null)
    {
        return CreateManager([CreateEvent(42u, out eventProxy)], out factoryProxy, characterFactory);
    }

    private static PublicEventManager CreateManager(
        IReadOnlyList<IPublicEvent> events,
        out RecordingDispatchProxy<IPublicEventFactory> factoryProxy,
        IFactory<IPublicEventCharacter> characterFactory = null)
    {
        var eventsById = events.ToDictionary(e => e.Id);
        IPublicEventTemplateManager templateManager = RecordingDispatchProxy<IPublicEventTemplateManager>.Create(
            out RecordingDispatchProxy<IPublicEventTemplateManager> templateManagerProxy);
        templateManagerProxy.SetMethodHandler(nameof(IPublicEventTemplateManager.GetTemplate), args =>
        {
            uint id = (uint)args[0];
            if (!eventsById.ContainsKey(id))
                return null;

            IPublicEventTemplate template = RecordingDispatchProxy<IPublicEventTemplate>.Create(out var templateProxy);
            templateProxy.SetProperty(nameof(IPublicEventTemplate.Entry), new PublicEventEntry { Id = id, WorldId = 7u });
            return template;
        });

        IPublicEventFactory eventFactory = RecordingDispatchProxy<IPublicEventFactory>.Create(out factoryProxy);
        factoryProxy.SetMethodHandler(nameof(IPublicEventFactory.CreateEvent), args =>
            eventsById[(uint)args[0]]);

        var manager = new PublicEventManager(
            NullLogger<PublicEventManager>.Instance,
            templateManager,
            eventFactory,
            characterFactory ?? new PublicEventTestSupport.ThrowingFactory<IPublicEventCharacter>());

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry
        {
            Id = 7u
        });
        manager.Initialise(map);

        return manager;
    }
}
