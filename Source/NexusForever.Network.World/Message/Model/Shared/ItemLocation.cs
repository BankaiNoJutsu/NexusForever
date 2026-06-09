using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    public class ItemLocation : IReadable, IWritable
    {
        private const uint RealmBankWireLocation = 0x0Au;

        public InventoryLocation Location { get; set; }
        public uint BagIndex { get; set; }

        public static InventoryLocation FromWireLocation(uint location)
        {
            return location == RealmBankWireLocation
                ? InventoryLocation.RealmBank
                : (InventoryLocation)location;
        }

        public static uint ToWireLocation(InventoryLocation location)
        {
            return location == InventoryLocation.RealmBank
                ? RealmBankWireLocation
                : (uint)location;
        }

        public static ulong ToDragDropData(InventoryLocation location, ushort slot)
        {
            return (ulong)ToWireLocation(location) << 8 | slot;
        }

        public void Read(GamePacketReader reader)
        {
            Location = FromWireLocation(reader.ReadUInt(9u));
            BagIndex = reader.ReadUInt();
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ToWireLocation(Location), 9u);
            writer.Write(BagIndex);
        }
    }
}
