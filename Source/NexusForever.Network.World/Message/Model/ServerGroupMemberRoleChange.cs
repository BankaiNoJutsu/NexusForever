using NexusForever.Game.Static.Group;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Provisional managed payload for opcode <c>0x0438</c>.
    /// Native reader <c>ServerGroupIdentityListAndUInt32Array_ReadPayload</c> (<c>140083990</c>)
    /// parses group id, one leading uint32, a counted identity array, and a parallel uint32 array,
    /// so this single-member wrapper remains blocked pending re-validation.
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
