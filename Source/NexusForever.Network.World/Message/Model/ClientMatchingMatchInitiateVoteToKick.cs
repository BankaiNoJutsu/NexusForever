using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05D1. Native client registration binds this request to shared local writer slot
    /// <c>LAB_1400867f0</c> with registered size <c>0x10</c>, the same structural
    /// realm-scoped-id surface reused by multiple client target-selection requests.
    /// The current model stays a single <see cref="Identity"/> payload.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingMatchInitiateVoteToKick)]
    public class ClientMatchingMatchInitiateVoteToKick : IReadable
    {
        public Identity MemberToKick { get; private set; } = new();

        public void Read(GamePacketReader reader)
        {
            MemberToKick.Read(reader);
        }
    }
}
