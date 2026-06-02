using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Entity
{
    /// <summary>
    /// Entity-stat auxiliary triplet (<c>0x0889</c>). Registration uses
    /// <c>ServerSpellUInt32TripletListRow_ReadPayload</c> @ <c>140080bf0</c> (12 bytes), same reader as
    /// <see cref="ServerEntityThreatUpdate"/> and <see cref="ServerEntityTargetUnit"/>; distinct opcode semantics.
    /// </summary>
    /// <remarks>Per-opcode apply handler (<c>vtable+0x58</c>) not verified - keep <c>ValueN</c>; no production emit.</remarks>
    [Message(GameMessageOpcode.ServerEntityStatUInt32Triplet)]
    public class ServerEntityStatUInt32Triplet : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
        }
    }

    /// <summary>Entity-stat aux (<c>0x08CC</c>). Reader <c>ServerUInt32WideString_ReadPayload</c> @ <c>1400980f0</c>.</summary>
    /// <remarks>Wide-string role at apply time blocked - no production emit.</remarks>
    [Message(GameMessageOpcode.ServerEntityStatUInt32WideString)]
    public class ServerEntityStatUInt32WideString : Shared.ServerUInt32WideStringPayload
    {
    }

    /// <summary>Entity-stat aux (<c>0x08F4</c>). Reader <c>ServerEntityStatUInt32UInt5UInt32_ReadPayload</c> @ <c>140097620</c>.</summary>
    /// <remarks>5-bit <see cref="Value1"/> meaning blocked - no production emit.</remarks>
    [Message(GameMessageOpcode.ServerEntityStatUInt32UInt5UInt32)]
    public class ServerEntityStatUInt32UInt5UInt32 : IWritable
    {
        public uint Value0 { get; set; }

        /// <summary>5-bit field; enum/bit semantics blocked at apply site.</summary>
        public uint Value1 { get; set; }

        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 5u);
            writer.Write(Value2);
        }
    }

    /// <summary>Entity-stat aux (<c>0x0939</c>). Reader <c>ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload</c> @ <c>140097ee0</c>.</summary>
    /// <remarks>14/18-bit split fields blocked - no production emit.</remarks>
    [Message(GameMessageOpcode.ServerEntityStatUInt32UInt14UInt18WideString)]
    public class ServerEntityStatUInt32UInt14UInt18WideString : IWritable
    {
        public uint Value0 { get; set; }

        /// <summary>14-bit field; apply semantics blocked.</summary>
        public uint Value1 { get; set; }

        /// <summary>18-bit field; apply semantics blocked.</summary>
        public uint Value2 { get; set; }

        public string Text { get; set; } = string.Empty;

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 14u);
            writer.Write(Value2, 18u);
            writer.WriteStringWide(Text);
        }
    }

    /// <summary>Entity-stat aux (<c>0x093D</c>). Reader <c>ServerEntityStatUInt32UInt5Pair_ReadPayload</c> @ <c>140097690</c>.</summary>
    /// <remarks>Apply semantics blocked - no production emit.</remarks>
    [Message(GameMessageOpcode.ServerEntityStatUInt32UInt5Pair)]
    public class ServerEntityStatUInt32UInt5Pair : IWritable
    {
        public uint Value0 { get; set; }

        /// <summary>5-bit field; apply semantics blocked.</summary>
        public uint Value1 { get; set; }

        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 5u);
            writer.Write(Value2);
            writer.Write(Value3);
        }
    }

    /// <summary>Entity-stat aux (<c>0x093E</c>). Reader <c>ServerEntityStatTwoUInt32UInt64_ReadPayload</c> @ <c>140097f70</c>.</summary>
    /// <remarks>Apply semantics blocked - no production emit.</remarks>
    [Message(GameMessageOpcode.ServerEntityStatTwoUInt32UInt64)]
    public class ServerEntityStatTwoUInt32UInt64 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public ulong Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
        }
    }
}
