using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Configuration.Model
{
    [ConfigurationBind]
    public class WorldConfig
    {
        public uint? CreatureRespawnSeconds { get; set; } = 30u;
        public double? PlayerSaveIntervalSeconds { get; set; } = 60d;
        public double? GuildSaveIntervalSeconds { get; set; } = 60d;
        public double? ResidenceSaveIntervalSeconds { get; set; } = 60d;
        public float? MailExpiryDays { get; set; } = 30f;
        public byte? MaxCharacterLevel { get; set; } = 50;
        public float? SignatureXpRate { get; set; } = 0.25f;
        public double? GuildInviteExpirySeconds { get; set; } = 300d;
        public uint? WakeHereCreditCost { get; set; } = 0u;
    }
}
