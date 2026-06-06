using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        public override bool CanSeeEntity(IGridEntity entity)
        {
            if (ShouldForceStarterTutorialEntityVisibility(entity))
                return true;

            return base.CanSeeEntity(entity) && !ShouldHideStarterTutorialEntity(entity);
        }

        public override void AddVisible(IGridEntity entity)
        {
            bool wasVisible = visibleEntities.ContainsKey(entity.Guid);
            base.AddVisible(entity);

            if (wasVisible || !visibleEntities.ContainsKey(entity.Guid))
                return;

            if (entity is IWorldEntity worldEntity)
            {
                foreach (IWritable auxiliary in worldEntity.BuildEntityCreateAuxPackets())
                    Session.EnqueueMessageEncrypted(auxiliary);

                Session.EnqueueMessageEncrypted(worldEntity.BuildCreatePacket(IsLoading));
                GetGlobalLootManager()?.SendLootNotifyForVisibleOwner(this, worldEntity);
            }

            if (entity is IPlayer playerEntity)
                Session.EnqueueMessageEncrypted(new ServerSetUnitPathType
                {
                    UnitId = playerEntity.Guid,
                    Path   = playerEntity.Path
                });

            if (entity == this)
            {
                Session.EnqueueMessageEncrypted(new ServerPlayerChanged
                {
                    Guid     = entity.Guid,
                    Unknown1 = 1
                });
            }

            if (entity is IUnitEntity unitEntity && unitEntity.InCombat)
            {
                Session.EnqueueMessageEncrypted(new ServerUnitEnteredCombat
                {
                    UnitId   = unitEntity.Guid,
                    InCombat = unitEntity.InCombat
                });
            }

            if (entity is IWorldEntity busyEntity && busyEntity.IsBusy)
            {
                Session.EnqueueMessageEncrypted(new ServerUnitInUse
                {
                    UnitId = busyEntity.Guid,
                    InUse  = true
                });
            }
        }

        public override void RemoveVisible(IGridEntity entity)
        {
            if (ShouldForceStarterTutorialEntityVisibility(entity))
                return;

            bool wasVisible = visibleEntities.ContainsKey(entity.Guid);
            base.RemoveVisible(entity);

            if (!wasVisible || visibleEntities.ContainsKey(entity.Guid))
                return;


            if (selectedVendorGuid == entity.Guid)
                SelectedVendorInfo = null;

            if (entity is IWorldEntity && entity != this)
            {
                Session.EnqueueMessageEncrypted(new ServerEntityDestroy
                {
                    Guid = entity.Guid,
                    Flag = true
                });
            }
        }

        protected override void AddVisible(uint gridX, uint gridZ)
        {
            base.AddVisible(gridX, gridZ);
            Map.GridAddVisiblePlayer(gridX, gridZ);
        }

        private GlobalLootManager GetGlobalLootManager()
        {
            return globalLootManager
                ?? LegacyServiceProvider.Provider?.GetService<GlobalLootManager>();
        }

        protected override void RemoveVisible(uint gridX, uint gridZ)
        {
            base.RemoveVisible(gridX, gridZ);
            Map.GridRemoveVisiblePlayer(gridX, gridZ);
        }
    }
}
