using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x0606. Native client registration binds this request to shared
    /// <c>ClientCraftingAbandon_WritePayload</c> (<c>140001ba0</c>), the same zero-payload
    /// writer reused by other empty client requests.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingTransferIntoMatch)]
    public class ClientMatchingTransferIntoMatch : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // deliberately empty
        }
    }
}
