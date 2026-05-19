using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class StructuredPlugEntity : WorldEntity, IStructuredPlugEntity
    {
        public override EntityType Type => EntityType.StructuredPlug;

        #region Dependency Injection

        public StructuredPlugEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        protected override IEntityModel BuildEntityModel()
        {
            return new StructuredPlugEntityModel
            {
                CreatureId = CreatureId,
                SocketId   = WorldSocketId
            };
        }
    }
}
