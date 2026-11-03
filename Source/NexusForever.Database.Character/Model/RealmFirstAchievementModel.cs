namespace NexusForever.Database.Character.Model
{
    // A null owner/token closes an imported completion without inventing its
    // winner. There is deliberately no owner FK: deleting an owner cannot
    // reopen an already completed realm-first achievement.
    public class RealmFirstAchievementModel
    {
        public bool IsGuild { get; set; }
        public ushort AchievementId { get; set; }
        public ulong? OwnerId { get; set; }
        public string Token { get; set; }
        public string Name { get; set; }
    }
}
