using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    internal class LockboxEntity : WorldEntity, ILockboxEntity
    {
        public override EntityType Type => EntityType.Lockbox;

        #region Dependency Injection

        public LockboxEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        protected override IEntityModel BuildEntityModel()
        {
            return new LockboxEntityModel
            {
                CreatureId = CreatureId
            };
        }
    }
}
