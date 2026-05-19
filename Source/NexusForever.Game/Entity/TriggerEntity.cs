using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class TriggerEntity : WorldEntity, ITriggerEntity
    {
        public override EntityType Type => EntityType.Trigger;

        public TriggerEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new TriggerEntityModel
            {
                Name = string.Empty
            };
        }
    }
}
