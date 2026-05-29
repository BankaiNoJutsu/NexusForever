using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05B6. Native client registration binds this request to shared local writer slot
    /// <c>LAB_14008a150</c> and reader slot <c>LAB_14008a140</c> with registered size <c>4</c>, the same
    /// 4-byte structural slot reused by opcode <c>0x05B5</c> and unresolved opcode <c>0x00C8</c>.
    /// The current model stays a single <c>MatchType</c> payload.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingQueueLeaveAsGroup)]
    public class ClientMatchingQueueLeaveAsGroup : IReadable
    {
        public Game.Static.Matching.MatchType MatchType { get; private set; }

        public void Read(GamePacketReader reader)
        {
            MatchType = reader.ReadEnum<Game.Static.Matching.MatchType>(5u);
        }
    }
}
