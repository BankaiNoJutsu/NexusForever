using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMatchingMatchReadyCancel)]
    public class ServerMatchingMatchReadyCancel : IWritable
    {
        /// <summary>
        /// Native server registration binds opcode 0x05BC to shared
        /// ServerEmpty_ReadPayload (14007d8e0).
        /// Sent when someone declines an invitation and the group is put back in the queue.
        /// </summary>
        public void Write(GamePacketWriter writer)
        {
            // deliberately empty
        }
    }
}
