using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientMovementFallDamage)]
    public class ClientMovementFallDamage : IReadable
    {
        public float FallStateValue { get; private set; }

        public void Read(GamePacketReader reader)
        {
            FallStateValue = reader.ReadSingle();
        }
    }
}