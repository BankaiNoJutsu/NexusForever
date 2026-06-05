using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode
    /// <c>0x05F1</c> to the shared reader slot <c>LAB_1400807f0</c> (<c>1400807f0</c>), the same
    /// structural one-flag path reused by <c>0x05B0</c> and <c>0x05CC</c>.
    /// <c>MatchingManager_ApplyMatchingRoleCheckStarted</c> (<c>1405c0e90</c>) consumes a
    /// payload word for the client event, but its only current xref is PE <c>.pdata</c> and
    /// no dispatch slot has been recovered, so keep this model structural.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingRoleCheckStarted)]
    public class ServerMatchingRoleCheckStarted : IWritable
    {
        public bool RolesRequired { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(RolesRequired);
        }
    }
}
