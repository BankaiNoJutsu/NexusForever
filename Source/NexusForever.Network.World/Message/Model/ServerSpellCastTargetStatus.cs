using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerSpellCastTargetStatus)]
    public class ServerSpellCastTargetStatus : IWritable
    {
        public uint CastingId { get; set; }
        public ushort Unknown5 { get; set; }
        public uint CasterId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(CastingId);
            writer.Write(Unknown5, 9u);
            writer.Write(CasterId);
        }
    }
}

