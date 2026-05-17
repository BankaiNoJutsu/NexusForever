using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpellCastState)]
    public class ClientSpellCastState : IReadable
    {
        public bool State { get; private set; }

        public void Read(GamePacketReader reader)
        {
            State = reader.ReadBit();
        }
    }
}