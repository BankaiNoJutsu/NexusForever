using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientEntityInteract)]
    [PacketSerializable(PacketSerializationMode.Read)]
    public partial class ClientEntityInteract : IReadable
    {
        [PacketField]
        public uint Guid { get; private set; }

        [PacketField(7u)]
        public byte Event { get; private set; }
    }
}
