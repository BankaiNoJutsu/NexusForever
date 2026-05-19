using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    internal class PickupEntity : WorldEntity, IPickupEntity
    {
        public override EntityType Type => EntityType.Pickup;

        public PickupEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new PickupEntityModel
            {
                CreatureId = CreatureId,
                OwnerId    = SummonerGuid ?? 0u
            };
        }
    }
}
