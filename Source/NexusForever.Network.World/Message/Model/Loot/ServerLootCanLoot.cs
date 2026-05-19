using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Loot
{
    // Packet model retained for evidence-backed validation only.
    // Runtime currently sends CanLoot inside ServerLootNotify instead of enqueuing this opcode.
    [Message(GameMessageOpcode.ServerLootCanLoot)]
    public class ServerLootCanLoot : IWritable
    {
        public uint LootUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(LootUnitId);
        }
    }
}
