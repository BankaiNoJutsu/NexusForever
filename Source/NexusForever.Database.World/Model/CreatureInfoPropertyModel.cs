using NexusForever.Game.Static.Entity;

namespace NexusForever.Database.World.Model
{
    public class CreatureInfoPropertyModel
    {
        public uint CreatureId { get; set; }
        public Property Property { get; set; }
        public float Value { get; set; }
    }
}
