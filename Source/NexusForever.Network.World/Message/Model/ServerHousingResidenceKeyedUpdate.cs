using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Housing privacy-cluster payload for opcode 0x00CA. Client reader FUN_14008de20
    /// reads one 64-bit field then one uint32 field; semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceKeyedUpdate)]
    public class ServerHousingResidenceKeyedUpdate : IWritable
    {
        public ulong Key { get; set; }
        public uint Unknown0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Key);
            writer.Write(Unknown0);
        }
    }
}
