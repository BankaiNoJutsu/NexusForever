using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ServerPathUpdateXP)]
    [PacketSerializable(PacketSerializationMode.Write)]
    public partial class ServerPathUpdateXP : IWritable
    {
        [PacketField]
        public uint TotalXP { get; set; }
    }
}
