using System.Numerics;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientMovementFallLand)]
    public class ClientMovementFallLand : IReadable
    {
        public bool LandingState { get; private set; }
        public Vector3 Position { get; private set; }

        public void Read(GamePacketReader reader)
        {
            LandingState = reader.ReadBit();
            Position     = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
