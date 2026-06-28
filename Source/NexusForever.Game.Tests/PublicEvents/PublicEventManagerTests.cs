using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

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

    private static PublicEventManager CreateManager(
        out RecordingDispatchProxy<IPublicEventFactory> factoryProxy,
        out RecordingDispatchProxy<IPublicEvent> eventProxy)
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
            new PublicEventTestSupport.ThrowingFactory<IPublicEventCharacter>());

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry
        {
            Id = 7u
        });
        manager.Initialise(map);

        return manager;
    }
}
