using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpellToggleCast)]
    public class ClientSpellToggleCast : IReadable
    {
        public bool Enabled { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Enabled = reader.ReadBit();
        }
    }
}