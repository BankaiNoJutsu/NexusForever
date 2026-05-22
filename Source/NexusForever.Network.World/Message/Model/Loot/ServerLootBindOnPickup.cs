using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Loot
{
    // Client Loot_PrepareAndDispatchBindcheck keys the local loot tree from LootUnitId at payload +4.
    // The first uint is read on the wire but unused in the mapped bindcheck consumer; mirror OwnerUnitId for parity with ClientLootItem collect shape.
    [Message(GameMessageOpcode.ServerLootBindOnPickup)]
    public class ServerLootBindOnPickup : IWritable
    {
        public uint OwnerUnitId { get; set; }
        public uint LootUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(OwnerUnitId);
            writer.Write(LootUnitId);
        }
    }
}
