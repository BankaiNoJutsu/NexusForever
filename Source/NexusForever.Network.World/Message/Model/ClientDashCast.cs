using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientDashCast)]
    public class ClientDashCast : IReadable
    {
        public uint DirectionOrState { get; private set; }

        public void Read(GamePacketReader reader)
        {
            // Native writer 14007edf0 emits three bits. Keep the existing
            // empty-body recovery used by short legacy requests.
            if (reader.BytesRemaining != 0)
                DirectionOrState = reader.ReadUInt(3u);
        }
    }
}
