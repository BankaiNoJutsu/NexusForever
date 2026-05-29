using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMatchingMatchFinished)]
    public class ServerMatchingMatchFinished : IWritable
    {
        /// <summary>
        /// Native server registration binds opcode 0x05BE to shared
        /// ServerEmpty_ReadPayload (14007d8e0).
        /// </summary>
        public void Write(GamePacketWriter writer)
        {
        }
    }
}
