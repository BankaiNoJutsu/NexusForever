using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMatchingMatchExited)]
    public class ServerMatchingMatchExited : IWritable
    {
        /// <summary>
        /// Native server registration binds opcode 0x05C0 to shared
        /// ServerEmpty_ReadPayload (14007d8e0).
        /// </summary>
        public void Write(GamePacketWriter writer)
        {
        }
    }
}
