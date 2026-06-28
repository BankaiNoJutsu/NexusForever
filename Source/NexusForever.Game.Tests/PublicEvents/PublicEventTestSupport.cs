using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.GameTable.Model;
using NexusForever.Script;
using NexusForever.Shared;
using SharedPublicEventObjectiveStatus = NexusForever.Network.World.Message.Model.Shared.PublicEventObjectiveStatus;

namespace NexusForever.Game.Tests.PublicEvents;

internal static class PublicEventTestSupport
{
    public static PublicEventObjective CreateObjective(PublicEventObjectiveEntry entry)
    {
        return CreateTeamWithObjective(entry).Objective;
    }

    public static (PublicEventTeam Team, PublicEventObjective Objective) CreateTeamWithObjective(PublicEventObjectiveEntry entry)
    {
        var team = new PublicEventTeam(
            NullLogger<PublicEventTeam>.Instance,
            new DelegateFactory<IPublicEventObjective>(() => new PublicEventObjective()),
            new ThrowingFactory<IPublicEventTeamMember>(),
            new ThrowingFactory<IPublicEventVote>(),
            new PublicEventStats());

        IPublicEvent owner = TestSupport.RecordingDispatchProxy<IPublicEvent>.Create(out _);
        var template = new TestPublicEventTemplate(entry);
        team.Initialise(owner, template, template.Teams.Single());

        PublicEventObjective objective = Assert.IsType<PublicEventObjective>(Assert.Single(team.GetObjectives()));
        return (team, objective);
    }

    public static NexusForever.Game.PublicEvent.PublicEvent CreatePublicEvent(PublicEventObjectiveEntry entry, out TestSupport.RecordingDispatchProxy<IBaseMap> mapProxy)
    {
        IScriptManager scriptManager = TestSupport.RecordingDispatchProxy<IScriptManager>.Create(out _);
        IPublicEventEntityFactory entityFactory = TestSupport.RecordingDispatchProxy<IPublicEventEntityFactory>.Create(out _);

        var publicEvent = new NexusForever.Game.PublicEvent.PublicEvent(
            NullLogger<NexusForever.Game.PublicEvent.PublicEvent>.Instance,
            scriptManager,
            new DelegateFactory<IPublicEventTeam>(() => new PublicEventTeam(
                NullLogger<PublicEventTeam>.Instance,
                new DelegateFactory<IPublicEventObjective>(() => new PublicEventObjective()),
                new ThrowingFactory<IPublicEventTeamMember>(),
                new ThrowingFactory<IPublicEventVote>(),
                new PublicEventStats())),
            entityFactory);

        IPublicEventManager manager = TestSupport.RecordingDispatchProxy<IPublicEventManager>.Create(out _);
        IBaseMap map = TestSupport.RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        publicEvent.Initialise(manager, new TestPublicEventTemplate(entry), map);
        return publicEvent;
    }

    internal sealed class DelegateFactory<T>(Func<T> factory) : IFactory<T> where T : class
    {
        public T Resolve() => factory();
    }

    internal sealed class ThrowingFactory<T> : IFactory<T> where T : class
    {
        public T Resolve() => throw new NotSupportedException($"{typeof(T).Name} should not be resolved in this test.");
    }

    internal sealed class TestPublicEventTemplate(
        PublicEventObjectiveEntry objectiveEntry,
        IReadOnlyList<uint> locations = null,
        IReadOnlyList<SharedPublicEventObjectiveStatus.VirtualItem> virtualItems = null) : IPublicEventTemplate
    {
        public PublicEventEntry Entry { get; } = new()
        {
            Id = 9001
        };

        public Dictionary<uint, PublicEventObjectiveEntry> Objectives { get; } = new()
        {
            [objectiveEntry.Id] = objectiveEntry
        };

        public List<PublicEventTeamEntry> Teams { get; } =
        [
            new PublicEventTeamEntry
            {
                Id = objectiveEntry.PublicEventTeamId
            }
        ];

        public List<PublicEventCustomStatEntry> CustomStats { get; } = [];
        public IReadOnlyList<uint> Locations { get; } = locations ?? [];
        public IReadOnlyList<uint> ChildEventIds { get; } = [];
        private IReadOnlyList<SharedPublicEventObjectiveStatus.VirtualItem> VirtualItems { get; } = virtualItems ?? [];

        public void Initialise(PublicEventEntry entry) => throw new NotSupportedException();

        public IReadOnlyList<SharedPublicEventObjectiveStatus.VirtualItem> GetObjectiveVirtualItems(PublicEventObjectiveEntry entry)
        {
            return entry?.Id == objectiveEntry.Id ? VirtualItems : [];
        }

        public bool HasLiveStats() => false;
    }
}
