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

    private static PublicEventManager CreateManager(
        out RecordingDispatchProxy<IPublicEventFactory> factoryProxy,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        IFactory<IPublicEventCharacter> characterFactory = null)
    {
        IPublicEventTemplate template = RecordingDispatchProxy<IPublicEventTemplate>.Create(
            out RecordingDispatchProxy<IPublicEventTemplate> templateProxy);
        templateProxy.SetProperty(nameof(IPublicEventTemplate.Entry), new PublicEventEntry
        {
            Id = 42u,
            WorldId = 7u
        });

        IPublicEventTemplateManager templateManager = RecordingDispatchProxy<IPublicEventTemplateManager>.Create(
            out RecordingDispatchProxy<IPublicEventTemplateManager> templateManagerProxy);
        templateManagerProxy.SetMethodHandler(nameof(IPublicEventTemplateManager.GetTemplate), args =>
            (uint)args[0] == 42u ? template : null);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        IPublicEventFactory eventFactory = RecordingDispatchProxy<IPublicEventFactory>.Create(out factoryProxy);
        factoryProxy.SetMethodHandler(nameof(IPublicEventFactory.CreateEvent), args =>
            (uint)args[0] == 42u ? publicEvent : null);

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
