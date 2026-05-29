using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMatchingPenaltyUpdated)]
    public class ServerMatchingPenaltyUpdated : IWritable
    {
        private const int PenaltySlotCount = 16;

        // Native registration in 14006c290 binds 0x05D9 to LAB_140099920 with a fixed 0x40-byte payload.
        public uint[] MatchingPenaltyTimesMS { get; set; } = new uint[PenaltySlotCount];

        public void Write(GamePacketWriter writer)
        {
            var penaltyTimes = MatchingPenaltyTimesMS;

            for (int index = 0; index < PenaltySlotCount; index++)
            {
                uint penaltyTime = penaltyTimes != null && index < penaltyTimes.Length ? penaltyTimes[index] : 0u;
                writer.Write(penaltyTime);
            }
        }
    }
}
