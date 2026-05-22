using NexusForever.Game.Static.Group;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Group member role/flag change broadcast (0x0438, 0x28 bytes).
    /// Layout mirrors <see cref="ServerGroupMemberFlagsChanged"/> (0x0437).
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupMemberRoleChange)]
    public class ServerGroupMemberRoleChange : IWritable
    {
        public ulong GroupId { get; set; }

        public uint MemberIndex { get; set; }

        public Identity TargetedPlayer { get; set; } = new();

        public GroupMemberInfoFlags ChangedFlags { get; set; }

        public bool IsFromPromotion { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            writer.Write(MemberIndex);
            TargetedPlayer.Write(writer);
            writer.Write(ChangedFlags, 32);
            writer.Write(IsFromPromotion);
            writer.WriteBytes(new byte[14]);
        }
    }
}
