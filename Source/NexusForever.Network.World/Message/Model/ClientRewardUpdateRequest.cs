using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRewardUpdateRequest)]
    public class ClientRewardUpdateRequest : IReadable
    {
        public uint RewardRotationIndex { get; private set; }

        public void Read(GamePacketReader reader)
        {
            RewardRotationIndex = reader.ReadUInt();
        }
    }
}
