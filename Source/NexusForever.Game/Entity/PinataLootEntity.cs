using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class PinataLootEntity : WorldEntity, IPinataLootEntity
    {
        public override EntityType Type => EntityType.PinataLoot;

        #region Dependency Injection

        public PinataLootEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        protected override IEntityModel BuildEntityModel()
        {
            return new PinataLootEntityModel
            {
                CreatureId = CreatureId
            };
        }
    }
}
