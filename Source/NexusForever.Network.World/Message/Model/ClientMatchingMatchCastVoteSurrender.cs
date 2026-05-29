using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Only sent if a surrender vote is active.
    /// If the current match is PvP this is a surrender vote; in PvE it is a disband vote.
    /// Opcode 0x0624. Native client registration binds this request to shared
    /// <c>ClientBool_WritePayload</c> (<c>14007e610</c>).
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingMatchCastVoteSurrender)]
    public class ClientMatchingMatchCastVoteSurrender : IReadable
    {
        public bool Vote { get; private set; } // true = yes, false = no

        public void Read(GamePacketReader reader)
        {
            Vote = reader.ReadBit();
        }
    }
}
