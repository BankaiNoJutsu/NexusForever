using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.State)]
    public class State : IReadable, IWritable
    {
        public byte[] Payload { get; private set; } = [];

        public void Read(GamePacketReader reader)
        {
            Payload = reader.ReadBytes(reader.BytesRemaining);
        }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteBytes(Payload);
        }
    }
}
