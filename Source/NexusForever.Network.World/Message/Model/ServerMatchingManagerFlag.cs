using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05B0</c>
    /// to the shared one-flag reader slot <c>LAB_1400807f0</c> (<c>1400807f0</c>) inside a
    /// registered 4-byte object.
    /// No current server sender/consumer or direct client opcode-immediate reference has been
    /// recovered, so the flag meaning remains diagnostic-only.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingManagerFlag)]
    public class ServerMatchingManagerFlag : IWritable
    {
        public bool Flag { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Flag);
        }
    }
}