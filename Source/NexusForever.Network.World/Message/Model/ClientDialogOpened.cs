using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientDialogOpened)]
    [PacketSerializable(PacketSerializationMode.Read)]
    public partial class ClientDialogOpened : IReadable
    {
    }
}
