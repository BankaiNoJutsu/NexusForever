using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    // Toggles whether the PublicEventLiveStatsUpdate event will fire for this public event.
    // Client sender WildStar64.exe 14007c620 writes publicEventId u14 then Subscribe bit.
    [Message(GameMessageOpcode.ClientPublicEventRequestScoreboard)]
    public class ClientPublicEventRequestScoreboard : IReadable
    {
        public uint PublicEventId { get; private set; }
        public bool Subscribe { get; private set; } 

        public void Read(GamePacketReader reader)
        {
            PublicEventId = reader.ReadUInt(14u);
            Subscribe = reader.ReadBit();
        }
    }
}
