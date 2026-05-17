using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpline2DataRequest)]
    public class ClientSpline2DataRequest : IReadable
    {
        public uint Spline2Id { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Spline2Id = reader.ReadUInt();
        }
    }
}
