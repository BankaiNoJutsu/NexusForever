using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    // Client reader WildStar64.exe 14007bde0 reads publicEventId u14,
    // counted PublicEventTeamStats rows, then counted PublicEventParticipantStats rows.
    [Message(GameMessageOpcode.ServerPublicEventStatsUpdate)]
    public class ServerPublicEventStatsUpdate : IWritable
    {
        public uint PublicEventId { get; set; }
        public List<PublicEventTeamStats> TeamStats { get; set; } = [];
        public List<PublicEventParticipantStats> ParticipantStats { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PublicEventId, 14);

            writer.Write(TeamStats.Count);
            foreach (PublicEventTeamStats stats in TeamStats)
                stats.Write(writer);

            writer.Write(ParticipantStats.Count);
            foreach (PublicEventParticipantStats stats in ParticipantStats)
                stats.Write(writer);
        }
    }
}
