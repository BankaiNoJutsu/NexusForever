using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Fixed 16-byte spell auxiliary payload for opcode 0x0812 (reader FUN_14007fef0).
    /// Field semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellFourUInt32)]
    public class ServerSpellFourUInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
        }
    }
}
