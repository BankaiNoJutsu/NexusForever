using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingNeighborInvite)]
    public class ClientHousingNeighborInvite : IReadable
    {
        public TargetResidence TargetResidence { get; } = new();
        public string TargetName { get; private set; }

        public void Read(GamePacketReader reader)
        {
            TargetResidence.Read(reader);
            TargetName = reader.ReadWideString();
        }
    }
}
