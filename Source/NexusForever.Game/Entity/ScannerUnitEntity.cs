using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Shared.Game;
using NLog;

namespace NexusForever.Game.Entity
{
    public class ScannerUnitEntity : WorldEntity, IScannerUnitEntity
    {
        private const float FollowDistance = 3f;
        private const float FollowMinRecalculateDistance = 5f;

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public override EntityType Type => EntityType.ScannerUnit;

        private readonly UpdateTimer followTimer = new(1d);

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

        public override void OnEnqueueRemoveFromMap()
        {
            followTimer.Reset(false);
            base.OnEnqueueRemoveFromMap();
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);
            FollowOwner(lastTick);
        }

        private void FollowOwner(double lastTick)
        {
            if (SummonerGuid == null)
                return;

            followTimer.Update(lastTick);
            if (!followTimer.HasElapsed)
                return;

            IPlayer owner = GetVisible<IPlayer>(SummonerGuid.Value);
            if (owner == null)
            {
                log.Warn($"ScannerUnit {Guid} has lost its owner {SummonerGuid.Value}.");
                RemoveFromMap();
                return;
            }

            if (owner.Position.GetDistance(Position) < FollowMinRecalculateDistance)
                return;

            MovementManager.Follow(owner, FollowDistance);
            followTimer.Reset();
        }
    }
}
