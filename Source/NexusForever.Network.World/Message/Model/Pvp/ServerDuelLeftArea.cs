using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pvp
{
    [Message(GameMessageOpcode.ServerDuelLeftArea)]
    [PacketSerializable(PacketSerializationMode.Write)]
    public partial class ServerDuelLeftArea : IWritable
    {
    }
}
