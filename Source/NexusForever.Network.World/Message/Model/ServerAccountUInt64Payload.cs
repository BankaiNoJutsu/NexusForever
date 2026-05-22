using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Account-cluster uint64 payload for opcode 0x0986. Reader is mapped; no store or
    /// account inventory consumer has been proven yet.
    /// </summary>
    [Message(GameMessageOpcode.ServerAccountUInt64Payload)]
    public class ServerAccountUInt64Payload : IWritable
    {
        public ulong Value { get; set; }

        public ServerAccountUInt64Payload(ulong value = 0ul)
        {
            Value = value;
        }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
        }
    }
}
