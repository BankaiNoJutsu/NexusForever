using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05CC</c>
    /// to the shared one-flag reader slot <c>LAB_1400807f0</c> (<c>1400807f0</c>), the same
    /// structural path reused by <c>0x05B0</c> and <c>0x05F1</c>.
    /// Current runtime emits that flag as ally-versus-enemy participant direction, but the native
    /// reader only proves a one-bit surface.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchParticipantCountUpdate)]
    public class ServerMatchingMatchParticipantCountUpdate : IWritable
    {
        public bool Ally { get; set; } // true = ally, false = enemy

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Ally);
        }
    }
}
