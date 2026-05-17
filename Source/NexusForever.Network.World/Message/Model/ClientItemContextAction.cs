using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientItemContextAction)]
    public class ClientItemContextAction : IReadable
    {
        public ulong ItemGuid { get; private set; }
        public bool SelectedBranch { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ItemGuid = reader.ReadULong();
            SelectedBranch = reader.ReadBit();
        }
    }
}