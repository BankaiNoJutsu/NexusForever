using NexusForever.Network.Message;

using NexusForever.Game.Static.Item;

namespace NexusForever.Network.World.Message.Model.Shared
{
    public class Item : IWritable
    {
        public class PriceInfo : IWritable
        {
            public byte CostType { get; set; }
            public uint Amount { get; set; }
            public uint Value { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(CostType, 3u);
                writer.Write(Amount);
                writer.Write(Value);
            }
        }

        public class TradingPartnerInfo : IWritable
        {
            public ushort Unknown0 { get; set; }
            public ulong Unknown8 { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(Unknown0, 14u);
                writer.Write(Unknown8);
            }
        }

        public ulong ItemGuid { get; set; }
        public ulong MakerCharacterId { get; set; }
        public uint Item2Id { get; set; }
        public ItemLocation LocationData { get; set; }
        public uint StackCount { get; set; }
        public uint Charges { get; set; }
        public ulong RandomCircuitData { get; set; }
        public uint RandomGlyphData { get; set; }
        public ulong ThresholdData { get; set; }
        public float Durability { get; set; }
        public uint Unknown44 { get; set; }
        public byte Unknown48 { get; set; }
        public uint DyeData { get; set; }
        public DynamicItemFlags DynamicFlags { get; set; }
        public uint ExpirationTimeLeft { get; set; }
        public PriceInfo[] SellPrices { get; set; }
        public uint PowerCoreItem2Id { get; set; }
        public List<uint> Microchips { get; } = new();
        public List<uint> Glyphs { get; } = new();
        public List<TradingPartnerInfo> TimeLimitedTradingPartners { get; } = new();
        public uint EffectiveItemLevel { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(MakerCharacterId);
            writer.Write(Item2Id, 18u);
            LocationData.Write(writer);
            writer.Write(StackCount);
            writer.Write(Charges);
            writer.Write(RandomCircuitData);
            writer.Write(RandomGlyphData);
            writer.Write(ThresholdData);
            writer.Write(Durability);
            writer.Write(Unknown44);
            writer.Write(Unknown48);
            writer.Write(DyeData);
            writer.Write(DynamicFlags, 32u);
            writer.Write(ExpirationTimeLeft);

            for (uint i = 0u; i < SellPrices.Length; i++)
                SellPrices[i].Write(writer);

            writer.Write(PowerCoreItem2Id, 18u);

            writer.Write(Microchips.Count, 3u);
            Microchips.ForEach(m => writer.Write(m));
            writer.Write(Glyphs.Count, 4u);
            Glyphs.ForEach(g => writer.Write(g));
            writer.Write(TimeLimitedTradingPartners.Count, 6u);
            TimeLimitedTradingPartners.ForEach(u => u.Write(writer));

            writer.Write(EffectiveItemLevel);
        }
    }
}
