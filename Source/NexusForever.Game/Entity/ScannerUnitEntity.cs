using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class ScannerUnitEntity : WorldEntity, IScannerUnitEntity
    {
        public override EntityType Type => EntityType.ScannerUnit;

        #region Dependency Injection

        public ScannerUnitEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        protected override IEntityModel BuildEntityModel()
        {
            return new ScannerUnitEntityModel
            {
                CreatureId = CreatureId,
                OwnerId    = SummonerGuid ?? 0u,
                Name       = string.Empty
            };
        }
    }
}
