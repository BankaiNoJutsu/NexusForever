using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpellStopCast)]
    public class ClientSpellStopCast : IReadable
    {
        public uint CastingId { get; private set; } // Active casting id echoed by the native 0x0801 stop-cast sender variants.
        public CastResult CastResult { get; set; }
        public bool TrailingFlag { get; private set; } // Native 0x0801 sender variants toggle this trailing bit.

        public void Read(GamePacketReader reader)
        {
            CastingId    = reader.ReadUInt();
            CastResult   = reader.ReadEnum<CastResult>(9u);
            TrailingFlag = reader.ReadBit();
        }
    }
}
