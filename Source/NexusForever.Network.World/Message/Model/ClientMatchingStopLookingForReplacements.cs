using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x0602. Native client registration binds this request to shared
    /// <c>ClientCraftingAbandon_WritePayload</c> (<c>140001ba0</c>), so the current model remains
    /// an empty stop-looking-for-replacements trigger.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingStopLookingForReplacements)]
    public class ClientMatchingStopLookingForReplacements : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // Zero byte message
        }
    }
}
