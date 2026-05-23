using System;

namespace NexusForever.Database.Character.Model
{
    public class GuildAchievementModel : IAchievementModel
    {
        public ulong Id { get; set; }
        public ushort AchievementId { get; set; }
        public uint ProgressState { get; set; }
        public uint CreditedChecklistMask { get; set; }
        public DateTime? DateCompleted { get; set; }

        public GuildModel Guild { get; set; }
    }
}
