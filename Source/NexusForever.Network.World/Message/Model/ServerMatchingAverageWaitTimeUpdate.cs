using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerUInt5UInt32_ReadPayload</c> (<c>140081f00</c>).
    /// The exported body reads one 5-bit field then one uint32 into an 8-byte object; runtime
    /// evidence maps opcode <c>0x0628</c> to <see cref="Game.Static.Matching.MatchType"/> plus
    /// average wait time, while unresolved server opcode <c>0x0015</c> reuses the same reader.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingAverageWaitTimeUpdate)]
    public class ServerMatchingAverageWaitTimeUpdate : IWritable
    {
        public Game.Static.Matching.MatchType Type { get; set; }
        public uint AverageWaitTime { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Type, 5u);
            writer.Write(AverageWaitTime);
        }
    }
}
