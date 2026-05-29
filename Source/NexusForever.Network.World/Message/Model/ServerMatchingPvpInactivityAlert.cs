using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05E6</c>
    /// to shared <c>ServerUInt32_ReadPayload</c> (<c>14007ab50</c>).
    /// The current single-field model matches that shared 32-bit countdown surface.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingPvpInactivityAlert)]
    public class ServerMatchingPvpInactivityAlert : IWritable
    {
        public uint RemainingTimeMs { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(RemainingTimeMs);
        }
    }
}
