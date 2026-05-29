using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05B4. Native client registration binds this request to shared
    /// <c>ClientCraftingAbandon_WritePayload</c> (<c>140001ba0</c>), so the current model remains
    /// an empty queue-leave-all trigger.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingQueueLeaveAll)]
    public class ClientMatchingQueueLeaveAll : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // deliberately empty
        }
    }
}
