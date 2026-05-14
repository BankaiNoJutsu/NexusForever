using NexusForever.Game.Static.Entity;

namespace NexusForever.Database.World.Model
{
    public class CreatureInfoStatModel
    {
        public uint CreatureId { get; set; }
        public Stat Stat { get; set; }
        public float Value { get; set; }
    }
}
