using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Join-request dialog for the group leader (0x045A, 0x20 bytes).
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupRequestJoinWindow)]
    public class ServerGroupRequestJoinWindow : IWritable
    {
        public ulong GroupId { get; set; }

        public Identity RequesterIdentity { get; set; } = new();

        public uint TimeoutSeconds { get; set; }

        public uint Unknown0 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            RequesterIdentity.Write(writer);
            writer.Write(TimeoutSeconds);
            writer.Write(Unknown0);
            writer.WriteBytes(new byte[6]);
        }
    }
}
