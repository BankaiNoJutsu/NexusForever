using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Abstract.PublicEvent
{
    public interface IPublicEventTemplate
    {
        PublicEventEntry Entry { get; }
        Dictionary<uint, PublicEventObjectiveEntry> Objectives { get; }
        List<PublicEventTeamEntry> Teams { get; }
        List<PublicEventCustomStatEntry> CustomStats { get; }
        IReadOnlyList<uint> Locations { get; }
        IReadOnlyList<uint> ChildEventIds { get; }

        /// <summary>
        /// Initialise <see cref="IPublicEventTemplate"/> with <see cref="PublicEventEntry"/>.
        /// </summary>
        void Initialise(PublicEventEntry entry);

        /// <summary>
        /// Return virtual item depot choices for the supplied objective, when backed by public-event depot rows.
        /// </summary>
        IReadOnlyList<PublicEventObjectiveStatus.VirtualItem> GetObjectiveVirtualItems(PublicEventObjectiveEntry entry);

        /// <summary>
        /// Returns if event has live stats.
        /// </summary>
        /// <remarks>
        /// This determines if live stats should be periodically sent to members of the event.
        /// </remarks>
        bool HasLiveStats();
    }
}
