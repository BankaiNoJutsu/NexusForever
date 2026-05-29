using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05D3. Native client registration binds this request to shared
    /// <c>ClientCraftingAbandon_WritePayload</c> (<c>140001ba0</c>), so the current model remains
    /// an empty trigger packet.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingMatchInitiateVoteToSurrender)]
    public class ClientMatchingMatchInitiateVoteToSurrender : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // Zero size message
        }
    }
}
