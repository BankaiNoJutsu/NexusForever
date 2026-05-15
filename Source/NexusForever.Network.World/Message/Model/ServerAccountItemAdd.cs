using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerAccountItemAdd)]
    public class ServerAccountItemAdd : IWritable
    {
        public AccountInventoryItem AccountItem { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            AccountItem.Write(writer);
        }
    }
}
