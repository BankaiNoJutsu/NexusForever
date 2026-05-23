using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Entity
{
    /// <summary>Shares three-uint32 reader with <see cref="ServerEntityThreatUpdate"/>; opcode <c>0x0889</c>.</summary>
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

    /// <summary>Reader <c>ServerUInt32WideString_ReadPayload</c> @ <c>1400980f0</c>; opcode <c>0x08CC</c>.</summary>
    [Message(GameMessageOpcode.ServerEntityStatUInt32WideString)]
    public class ServerEntityStatUInt32WideString : Shared.ServerUInt32WideStringPayload
    {
    }

    /// <summary>Reader <c>ServerEntityStatUInt32UInt5UInt32_ReadPayload</c> @ <c>140097620</c>; opcode <c>0x08F4</c>.</summary>
    [Message(GameMessageOpcode.ServerEntityStatUInt32UInt5UInt32)]
    public class ServerEntityStatUInt32UInt5UInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 5u);
            writer.Write(Value2);
        }
    }

    /// <summary>Reader <c>ServerEntityStatUInt32UInt14UInt18WideString_ReadPayload</c> @ <c>140097ee0</c>; opcode <c>0x0939</c>.</summary>
    [Message(GameMessageOpcode.ServerEntityStatUInt32UInt14UInt18WideString)]
    public class ServerEntityStatUInt32UInt14UInt18WideString : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
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

    /// <summary>Reader <c>ServerEntityStatUInt32UInt5Pair_ReadPayload</c> @ <c>140097690</c>; opcode <c>0x093D</c>.</summary>
    [Message(GameMessageOpcode.ServerEntityStatUInt32UInt5Pair)]
    public class ServerEntityStatUInt32UInt5Pair : IWritable
    {
        public uint Value0 { get; set; }
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

    /// <summary>Reader <c>ServerEntityStatTwoUInt32UInt64_ReadPayload</c> @ <c>140097f70</c>; opcode <c>0x093E</c>.</summary>
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
