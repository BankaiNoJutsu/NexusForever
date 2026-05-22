using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Ready-check status broadcast (0x0441, 0x38 bytes).
    /// Client handler <c>Group_HandleReadyCheck_ReadPayload</c> @ 140603970 reads group id plus member key fields.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupReadyCheckStatusUpdate)]
    public class ServerGroupReadyCheckStatusUpdate : IWritable
    {
        public ulong GroupId { get; set; }

        public Identity MemberIdentity { get; set; } = new();

        public uint ReadyStatus { get; set; }

        public uint Unknown0 { get; set; }

        public uint Unknown1 { get; set; }

        public uint Unknown2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            MemberIdentity.Write(writer);
            writer.Write(ReadyStatus);
            writer.Write(Unknown0);
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.WriteBytes(new byte[22]);
        }
    }
}
