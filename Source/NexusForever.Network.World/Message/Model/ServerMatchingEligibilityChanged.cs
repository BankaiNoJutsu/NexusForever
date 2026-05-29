using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMatchingEligibilityChanged)]
    public class ServerMatchingEligibilityChanged : IWritable
    {
        /// <summary>
        /// Native server registration binds opcode 0x05B8 to shared
        /// ServerUInt32_ReadPayload (14007ab50).
        /// </summary>
        public uint MatchingEligibilityFlags { get; set; } // Checked against tbl matchingMapPrequisite->matchingEligibilityFlagEnum

        public void Write(GamePacketWriter writer)
        {
            writer.Write(MatchingEligibilityFlags);
        }
    }
}
