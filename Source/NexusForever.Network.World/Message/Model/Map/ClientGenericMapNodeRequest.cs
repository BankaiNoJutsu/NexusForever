using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Map
{
    // Compatibility model only; 0x07CF belongs to RewardTrackLib.ChooseRewardTrack.
    public class ClientGenericMapNodeRequest : IReadable
    {
        public ushort GenericMapNodeId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            GenericMapNodeId = reader.ReadUShort(14u);
        }
    }
}
