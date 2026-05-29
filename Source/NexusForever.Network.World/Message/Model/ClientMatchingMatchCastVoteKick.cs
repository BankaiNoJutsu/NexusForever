using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Only sent if a kick vote is active.
    /// Opcode 0x0617. Native client registration binds this request to shared
    /// <c>ClientRealmScopedIdAndBit_WritePayload</c> (<c>140099030</c>), the same
    /// 14-bit realm-scoped id plus 64-bit id plus trailing 1-bit writer reused by opcodes
    /// <c>0x0538</c> and <c>0x053C</c>.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingMatchCastVoteKick)]
    public class ClientMatchingMatchCastVoteKick : IReadable
    {
        public Identity MemberToKick { get; private set; } = new(); // The match member to kick
        public bool Vote { get; private set; } // true = yes, false = no

        public void Read(GamePacketReader reader)
        {
            MemberToKick.Read(reader);
            Vote = reader.ReadBit();
        }
    }
}
