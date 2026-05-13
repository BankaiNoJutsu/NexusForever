using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Configuration.Model
{
    [ConfigurationBind]
    public class WorldConfig
    {
        public uint? CreatureRespawnSeconds { get; set; } = 30u;
    }
}
