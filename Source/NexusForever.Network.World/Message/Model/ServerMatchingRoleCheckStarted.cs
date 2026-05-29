using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode
    /// <c>0x05F1</c> to the shared reader slot <c>LAB_1400807f0</c> (<c>1400807f0</c>), the same
    /// structural one-flag path reused by <c>0x05B0</c> and <c>0x05CC</c>.
    /// No dedicated producer/consumer evidence has yet proven whether the payload is more
    /// specific than the current one-flag surface, so keep this model structural.
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
