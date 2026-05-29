using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode <c>0x041F</c>; mapped from <c>ServerGroupInviteReceived_ReadPayload</c> (<c>140084390</c>).
    /// Native layout is group id, leader index, inviter index, then counted <see cref="Shared.GroupCharacter"/> rows.
    /// </summary>
    [Message(GameMessageOpcode.ServerGroupInviteReceived)]
    public class ServerGroupInviteReceived : IWritable
    {
        public ulong GroupId { get; set; }
        public uint LeaderIndex { get; set; }
        public uint InviterIndex { get; set; }

        public List<GroupCharacter> Members = new List<GroupCharacter>();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(GroupId);
            writer.Write(LeaderIndex);
            writer.Write(InviterIndex);

            writer.Write(Members.Count);
            Members.ForEach(x => x.Write(writer));
        }
    }
}
