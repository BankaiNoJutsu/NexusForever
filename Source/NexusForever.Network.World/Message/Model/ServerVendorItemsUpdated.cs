using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerVendorItemsUpdated)]
    public class ServerVendorItemsUpdated : IWritable
    {
        public class VendorGroup : IWritable
        {
            public uint GroupIndex { get; set; }
            public uint LocalisedTextId { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(GroupIndex);
                writer.Write(LocalisedTextId);
            }
        }

        public class VendorItem : IWritable
        {
            public class ItemExtraCost : IWritable
            {
                public ItemExtraCostType ExtraCostType { get; set; }
                public uint Quantity { get; set; }
                public uint ItemOrCurrencyId { get; set; }

                public void Write(GamePacketWriter writer)
                {
                    writer.Write(ExtraCostType, 3);
                    writer.Write(Quantity);
                    writer.Write(ItemOrCurrencyId);
                }
            }

            public uint StockUniqueId { get; set; }
            public byte VendorItemType { get; set; }
            public uint StaticDbId { get; set; }
            public uint RewardOption { get; set; }
            public uint StockCount { get; set; }
            public uint PrerequisiteId { get; set; }
            public uint ExchangeRate { get; set; }
            public uint VendorGroupId { get; set; }
            public uint Flags { get; set; }
            public ulong CircuitData { get; set; }
            public uint GlyphData { get; set; }
            public ItemExtraCost ExtraCost1 { get; set; }
            public ItemExtraCost ExtraCost2 { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(StockUniqueId);
                writer.Write(VendorItemType, 4);
                writer.Write(StaticDbId);
                writer.Write(RewardOption);
                writer.Write(StockCount);
                writer.Write(PrerequisiteId, 17);
                writer.Write(ExchangeRate);
                writer.Write(VendorGroupId);
                writer.Write(Flags);
                writer.Write(CircuitData);
                writer.Write(GlyphData);
                ExtraCost1.Write(writer);
                ExtraCost2.Write(writer);
            }
        }

        public uint VendorUnitId { get; set; }
        public List<VendorGroup> VendorGroups { get; } = new();
        public List<VendorItem> VendorItems { get; } = new();
        public float SellPriceMultiplier { get; set; }
        public float BuyPriceMultiplier { get; set; }
        public bool InitialList { get; set; }
        public bool Failed { get; set; }
        public bool ClearList { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(VendorUnitId);

            writer.Write(VendorGroups.Count);
            VendorGroups.ForEach(c => c.Write(writer));
            writer.Write(VendorItems.Count);
            VendorItems.ForEach(i => i.Write(writer));

            writer.Write(SellPriceMultiplier);
            writer.Write(BuyPriceMultiplier);
            writer.Write(InitialList);
            writer.Write(Failed);
            writer.Write(ClearList);
        }
    }
}
