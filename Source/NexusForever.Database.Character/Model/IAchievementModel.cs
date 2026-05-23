using System;

namespace NexusForever.Database.Character.Model
{
    public interface IAchievementModel
    {
       ulong Id { get; set; }
       ushort AchievementId { get; set; }
       uint ProgressState { get; set; }
       uint CreditedChecklistMask { get; set; }
       DateTime? DateCompleted { get; set; }
    }
}
