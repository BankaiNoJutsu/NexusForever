using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Loot
{
    [Message(GameMessageOpcode.ServerLootItemUpdate)]
    public class ServerLootItemUpdate : IWritable
    {
        public LootItem LootItem { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            LootItem.Write(writer);
        }
    }
}
