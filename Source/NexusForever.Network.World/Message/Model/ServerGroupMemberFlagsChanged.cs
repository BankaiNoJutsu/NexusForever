using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Game.Static.Group;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode <c>0x0437</c>; mapped from <c>ServerGroupMemberFlagsChanged_ReadPayload</c> (<c>1400838c0</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupMemberFlagsChanged)]
    public class ServerGroupMemberFlagsChanged : IWritable
    {
        public ulong GroupId { get; set; }
        public uint MemberIndex { get; set; } //< Not sure
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
        }
    }
}
