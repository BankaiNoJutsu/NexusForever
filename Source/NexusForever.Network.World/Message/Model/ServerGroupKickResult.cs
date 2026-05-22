using NexusForever.Game.Static.Group;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Kick action result to the initiating member (0x042A, 0x0C bytes).
    /// Correlated to <see cref="ClientGroupKick"/> (0x0428).
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupKickResult)]
    public class ServerGroupKickResult : IWritable
    {
        public ulong GroupId { get; set; }

        public GroupActionResult Result { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            writer.Write(Result, 32u);
        }
    }
}
