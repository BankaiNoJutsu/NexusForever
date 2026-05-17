using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingVisitResidence)]
    public class ClientHousingVisitResidence : IReadable
    {
        public TargetResidence TargetResidence { get; } = new();

        public void Read(GamePacketReader reader)
        {
            TargetResidence.Read(reader);
        }
    }
}