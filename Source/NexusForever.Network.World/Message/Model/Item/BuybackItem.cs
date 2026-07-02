using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Item
{
    public class BuybackItem : IWritable
    {
        public uint UniqueId { get; set; }
        public uint Item2Id { get; set; }
        public uint Quantity { get; set; }
        public CraftStats CircuitData { get; set; } = new();
        public RuneSlots GlyphData { get; set; } = new();
        public ItemThresholds ThresholdData { get; set; } = new();
        public ulong MakerCharacterId { get; set; }
        public uint WorldReqItem2Id { get; set; }
        public uint[] ChargeAmounts { get; set; } = new uint[5];
        public uint[] GlyphItem2Ids { get; set; } = new uint[8];
        public ulong CurrencyAmountFirst { get; set; }
        public ulong CurrencyAmountSecond { get; set; }
        public CurrencyType CurrencyTypeIdFirst { get; set; }
        public CurrencyType CurrencyTypeIdSecond { get; set; }
        public WorldRequirements WorldRequirements { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UniqueId);
            writer.Write(Item2Id, 18u);
            writer.Write(Quantity);
            CircuitData.Write(writer);
            GlyphData.Write(writer);
            ThresholdData.Write(writer);
            writer.Write(MakerCharacterId);
            writer.Write(WorldReqItem2Id, 18u);

            foreach (uint chargeAmount in ChargeAmounts)
                writer.Write(chargeAmount);

            foreach (uint glyphItem2Id in GlyphItem2Ids)
                writer.Write(glyphItem2Id);

            writer.Write(CurrencyAmountFirst);
            writer.Write(CurrencyAmountSecond);
            writer.Write(CurrencyTypeIdFirst, 4u);
            writer.Write(CurrencyTypeIdSecond, 4u);
            WorldRequirements.Write(writer);
        }
    }
}
