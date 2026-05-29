using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05DA. Native client registration binds this request to shared
    /// <c>ClientCraftingAbandon_WritePayload</c> (<c>140001ba0</c>), so the current model remains
    /// an empty leave-match trigger.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingMatchLeave)]
    public class ClientMatchingMatchLeave : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // deliberately empty
        }
    }
}
