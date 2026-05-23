using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Entity
{
    public class ServerEntityCreateAuxUInt32Triple : IWritable
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

    /// <summary>Entity-create auxiliary row; reader <c>ServerEntityCreateAuxRow_ReadPayload</c> @ <c>140095c20</c> (opcode <c>0x025F</c>).</summary>
    public class ServerEntityCreateAuxRow : IWritable
    {
        public ushort Value0 { get; set; }
        public uint Value1 { get; set; }
        public ServerEntityCreateAuxUInt32Triple Value2 { get; set; } = new();
        public uint Value5 { get; set; }
        public ServerEntityCreateAuxUInt32Triple Value6 { get; set; } = new();
        public uint Value9 { get; set; }
        public uint Value10 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0, 16u);
            writer.Write(Value1);
            Value2.Write(writer);
            writer.Write(Value5);
            Value6.Write(writer);
            writer.Write(Value9);
            writer.Write(Value10);
        }
    }

    [Message(GameMessageOpcode.ServerEntityCreateAuxRow)]
    public class ServerEntityCreateAuxSingleRow : ServerEntityCreateAuxRow
    {
    }

    [Message(GameMessageOpcode.ServerEntityCreateAuxRowList)]
    public class ServerEntityCreateAuxRowList : IWritable
    {
        public List<ServerEntityCreateAuxRow> Rows { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Rows.Count);
            Rows.ForEach(row => row.Write(writer));
        }
    }

    /// <summary>Entity-create auxiliary bit-packed row; reader <c>ServerEntityCreateAuxBitPackedRow_ReadPayload</c> @ <c>1400959c0</c> (opcode <c>0x0263</c>).</summary>
    public class ServerEntityCreateAuxBitPackedRow : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }
        public uint Value5 { get; set; }
        public ServerEntityCreateAuxUInt32Triple Value6 { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2, 17u);
            writer.Write(Value3, 17u);
            writer.Write(Value4, 17u);
            writer.Write(Value5);
            Value6.Write(writer);
        }
    }

    [Message(GameMessageOpcode.ServerEntityCreateAuxBitPackedRow)]
    public class ServerEntityCreateAuxSingleBitPackedRow : ServerEntityCreateAuxBitPackedRow
    {
    }

    [Message(GameMessageOpcode.ServerEntityCreateAuxBitPackedRowList)]
    public class ServerEntityCreateAuxBitPackedRowList : IWritable
    {
        public List<ServerEntityCreateAuxBitPackedRow> Rows { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Rows.Count);
            Rows.ForEach(row => row.Write(writer));
        }
    }

    /// <summary>Entity-create auxiliary scalar list; reader <c>ServerEntityCreateAuxScalarList_ReadPayload</c> @ <c>140095b40</c> (opcode <c>0x0264</c>).</summary>
    [Message(GameMessageOpcode.ServerEntityCreateAuxScalarList)]
    public class ServerEntityCreateAuxScalarList : IWritable
    {
        public uint Value0 { get; set; }
        public ushort Value1 { get; set; }
        public uint Value2 { get; set; }
        public List<uint> Values { get; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 16u);
            writer.Write(Value2);
            writer.Write((uint)Values.Count);
            Values.ForEach(value => writer.Write(value));
        }
    }
}
