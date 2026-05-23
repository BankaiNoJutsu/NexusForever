using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Achievement
{
    public class Achievement : IWritable
    {
        public ushort AchievementId { get; set; } 
        public uint ProgressState { get; set; }
        public uint CreditedChecklistMask { get; set; }
        public ulong DateCompleted { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(AchievementId, 15u);
            writer.Write(ProgressState);
            writer.Write(CreditedChecklistMask);
            writer.Write(DateCompleted);
        }
    }
}
