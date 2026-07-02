using NexusForever.Game.Static.Item;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.Item
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

        public ulong ItemGuid { get; set; }
        public ulong MakerCharacterId { get; set; }
        public uint Item2Id { get; set; }
        public ItemLocation LocationData { get; set; } = new();
        public uint StackCount { get; set; }
        public uint Charges { get; set; }
        public CraftStats CircuitData { get; set; } = new();
        public RuneSlots GlyphData { get; set; } = new();
        public ItemThresholds ThresholdData { get; set; } = new();
        public float Durability { get; set; }
        public uint DyeData { get; set; }
        public ItemFlags Flags { get; set; }
        public uint ReturnTimeRemaining { get; set; }
        public uint ExpireTimeRemaining { get; set; }
        public uint UpdateTimeOffset { get; set; }
        public PriceInfo SellPricePrimary { get; set; } = new();
        public PriceInfo SellPriceSecondary { get; set; } = new();
        public uint PowerCoreItem2Id { get; set; }
        public List<uint> Microchips { get; } = [];
        public List<uint> Glyphs { get; } = [];
        public List<Identity> TimeLimitedTradingPartnerIdentities { get; } = [];
        public WorldRequirements WorldRequirements { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(MakerCharacterId);
            writer.Write(Item2Id, 18u);
            LocationData.Write(writer);
            writer.Write(StackCount);
            writer.Write(Charges);
            CircuitData.Write(writer);
            GlyphData.Write(writer);
            ThresholdData.Write(writer);
            writer.Write(Durability);
            writer.Write(DyeData);
            writer.Write(Flags, 32u);
            writer.Write(ReturnTimeRemaining);
            writer.Write(ExpireTimeRemaining);
            writer.Write(UpdateTimeOffset);
            SellPricePrimary.Write(writer);
            SellPriceSecondary.Write(writer);
            writer.Write(PowerCoreItem2Id, 18u);

            writer.Write(Microchips.Count, 3u);
            foreach (uint microchip in Microchips)
                writer.Write(microchip);

            writer.Write(Glyphs.Count, 4u);
            foreach (uint glyph in Glyphs)
                writer.Write(glyph);

            writer.Write(TimeLimitedTradingPartnerIdentities.Count, 6u);
            foreach (Identity identity in TimeLimitedTradingPartnerIdentities)
                identity.Write(writer);

            WorldRequirements.Write(writer);
        }
    }
}
