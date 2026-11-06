using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model.Entity;
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

        public string Name
        {
            get => name;
            set
            {
                string updatedName = value ?? string.Empty;
                if (name == updatedName)
                    return;

                name = updatedName;
                if (InWorld && Guid != 0u)
                    EnqueueToVisible(new ServerUnitNameChange { UnitId = Guid, Name = name }, true);
            }
        }

        private string name = string.Empty;

        private readonly UpdateTimer followTimer = new(1d);
        private PathManager pathManager;
        private bool pathRemovalQueued;

        internal void BindPathManager(PathManager owner)
        {
            pathManager = owner;
            pathRemovalQueued = false;
        }

        public override void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
            if (pathManager != null && !pathManager.OwnsScientistScanbot(this, map))
            {
                InitialiseMapRegistration(map, guid, vector);
                RemoveFromMap();
                return;
            }
            base.OnAddToMap(map, guid, vector);
            if (!pathRemovalQueued && pathManager?.OnScientistScanbotSummoned(this) == false)
                RemoveFromMap();
        }

        public override void OnRemoveFromMap()
        {
            var original = pathManager;
            try { base.OnRemoveFromMap(); }
            finally { original?.OnScientistScanbotUnsummoned(this); }
        }


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
                Name       = Name
            };
        }

        public override void OnEnqueueRemoveFromMap()
        {
            pathRemovalQueued = true;
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
            if (pathManager != null && !pathManager.OwnsScientistScanbot(this))
            {
                if (!pathRemovalQueued) RemoveFromMap();
                return;
            }
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
