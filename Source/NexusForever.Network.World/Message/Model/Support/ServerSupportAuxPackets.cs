using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.Support
{
    /// <summary>
    /// Support / character-admin cluster (0x0347, 16-byte object). Reader <c>ServerUInt32WideString_ReadPayload</c> @ <c>1400980f0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportUInt32WideString)]
    public class ServerSupportUInt32WideString : ServerUInt32WideStringPayload
    {
    }

    /// <summary>
    /// Support cluster (0x0348, 48 bytes). Reader <c>FUN_1400a40c0</c> @ <c>1400a40c0</c>: three uint32 header plus three uint32 triplets.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportUInt32HeaderAndTriplets)]
    public class ServerSupportUInt32HeaderAndTriplets : IWritable
    {
        public uint Header0 { get; set; }
        public uint Header1 { get; set; }
        public uint Header2 { get; set; }
        public ServerUInt32Triplet Row0 { get; set; } = new();
        public ServerUInt32Triplet Row1 { get; set; } = new();
        public ServerUInt32Triplet Row2 { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Header0);
            writer.Write(Header1);
            writer.Write(Header2);
            Row0.Write(writer);
            Row1.Write(writer);
            Row2.Write(writer);
        }
    }

    [Message(GameMessageOpcode.ServerSupportEmptyAck349)]
    public class ServerSupportEmptyAck349 : ServerEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerSupportUInt32Ack34A)]
    public class ServerSupportUInt32Ack34A : ServerUInt32Payload
    {
    }

    [Message(GameMessageOpcode.ServerSupportEmptyAck34B)]
    public class ServerSupportEmptyAck34B : ServerEmptyPayload
    {
    }

    [Message(GameMessageOpcode.ServerSupportUInt32Ack350)]
    public class ServerSupportUInt32Ack350 : ServerUInt32Payload
    {
    }

    /// <summary>
    /// Support cluster (0x034C, 28 bytes). Reader <c>FUN_1400a4040</c> @ <c>1400a4040</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportUInt5AndSixUInt32)]
    public class ServerSupportUInt5AndSixUInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }
        public uint Value5 { get; set; }
        public uint Value6 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0, 5u);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Value4);
            writer.Write(Value5);
            writer.Write(Value6);
            writer.Write(0u, 27u);
        }
    }

    /// <summary>
    /// Support cluster (0x034D, 64 bytes). Reader <c>FUN_1400a4150</c> @ <c>1400a4150</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportUInt32HeaderAndFourTriplets)]
    public class ServerSupportUInt32HeaderAndFourTriplets : IWritable
    {
        public uint Header0 { get; set; }
        public uint Header1 { get; set; }
        public uint Header2 { get; set; }
        public uint Header3 { get; set; }
        public ServerUInt32Triplet Column0 { get; set; } = new();
        public ServerUInt32Triplet Column1 { get; set; } = new();
        public ServerUInt32Triplet Column2 { get; set; } = new();
        public ServerUInt32Triplet Column3 { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Header0);
            writer.Write(Header1);
            writer.Write(Header2);
            writer.Write(Header3);
            Column0.Write(writer);
            Column1.Write(writer);
            Column2.Write(writer);
            Column3.Write(writer);
        }
    }

    public class ServerSupportSurveyRow : IWritable
    {
        public string Text { get; set; } = string.Empty;
        public ulong Value0 { get; set; }
        public ulong Value1 { get; set; }
        public ulong Value2 { get; set; }
        public ulong Value3 { get; set; }
        public ulong Value4 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Text);
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Value4);
        }
    }

    /// <summary>
    /// Support cluster (0x034E). Reader <c>FUN_1400a4260</c> @ <c>1400a4260</c>: wide string, five uint32 header fields, counted rows.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportSurveyList)]
    public class ServerSupportSurveyList : IWritable
    {
        public string Text { get; set; } = string.Empty;
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        /// <summary>
        /// Computed convenience property for diagnostic / log use only.
        /// The wire serialises <c>Rows.Count</c> directly in <see cref="Write"/>; this property is not used for writing.
        /// </summary>
        public uint RowCount => (uint)Rows.Count;
        public List<ServerSupportSurveyRow> Rows { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Text);
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write((uint)Rows.Count);
            foreach (ServerSupportSurveyRow row in Rows)
                row.Write(writer);
        }
    }

    /// <summary>
    /// Support cluster (0x034F, 8 bytes). Reader <c>FUN_1400a3f70</c> @ <c>1400a3f70</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportUInt32AndFlags)]
    public class ServerSupportUInt32AndFlags : IWritable
    {
        public uint Value { get; set; }
        public uint Flags { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.Write(Flags, 2u);
            writer.Write(0u, 30u);
        }
    }

    /// <summary>
    /// Support cluster (0x0351, 32 bytes). Reader <c>FUN_1400a3fd0</c> @ <c>1400a3fd0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerSupportUInt32AndTwoTriplets)]
    public class ServerSupportUInt32AndTwoTriplets : IWritable
    {
        public uint LeadingValue { get; set; }
        public ServerUInt32Triplet Triplet0 { get; set; } = new();
        public ServerUInt32Triplet Triplet1 { get; set; } = new();
        public uint TrailingValue { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(LeadingValue);
            Triplet0.Write(writer);
            Triplet1.Write(writer);
            writer.Write(TrailingValue);
        }
    }
}
