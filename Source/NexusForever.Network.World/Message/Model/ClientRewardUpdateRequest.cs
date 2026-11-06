using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Build16042: sender140636ba0 ->07CC; registration140078439 selects a
    // four-byte payload, size14007d000 and uint32 writer14007d010.
    [Message(GameMessageOpcode.ClientRewardRotationsRequestUpdate)]
    public class ClientRewardUpdateRequest : IReadable
    {
        public uint RewardRotationIndex { get; private set; }

        public void Read(GamePacketReader reader)
        {
            RewardRotationIndex = reader.ReadUInt();
        }
    }
}
