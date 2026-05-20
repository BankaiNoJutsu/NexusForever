using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientDashCast)]
    public class ClientDashCast : IReadable
    {
        public uint DirectionOrState { get; private set; }

        public void Read(GamePacketReader reader)
        {
            if (reader.BytesRemaining >= sizeof(uint))
                DirectionOrState = reader.ReadUInt();
        }
    }
}
