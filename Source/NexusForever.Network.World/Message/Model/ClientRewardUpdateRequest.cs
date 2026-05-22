using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRewardUpdateRequest)]
    public class ClientRewardUpdateRequest : IReadable
    {
        public uint RewardRotationIndex { get; private set; }

        /// <summary>
        /// Optional claim target when the client sends trailing fields after the rotation index.
        /// Retail <c>Reward_SendRewardUpdateRequest</c> (140636ba0) currently sends only the index;
        /// these fields are accepted for forward-compatible captures and server-side validation tests.
        /// </summary>
        public uint ClaimContentId { get; private set; }

        /// <summary>
        /// Reward-type lane (1=item, 2=essence, 3=modifier) for an optional trailing claim payload.
        /// </summary>
        public byte ClaimRewardType { get; private set; }

        public bool HasClaimRequest => ClaimContentId != 0u && ClaimRewardType != 0;

        public void Read(GamePacketReader reader)
        {
            RewardRotationIndex = reader.ReadUInt();
            ClaimContentId = 0u;
            ClaimRewardType = 0;

            if (reader.BytesRemaining < 5u)
                return;

            ClaimContentId = reader.ReadUInt();
            if (reader.BytesRemaining >= 1u)
                ClaimRewardType = reader.ReadByte();
        }
    }
}
