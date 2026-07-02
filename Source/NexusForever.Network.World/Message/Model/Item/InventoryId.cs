using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Item
{
    public class InventoryId : IReadable, IWritable
    {
        public byte BagSlot { get; set; }
        public InventoryLocation Location { get; set; }
        public ushort Count { get; set; }

        public static InventoryId FromLocation(InventoryLocation location, ushort slot, ushort count = 0)
        {
            if (slot > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Inventory drag/drop slot exceeds the 8-bit wire limit.");

            return new InventoryId
            {
                BagSlot  = (byte)slot,
                Location = location,
                Count    = count
            };
        }

        public void Write(GamePacketWriter writer)
        {
            ulong value = BagSlot
                | (ulong)(byte)ItemLocation.ToWireLocation(Location) << 8
                | (ulong)Count << 16;

            writer.Write(value);
        }

        public void Read(GamePacketReader reader)
        {
            ulong value = reader.ReadULong();

            BagSlot  = (byte)(value & 0xFF);
            Location = ItemLocation.FromWireLocation((uint)((value >> 8) & 0xFF));
            Count    = (ushort)((value >> 16) & 0xFFFF);
        }
    }
}
