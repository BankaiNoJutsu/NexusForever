using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Harvest result rows consumed by <c>Harvest_DispatchItemsSentToOwnerEvent</c> (WildStar64 @ 1404bbb80).
    /// Wire layout: item2 id array then count array (client builds Lua <c>{ item, nCount }</c> tables).
    /// Opcode registration remains inference-level until a static <c>FUN_14006c290</c> row is confirmed.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingHarvestItemsSentToOwner)]
    public class ServerHousingHarvestItemsSentToOwner : IWritable
    {
        public class HarvestItemRow
        {
            public uint Item2Id { get; set; }
            public uint Count { get; set; }
        }

        public List<HarvestItemRow> Items { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Items.Count);
            foreach (HarvestItemRow row in Items)
                writer.Write(row.Item2Id);

            foreach (HarvestItemRow row in Items)
                writer.Write(row.Count);
        }
    }
}
