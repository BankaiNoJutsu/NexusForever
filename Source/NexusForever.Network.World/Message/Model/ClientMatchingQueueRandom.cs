using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05F8. Wire layout from <c>ClientMatchingQueueRandom_WritePayload</c> @ <c>140098c00</c>:
    /// one 5-bit <c>MatchType</c>, one 32-bit <see cref="MatchingQueueFlags"/>, and one 32-bit <see cref="Role"/> bitfield.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingQueueRandom)]
    public class ClientMatchingQueueRandom : IReadable
    {
        public Game.Static.Matching.MatchType MatchType { get; private set; }
        public MatchingQueueFlags Flags { get; private set; }
        public Role Roles { get; private set; }

        public void Read(GamePacketReader reader)
        {
            MatchType = reader.ReadEnum<Game.Static.Matching.MatchType>(5u);
            Flags     = reader.ReadEnum<MatchingQueueFlags>(32u);
            Roles     = reader.ReadEnum<Role>(32u);
        }
    }
}
