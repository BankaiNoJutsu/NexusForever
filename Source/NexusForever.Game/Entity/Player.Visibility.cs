using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private const ushort SettingUpCampQuestId = 3671;
        private const uint NorthernWildsWorldId = 426u;
        private const uint NorthernWildsLandingSiteDeadeyeCreatureId = 11063u;
        private const uint DeadeyeNeutralPresentationCreatureId = 16962u;

        public override bool CanSeeEntity(IGridEntity entity)
        {
            if (ShouldForceStarterTutorialEntityVisibility(entity))
                return true;

            return base.CanSeeEntity(entity) && !ShouldHideStarterTutorialEntity(entity);
        }

        public override void AddVisible(IGridEntity entity)
        {
            AddVisible(entity, synchroniseReciprocalPlayer: true);
        }

        private void AddVisible(IGridEntity entity, bool synchroniseReciprocalPlayer)
        {
            bool wasVisible = visibleEntities.ContainsKey(entity.Guid);
            base.AddVisible(entity);

            if (wasVisible || !visibleEntities.ContainsKey(entity.Guid))
                return;

            if (entity is IWorldEntity worldEntity)
                SendVisibleEntityCreate(worldEntity, refreshExistingEntity: false);

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

            if (synchroniseReciprocalPlayer)
                SynchroniseReciprocalPlayerVisibility(entity);
        }

        internal void RefreshVisiblePlayersForNearbyList()
        {
            foreach (IPlayer playerEntity in visibleEntities.Values.OfType<IPlayer>().ToList())
            {
                if (ReferenceEquals(playerEntity, this))
                    continue;

                SendVisibleEntityCreate(playerEntity, refreshExistingEntity: true);
            }
        }

        private void SynchroniseReciprocalPlayerVisibility(IGridEntity entity)
        {
            if (entity is not Player playerEntity || ReferenceEquals(playerEntity, this))
                return;

            if (Map == null || playerEntity.Map != Map)
                return;

            if (playerEntity.GetVisible<IGridEntity>(Guid) == null)
            {
                playerEntity.AddVisible(this, synchroniseReciprocalPlayer: false);
                return;
            }

            playerEntity.RefreshVisiblePlayerForNearbyList(this);
        }

        private void RefreshVisiblePlayerForNearbyList(IPlayer playerEntity)
        {
            SendVisibleEntityCreate(playerEntity, refreshExistingEntity: true);
        }

        private void SendVisibleEntityCreate(IWorldEntity worldEntity, bool refreshExistingEntity)
        {
            if (refreshExistingEntity)
            {
                Session.EnqueueMessageEncrypted(new ServerEntityDestroy
                {
                    Guid = worldEntity.Guid,
                    Flag = true
                });
            }

            foreach (IWritable auxiliary in worldEntity.BuildEntityCreateAuxPackets())
                Session.EnqueueMessageEncrypted(auxiliary);

            ServerEntityCreate createPacket = worldEntity.BuildCreatePacket(IsLoading);
            ApplyQuestPresentationOverrides(worldEntity, createPacket);

            IPlayer playerEntity = worldEntity as IPlayer;
            if (playerEntity != null)
                AddPlayerPositionSnapshot(createPacket, playerEntity);

            Session.EnqueueMessageEncrypted(createPacket);
            GetGlobalLootManager()?.SendLootNotifyForVisibleOwner(this, worldEntity);

            if (playerEntity != null)
                SendVisiblePlayerMetadata(playerEntity);
        }

        internal void RefreshQuestPresentation(ushort questId)
        {
            if (questId != SettingUpCampQuestId)
                return;

            foreach (IWorldEntity worldEntity in GetVisibleCreature<IWorldEntity>(NorthernWildsLandingSiteDeadeyeCreatureId).ToList())
                SendVisibleEntityCreate(worldEntity, refreshExistingEntity: true);
        }

        private void ApplyQuestPresentationOverrides(IWorldEntity worldEntity, ServerEntityCreate createPacket)
        {
            if (!ShouldSuppressSettingUpCampReceiverPresentation(worldEntity))
                return;

            if (createPacket.EntityModel is NonPlayerEntityModel nonPlayerEntityModel)
                nonPlayerEntityModel.CreatureId = DeadeyeNeutralPresentationCreatureId;
        }

        private bool ShouldSuppressSettingUpCampReceiverPresentation(IWorldEntity worldEntity)
        {
            if (worldEntity.CreatureId != NorthernWildsLandingSiteDeadeyeCreatureId)
                return false;

            if (Map?.Entry?.Id != NorthernWildsWorldId)
                return false;

            return QuestManager?.GetQuestState(SettingUpCampQuestId) == QuestState.Achieved;
        }

        private static void AddPlayerPositionSnapshot(ServerEntityCreate createPacket, IPlayer playerEntity)
        {
            var positionCommand = new NetworkEntityCommand
            {
                Command = EntityCommand.SetPosition,
                Model   = new SetPositionCommand
                {
                    Position = playerEntity.Position,
                    Blend    = false
                }
            };

            int index = createPacket.Commands.FindIndex(c => c.Command == EntityCommand.SetPosition || c.Model is SetPositionCommand);
            if (index >= 0)
                createPacket.Commands[index] = positionCommand;
            else
                createPacket.Commands.Insert(0, positionCommand);
        }

        private void SendVisiblePlayerMetadata(IPlayer playerEntity)
        {
            Session.EnqueueMessageEncrypted(new ServerSetUnitPathType
            {
                UnitId = playerEntity.Guid,
                Path   = playerEntity.Path
            });

            Session.EnqueueMessageEncrypted(new ServerEntityGroupAssociation
            {
                UnitId  = playerEntity.Guid,
                GroupId = playerEntity.ClientGroupAssociation
            });
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

        private IGlobalLootManager GetGlobalLootManager()
        {
            return globalLootManager;
        }

        protected override void RemoveVisible(uint gridX, uint gridZ)
        {
            base.RemoveVisible(gridX, gridZ);
            Map.GridRemoveVisiblePlayer(gridX, gridZ);
        }
    }
}
