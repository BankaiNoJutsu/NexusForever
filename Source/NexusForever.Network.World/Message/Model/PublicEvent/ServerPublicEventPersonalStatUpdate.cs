using NexusForever.Game.Static.PublicEvent;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PublicEvent
{
    // Client reader WildStar64.exe 14007bb10 reads publicEventId u14, stat id u32, value u32.
    [Message(GameMessageOpcode.ServerPublicEventPersonalStatUpdate)]
    public class ServerPublicEventPersonalStatUpdate : IWritable
    {
        public uint PublicEventId { get; set; }
        public PublicEventStat StatType { get; set; }
        public int Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PublicEventId, 14u);
            writer.Write(StatType, 32u);
            writer.Write(Value);
        }
    }
}
