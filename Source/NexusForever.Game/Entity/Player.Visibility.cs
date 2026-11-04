using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private const ushort SettingUpCampQuestId = 3671;
        private const ushort EmpoweredTowerQuestId = 3486;
        private const uint NorthernWildsWorldId = 426u;
        private const uint NorthernWildsLandingSiteDeadeyeCreatureId = 11063u;
        private const uint NorthernWildsAvalancheVisualCreatureId = 15615u;
        private const uint NorthernWildsAvalancheCasterCreatureId = 67662u;

        private QuestVisibilityLevel questVisibilityLevel;
        private QuestVisibilityVitals questVisibilityVitals;

        private sealed class QuestVisibilityVitals
        {
            internal Dictionary<Stat, StatValueInitial> Previous { get; } = [];
            internal bool HealthPacketPending { get; set; }
        }

        internal bool IsRetainingQuestVital(Stat stat) => questVisibilityVitals?.Previous.ContainsKey(stat) == true;

        internal void RetainQuestVital(Stat stat, IStatValue previous)
        {
            if (IsRetainingQuestVital(stat) || QuestManager is not QuestManager { IsOrdinaryCompletionMutationInProgress: true } quests)
                return;

            if (questVisibilityVitals == null)
            {
                questVisibilityVitals = new QuestVisibilityVitals();
                quests.RetainOrdinaryCompletionPublication(PublishQuestVitals);
            }

            questVisibilityVitals.Previous.Add(stat, previous == null ? null : new StatValueInitial
            {
                Stat = previous.Stat, Type = previous.Type, Value = previous.Value, Data = previous.Data
            });
        }

        internal bool RetainQuestHealthPacket(Stat cause)
        {
            if (!IsRetainingQuestVital(Stat.Health) && !IsRetainingQuestVital(cause))
                return false;

            questVisibilityVitals.HealthPacketPending = true;
            return true;
        }

        private void PublishQuestVitals()
        {
            QuestVisibilityVitals snapshot = questVisibilityVitals;
            questVisibilityVitals = null;
            foreach (Stat stat in snapshot.Previous.Keys)
            {
                IStatValue current = stats[stat];
                if (stat == Stat.Shield)
                    EnqueueIntegerStatUpdate(current);
                PublishStatUpdate(stat, current.Value);
            }
            if (snapshot.HealthPacketPending)
                EnqueueEntityHealthUpdate(Stat.Health);
        }

        private sealed class QuestVisibilityLevel(StatValueInitial previous)
        {
            internal StatValueInitial Previous { get; } = previous;
            internal uint Level { get; set; }
            internal Dictionary<IPlayer, Action<ServerEntityStatUpdateInteger>> Observers { get; } = new(ReferenceEqualityComparer.Instance);
        }

        private void RetainQuestLevelForVisibility(uint level)
        {
            if (QuestManager is not QuestManager { IsOrdinaryCompletionMutationInProgress: true } quests)
                return;

            if (questVisibilityLevel == null)
            {
                stats.TryGetValue(Stat.Level, out IStatValue previous);
                var snapshot = new QuestVisibilityLevel(new StatValueInitial
                {
                    Stat = Stat.Level, Type = StatType.Integer, Value = Level, Data = previous?.Data ?? 0u
                });
                questVisibilityLevel = snapshot;
                quests.RetainOrdinaryCompletionPublication(() =>
                {
                    questVisibilityLevel = null;
                    if (Level != snapshot.Level)
                        return;

                    var update = new ServerEntityStatUpdateInteger
                    {
                        UnitId = Guid,
                        Stat = new StatValueUpdate { Stat = Stat.Level, Type = StatType.Integer, Value = Level }
                    };
                    foreach (Action<ServerEntityStatUpdateInteger> publish in snapshot.Observers.Values)
                    {
                        try { publish(update); }
                        catch (Exception exception) { log.Error(exception, "Failed to publish a committed quest level to an observer."); }
                    }
                });
            }

            questVisibilityLevel.Level = level;
        }

        internal void RetainQuestLevelObserver(IPlayer recipient)
        {
            if (questVisibilityLevel == null)
                return;

            var map = Map;
            uint sourceGuid = Guid;
            uint recipientGuid = recipient.Guid;
            var session = recipient.Session;
            var account = recipient.Account;
            ulong? epoch = account?.WorldSessionEpoch;
            questVisibilityLevel.Observers[recipient] = update =>
            {
                if (map == null || !ReferenceEquals(Map, map) || Guid != sourceGuid
                    || !visibleEntities.TryGetValue(recipientGuid, out IGridEntity visible) || !ReferenceEquals(visible, recipient)
                    || !ReferenceEquals(recipient.Map, map) || recipient.Guid != recipientGuid || recipient.IsLoading
                    || !ReferenceEquals(recipient.Session, session) || !ReferenceEquals(recipient.Account, account)
                    || account?.WorldSessionEpoch != epoch || session is NetworkSession { IsDisconnecting: true }
                    || session is not IPlayerSession bound || !ReferenceEquals(bound.Player, recipient))
                    return;

                session.EnqueueMessageEncrypted(update);
            };
        }

        private void ApplyQuestVisibilitySnapshot(ServerEntityCreate packet, Player observer)
        {
            if (questVisibilityLevel is { } snapshot && Level == snapshot.Level)
            {
                int index = packet.Stats.FindIndex(stat => stat.Stat == Stat.Level);
                if (index >= 0)
                {
                    packet.Stats[index] = snapshot.Previous;
                    RetainQuestLevelObserver(observer);
                }
            }

            if (questVisibilityVitals is { } vitals)
            {
                for (int i = packet.Stats.Count - 1; i >= 0; i--)
                {
                    if (!vitals.Previous.TryGetValue(packet.Stats[i].Stat, out StatValueInitial previous))
                        continue;
                    if (previous == null)
                        packet.Stats.RemoveAt(i);
                    else
                        packet.Stats[i] = previous;
                }
            }

            if (!HasRetainedQuestProperties)
                return;

            for (int i = packet.Properties.Count - 1; i >= 0; i--)
            {
                if (!retainedQuestProperties.TryGetValue(packet.Properties[i].Property, out var previous))
                    continue;
                if (previous == null)
                    packet.Properties.RemoveAt(i);
                else
                    packet.Properties[i] = previous;
            }
        }

        public override bool CanSeeEntity(IGridEntity entity)
        {
            if (ShouldForceStarterTutorialEntityVisibility(entity))
                return true;

            return base.CanSeeEntity(entity)
                && !ShouldHideStarterTutorialEntity(entity)
                && !ShouldHideNorthernWildsAvalanche(entity);
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
            if (worldEntity is Player source && !ReferenceEquals(source, this))
                source.ApplyQuestVisibilitySnapshot(createPacket, this);

            IPlayer playerEntity = worldEntity as IPlayer;
            if (playerEntity != null)
                AddPlayerPositionSnapshot(createPacket, playerEntity);

            Session.EnqueueMessageEncrypted(createPacket);
            GetGlobalLootManager()?.SendLootNotifyForVisibleOwner(this, worldEntity);

            if (playerEntity != null)
            {
                SendVisiblePlayerMetadata(playerEntity);
                SendVisibleVehicleAttachment(playerEntity);
            }
            else if (worldEntity is IVehicleEntity)
            {
                // The vehicle may enter visibility after its passenger.
                foreach (IPlayer passenger in visibleEntities.Values.OfType<IPlayer>())
                    if (passenger.PlatformGuid == worldEntity.Guid)
                        SendVisibleVehicleAttachment(passenger);
            }
        }

        private void SendVisibleVehicleAttachment(IPlayer playerEntity)
        {
            if (playerEntity.PlatformGuid is not uint platformGuid
                || !visibleEntities.TryGetValue(platformGuid, out IGridEntity platform)
                || platform is not IVehicleEntity vehicle)
                return;

            IVehiclePassenger passenger = vehicle.GetPassenger(playerEntity.Guid);
            if (passenger == null || passenger.Guid != playerEntity.Guid)
                return;

            // Both entities have been created for this observer. Restore their seat link.
            Session.EnqueueMessageEncrypted(new ServerVehiclePassengerSelf
            {
                Self = passenger.Guid,
                Vehicle = vehicle.Guid,
                SeatType = passenger.SeatType,
                SeatPosition = passenger.SeatPosition
            });
        }

        internal void RefreshQuestPresentation(ushort questId)
        {
            if (questId == EmpoweredTowerQuestId)
            {
                RemoveStoppedNorthernWildsAvalanches();
                return;
            }

            if (questId != SettingUpCampQuestId)
                return;

            foreach (IWorldEntity worldEntity in GetVisibleCreature<IWorldEntity>(NorthernWildsLandingSiteDeadeyeCreatureId).ToList())
                SendVisibleEntityCreate(worldEntity, refreshExistingEntity: true);
        }

        private bool ShouldHideNorthernWildsAvalanche(IGridEntity entity)
        {
            if (entity is not IWorldEntity worldEntity)
                return false;

            if (Map?.Entry?.Id != NorthernWildsWorldId)
                return false;

            if (worldEntity.CreatureId is not (NorthernWildsAvalancheVisualCreatureId or NorthernWildsAvalancheCasterCreatureId))
                return false;

            return HasStoppedNorthernWildsAvalanches();
        }

        private bool HasStoppedNorthernWildsAvalanches()
        {
            return QuestManager?.GetQuestState(EmpoweredTowerQuestId) is QuestState.Achieved or QuestState.Completed;
        }

        private void RemoveStoppedNorthernWildsAvalanches()
        {
            if (Map?.Entry?.Id != NorthernWildsWorldId || !HasStoppedNorthernWildsAvalanches())
                return;

            foreach (IWorldEntity worldEntity in GetVisibleCreature<IWorldEntity>(NorthernWildsAvalancheVisualCreatureId)
                .Concat(GetVisibleCreature<IWorldEntity>(NorthernWildsAvalancheCasterCreatureId))
                .ToList())
            {
                RemoveVisible(worldEntity);
            }
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
