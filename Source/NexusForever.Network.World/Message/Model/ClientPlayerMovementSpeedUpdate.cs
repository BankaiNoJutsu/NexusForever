using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientPlayerMovementSpeedUpdate)]
    public class ClientPlayerMovementSpeedUpdate : IReadable
    {
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }
}