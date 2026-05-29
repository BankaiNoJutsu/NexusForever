using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Provisional source model only.
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> currently binds
    /// opcode <c>0x0600</c> to <c>ServerHousingCommunityPlotReservation_ReadPayload</c>
    /// (<c>140086e70</c>), the same identity + uint32 reader already used by
    /// opcode <c>0x051F</c>. The current matching wrapper is still a safe structural fit because
    /// it writes the same identity + uint32 wire shape, but the matching-specific semantics remain
    /// blocked until a dedicated 0x0600 producer or consumer path explains the reused reader.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingGroupMemberRoleSelection)]
    public class ServerMatchingGroupMemberRoleSelection : IWritable
    {
        public Identity Identity { get; set; } = new();
        public Role Role { get; set; }

        public void Write(GamePacketWriter writer)
        {
            Identity.Write(writer);
            writer.Write(Role, 32u);
        }
    }
}
