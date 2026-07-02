using NexusForever.Network.Message;
using SharedItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Network.World.Message.Model.Utility
{
    [Message(GameMessageOpcode.ServerInspectPlayerResponse)]
    public class ServerInspectPlayerResponse : IWritable
    {
        public uint UnitId { get; set; }
        public List<SharedItem> Items { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);

            writer.Write(Items.Count, 5u);
            Items.ForEach(item => item.Write(writer));
        }
    }
}
