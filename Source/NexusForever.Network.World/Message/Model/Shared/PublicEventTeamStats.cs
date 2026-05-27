using NexusForever.Game.Static.PublicEvent;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    // Client reader WildStar64.exe 14007b9e0 reads team id u14 then PublicEventStats.
    public class PublicEventTeamStats : IWritable
    {
        public PublicEventTeam TeamId { get; set; }
        public PublicEventStats Stats { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(TeamId, 14);
            Stats.Write(writer);
        }
    }
}
