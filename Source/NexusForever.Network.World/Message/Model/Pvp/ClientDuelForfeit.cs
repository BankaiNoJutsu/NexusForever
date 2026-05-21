using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pvp
{
    // Can only be sent after dueling has begun with ServerDuelStart (0x895)
    [Message(GameMessageOpcode.ClientDuelForfeit)]
    [PacketSerializable(PacketSerializationMode.Read)]
    public partial class ClientDuelForfeit : IReadable
    {
    }
}
