using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode <c>0x0427</c>; mapped from <c>ServerGroupJoin_ReadPayload</c> (<c>1400844f0</c>)
    /// with nested <c>Group_ReadPayload</c> (<c>140082950</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupJoin)]
    public class ServerGroupJoin : IWritable
    {
        public Identity TargetPlayer { get; set; } = new();
        public Group Group { get; set; } = new Group();

        public void Write(GamePacketWriter writer)
        {
            TargetPlayer.Write(writer);
            Group.Write(writer);
        }
    }
}
