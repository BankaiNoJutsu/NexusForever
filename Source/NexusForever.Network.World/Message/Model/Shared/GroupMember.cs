using NexusForever.Network.Message;
using NexusForever.Game.Static.Group;

namespace NexusForever.Network.World.Message.Model.Shared
{
    /// <summary>
    /// Shared group-member wrapper from <c>GroupMember_ReadPayload</c> (<c>1400828a0</c>).
    /// Wire order is identity, 32-bit flags, <see cref="GroupCharacter"/>, then group index.
    /// </summary>
    public class GroupMember : IWritable
    {
        public Identity MemberIdentity { get; set; } = new();
        public GroupMemberInfoFlags Flags { get; set; }
        public GroupCharacter Member { get; set; }
        public uint GroupIndex { get; set; }

        public void Write(GamePacketWriter writer)
        {
            MemberIdentity.Write(writer);
            writer.Write(Flags, 32);
            Member.Write(writer);
            writer.Write(GroupIndex);
        }
    }
}
