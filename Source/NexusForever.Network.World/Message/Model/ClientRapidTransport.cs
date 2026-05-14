using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRapidTransport)]
    public class ClientRapidTransport : IReadable
    {
        public ushort TaxiNode { get; private set; }
        // Client-supplied timestamp/sequence; semantics are still unmapped.
        public uint Time { get; private set; }

        public void Read(GamePacketReader reader)
        {
            TaxiNode  = reader.ReadUShort(14u);
            Time      = reader.ReadUInt();
        }
    }
}
