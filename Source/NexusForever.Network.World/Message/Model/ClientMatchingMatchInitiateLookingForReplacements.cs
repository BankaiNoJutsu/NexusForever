using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05D5. Native client registration binds this request to shared
    /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), which serialises a
    /// single raw 32-bit field. The current model consumes that field as the replacement-role bitmask.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingMatchInitiateLookingForReplacements)]
    public class ClientMatchingMatchInitiateLookingForReplacements : IReadable
    {
        public Role Roles { get; private set; } // Roles being looked for

        public void Read(GamePacketReader reader)
        {
            Roles = reader.ReadEnum<Role>(32u);
        }
    }
}
