using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Housing privacy-cluster payload for opcode 0x00CA. Client reader FUN_14008de20
    /// reads one 64-bit field then two uint32 fields; semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceKeyedUpdate)]
    public class ServerHousingResidenceKeyedUpdate : IWritable
    {
        public ulong Key { get; set; }
        public uint Unknown0 { get; set; }
        public uint Reserved0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Key);
            writer.Write(Unknown0);
            writer.Write(Reserved0);
        }
    }
}
