using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Server trade-window item row (opcode 0x018E, native object size 0x50).
    /// Tail layout matches <see cref="Mail.ServerMailAvailable.Attachment"/> after stack count,
    /// except trade omits charges and the eight glyph slots.
    /// Client read: FUN_1400aa2b0 @ 1400aa2b0; write: FUN_1400aa030 @ 1400aa030.
    /// </summary>
    [Message(GameMessageOpcode.ServerP2PTradeUpdateItem)]
    public class ServerP2PTradeUpdateItem : IWritable
    {
        public uint TradeIndex { get; set; }
        public uint OwnerUnitId { get; set; }
        public uint ItemId { get; set; }
        public ulong ItemGuid { get; set; }
        public uint StackCount { get; set; }
        public ulong RandomCircuitData { get; set; }
        public uint RandomGlyphData { get; set; }
        public ulong ThresholdData { get; set; }
        public uint WorldRequirement_Item2Id { get; set; }
        /// <summary>
        /// Fixed five microchip item ids (20 bytes on wire). Same shape as mail
        /// <see cref="Mail.ServerMailAvailable.Attachment.Item2IdMicrochip"/>.
        /// </summary>
        public uint[] Item2IdMicrochip { get; set; } = new uint[5];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(TradeIndex);
            writer.Write(OwnerUnitId);
            writer.Write(ItemId, 18);
            writer.Write(ItemGuid);
            writer.Write(StackCount);
            writer.Write(RandomCircuitData);
            writer.Write(RandomGlyphData);
            writer.Write(ThresholdData);
            writer.Write(WorldRequirement_Item2Id, 18);
            foreach (uint microchipId in Item2IdMicrochip)
                writer.Write(microchipId);
        }
    }
}
