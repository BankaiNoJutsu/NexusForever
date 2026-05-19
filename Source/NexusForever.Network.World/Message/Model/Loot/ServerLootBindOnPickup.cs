using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Loot
{
    // Packet model retained for evidence-backed validation only.
    // Runtime does not currently enqueue this opcode until bind-confirmation semantics are confirmed.
    [Message(GameMessageOpcode.ServerLootBindOnPickup)]
    public class ServerLootBindOnPickup : IWritable
    {
        public uint Unused { get; set; }
        public uint LootUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Unused);
            writer.Write(LootUnitId);
        }
    }
}
