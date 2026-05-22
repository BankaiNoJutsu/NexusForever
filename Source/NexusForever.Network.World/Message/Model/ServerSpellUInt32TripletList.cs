using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    public class ServerSpellUInt32TripletListRow : IWritable
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

    /// <summary>
    /// Counted list of three uint32 fields per row. Client reader FUN_140095da0 at opcode 0x080F.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellUInt32TripletList)]
    public class ServerSpellUInt32TripletList : IWritable
    {
        public List<ServerSpellUInt32TripletListRow> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Rows.Count);
            Rows.ForEach(r => r.Write(writer));
        }
    }

    /// <summary>
    /// Opcode 0x0810 shares the 0x080F reader (FUN_140095da0); row semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellUInt32TripletListVariant)]
    public class ServerSpellUInt32TripletListVariant : IWritable
    {
        public List<ServerSpellUInt32TripletListRow> Rows { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Rows.Count);
            Rows.ForEach(r => r.Write(writer));
        }
    }
}
