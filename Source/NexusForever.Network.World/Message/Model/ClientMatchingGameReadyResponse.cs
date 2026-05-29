using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05C8. Native client registration binds this response packet to shared
    /// <c>ClientBool_WritePayload</c> (<c>14007e610</c>), matching the single join-or-decline bit.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingGameReadyResponse)]
    public class ClientMatchingGameReadyResponse : IReadable
    {
        public bool Response { get; private set; } // decline = false, join = true

        public void Read(GamePacketReader reader)
        {
            Response = reader.ReadBit();
        }
    }
}
