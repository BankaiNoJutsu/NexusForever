using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMatchingMatchEntered)]
    public class ServerMatchingMatchEntered : IWritable
    {
        /// <summary>
        /// Native server registration binds opcode 0x05BF to shared
        /// ServerUInt32_ReadPayload (14007ab50).
        /// </summary>
        public uint MatchingGameMapId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(MatchingGameMapId);
        }
    }
}
