using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel
{
    [ScriptFilterOwnerId(907)]
    public class ColdbloodCitadelEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint HailstoneEntityId = 1100300001u;
        private const uint HailstoneCreatureId = 75508u;
        private const ushort HailstoneWorldId = 3522;
        private const ushort HailstoneAreaId = 0;
        private const uint HailstonePublicEventId = 907u;
        private const uint HailstonePublicEventPhase = 1u;
        private const uint HailstoneDisplayInfo = 24808u;
        private const ushort HailstoneFactionId = 691;
        private const string HailstoneScriptName = "HailstoneGatecrasherEntityScript";

        private const uint GatherRingEntityId = 1100300002u;
        private const uint GatherRingCreatureId = 75624u;
        private const ushort GatherRingWorldId = 3522;
        private const ushort GatherRingAreaId = 0;
        private const uint GatherRingPublicEventId = 907u;
        private const uint GatherRingPublicEventPhase = 0u;
        private const uint GatherRingDisplayInfo = 30327u;
        private const ushort GatherRingFactionId = 219;

        private const uint ColdbloodGateEntityId = 1100300003u;
        private const uint ColdbloodGateCreatureId = 75698u;
        private const ushort ColdbloodGateWorldId = 3522;
        private const ushort ColdbloodGateAreaId = 0;
        private const uint ColdbloodGatePublicEventId = 907u;
        private const uint ColdbloodGatePublicEventPhase = 2u;
        private const uint ColdbloodGateDisplayInfo = 36619u;
        private const ushort ColdbloodGateFactionId = 219;

        private static readonly Vector3 HailstonePosition = new(459.544f, -467.2018f, -596.769f);
        private static readonly Vector3 GatherRingPosition = new(604.33f, -475.452f, -322.957f);
        private static readonly Vector3 GatherRingRotation = Vector3.Zero;
        private static readonly Vector3 ColdbloodGatePosition = new(371f, -456.92f, -592.31f);
        private static readonly Vector3 ColdbloodGateRotation = new(1.570796327f, 0f, 0f);

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private uint gatherRingGuid;
        private uint gatherRingTriggerGuid;
        private bool gatherRingPlacementSpawned;
        private bool coldbloodGateSpawned;

        #region Dependency Injection

        private readonly IGlobalQuestManager globalQuestManager;

        public ColdbloodCitadelEventScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Coldblood Citadel requires a map instance.");

            gatherRingGuid = 0;
            gatherRingTriggerGuid = 0;
            gatherRingPlacementSpawned = false;
            coldbloodGateSpawned = false;

            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            switch (entity)
            {
                case IWorldLocationVolumeGridTriggerEntity worldLocationEntity:
                    OnAddToMapWorldLocationEntity(worldLocationEntity);
                    break;
                case IWorldEntity worldEntity:
                    OnAddToMapWorldEntity(worldEntity);
                    break;
            }
        }

        private void OnAddToMapWorldEntity(IWorldEntity worldEntity)
        {
            if (worldEntity.CreatureId == (uint)PublicEventCreature.GatherRing)
                gatherRingGuid = worldEntity.Guid;
        }

        private void OnAddToMapWorldLocationEntity(IWorldLocationVolumeGridTriggerEntity worldLocationEntity)
        {
            if (worldLocationEntity.Entry.Id == (uint)PublicEventCreature.GatherRing)
                gatherRingTriggerGuid = worldLocationEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            switch (entity)
            {
                case IWorldLocationVolumeGridTriggerEntity worldLocationEntity:
                    if (worldLocationEntity.Entry.Id == (uint)PublicEventCreature.GatherRing)
                        gatherRingTriggerGuid = 0;
                    break;
                case IWorldEntity worldEntity:
                    if (worldEntity.CreatureId == (uint)PublicEventCreature.GatherRing)
                        gatherRingGuid = 0;
                    break;
            }
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    OnPhaseEnter();
                    break;
                case PublicEventPhase.HailStoneGatecrasher:
                    OnPhaseHailStoneGatecrasher();
                    break;
                case PublicEventPhase.IceBloodCoven:
                    OnPhaseIceBloodCoven();
                    break;
                case PublicEventPhase.RisenHarizog:
                    OnPhaseRisenHarizog();
                    break;
            }
        }

        private void OnPhaseEnter()
        {
            if (gatherRingPlacementSpawned)
                return;

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(53206, 8656);
            AddToMap(triggerEntity, GatherRingPosition);

            SpawnGatherRing();
            gatherRingPlacementSpawned = true;
        }

        private void OnPhaseHailStoneGatecrasher()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHailStoneGatecrasher);
            SpawnHailstoneGatecrasher();

            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(gatherRingTriggerGuid);
            triggerEntity?.RemoveFromMap();

            IWorldEntity gatherRing = mapInstance.GetEntity<IWorldEntity>(gatherRingGuid);
            gatherRing?.RemoveFromMap();

            publicEvent.ActivateObjective(PublicEventObjective.SavePellFightingTheOsun);

            ActivateWipOptionalObjective(PublicEventObjective.StealSampleOfLiquidSoulfrost);
            ActivateWipOptionalObjective(PublicEventObjective.GatherSoulfrostShards);
            ActivateWipOptionalObjective(PublicEventObjective.RallyTheWinterfuryPell);

            BroadcastCommunicatorMessage(CommunicatorMessage.TowerEngineerRenhakul1);
        }

        private void SpawnHailstoneGatecrasher()
        {
            // Build 16042 reviewed instance entity 1100300001 places Hailstone
            // Gatecrasher in event 907 phase 1 with its objective-credit script.
            INonPlayerEntity hailstone = publicEvent.CreateEntity<INonPlayerEntity>();
            hailstone.Initialise(CreateHailstoneEntityModel());
            AddToMap(hailstone, HailstonePosition);
        }

        private static EntityModel CreateHailstoneEntityModel()
        {
            return new EntityModel
            {
                Id          = HailstoneEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = HailstoneCreatureId,
                World       = HailstoneWorldId,
                Area        = HailstoneAreaId,
                X           = HailstonePosition.X,
                Y           = HailstonePosition.Y,
                Z           = HailstonePosition.Z,
                DisplayInfo = HailstoneDisplayInfo,
                Faction1    = HailstoneFactionId,
                Faction2    = HailstoneFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = HailstonePublicEventId,
                    Phase   = HailstonePublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = HailstoneScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 50f
                    }
                }
            };
        }

        private void SpawnGatherRing()
        {
            // Build 16042 reviewed instance entity 1100300002 places the Gather
            // Ring in event 907 phase 0 alongside the branch's entry trigger.
            ISimpleEntity gatherRing = publicEvent.CreateEntity<ISimpleEntity>();
            gatherRing.Initialise(CreateGatherRingEntityModel());
            AddToMap(gatherRing, GatherRingPosition);
        }

        private static EntityModel CreateGatherRingEntityModel()
        {
            return new EntityModel
            {
                Id          = GatherRingEntityId,
                Type        = EntityType.Simple,
                Creature    = GatherRingCreatureId,
                World       = GatherRingWorldId,
                Area        = GatherRingAreaId,
                X           = GatherRingPosition.X,
                Y           = GatherRingPosition.Y,
                Z           = GatherRingPosition.Z,
                Rx          = GatherRingRotation.X,
                Ry          = GatherRingRotation.Y,
                Rz          = GatherRingRotation.Z,
                DisplayInfo = GatherRingDisplayInfo,
                Faction1    = GatherRingFactionId,
                Faction2    = GatherRingFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = GatherRingPublicEventId,
                    Phase   = GatherRingPublicEventPhase
                }
            };
        }

        private void OnPhaseIceBloodCoven()
        {
            SpawnColdbloodGate();

            publicEvent.ActivateObjective(PublicEventObjective.DefeatTheIcebloodCoven);
            publicEvent.ActivateObjective(PublicEventObjective.RescueThePellArchitect);
            publicEvent.ActivateObjective(PublicEventObjective.KillKrovakSummonersAndTheirFrostguards);
            publicEvent.ActivateObjective(PublicEventObjective.ConcurrentCovenCollapse);

            BroadcastCommunicatorMessage(CommunicatorMessage.TowerEngineerRenhakul11);

            ActivateWipOptionalObjective(PublicEventObjective.RescueWinterfuryPrisoners);
            ActivateWipOptionalObjective(PublicEventObjective.DestroySoulrotCanisters);
            ActivateWipOptionalObjective(PublicEventObjective.DisableSoulfrostTraps);
        }

        private void SpawnColdbloodGate()
        {
            if (coldbloodGateSpawned)
                return;

            // Build 16042 reviewed instance entity 1100300003 places the
            // Coldblood Gate in event 907 phase 2. Door choreography remains
            // blocked pending manual dungeon smoke evidence.
            IDoorEntity coldbloodGate = publicEvent.CreateEntity<IDoorEntity>();
            coldbloodGate.Initialise(CreateColdbloodGateEntityModel());
            AddToMap(coldbloodGate, ColdbloodGatePosition);
            coldbloodGateSpawned = true;
        }

        private static EntityModel CreateColdbloodGateEntityModel()
        {
            return new EntityModel
            {
                Id          = ColdbloodGateEntityId,
                Type        = EntityType.Door,
                Creature    = ColdbloodGateCreatureId,
                World       = ColdbloodGateWorldId,
                Area        = ColdbloodGateAreaId,
                X           = ColdbloodGatePosition.X,
                Y           = ColdbloodGatePosition.Y,
                Z           = ColdbloodGatePosition.Z,
                Rx          = ColdbloodGateRotation.X,
                Ry          = ColdbloodGateRotation.Y,
                Rz          = ColdbloodGateRotation.Z,
                DisplayInfo = ColdbloodGateDisplayInfo,
                Faction1    = ColdbloodGateFactionId,
                Faction2    = ColdbloodGateFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ColdbloodGatePublicEventId,
                    Phase   = ColdbloodGatePublicEventPhase
                }
            };
        }

        private void OnPhaseRisenHarizog()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatTheRisenHarizog);
            publicEvent.ActivateObjective(PublicEventObjective.InfusionInterdiction);

            BroadcastCommunicatorMessage(CommunicatorMessage.TowerEngineerRenhakul4);
        }

        private void BroadcastCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch ties these Renhakul
            // callouts to Coldblood phase handoffs, but optional objective selection and exact
            // dungeon route timing remain blocked pending manual smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch activates these
            // optional objectives by random 0/1 rolls; exact retail route selection, weights,
            // and trigger placement remain blocked pending manual dungeon smoke proof.
            if (ShouldActivateWipOptionalObjective(objective))
                publicEvent.ActivateObjective(objective);
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.FindThePellAttackingColdbloodCitadel:
                    publicEvent.SetPhase(PublicEventPhase.HailStoneGatecrasher);
                    break;
                case PublicEventObjective.DefeatHailStoneGatecrasher:
                    publicEvent.SetPhase(PublicEventPhase.IceBloodCoven);
                    break;
                case PublicEventObjective.DefeatTheIcebloodCoven:
                    publicEvent.SetPhase(PublicEventPhase.RisenHarizog);
                    break;
                case PublicEventObjective.DefeatTheRisenHarizog:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void AddToMap(IGridEntity entity, Vector3 position)
        {
            mapInstance.EnqueueAdd(entity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = position
            });
        }
    }
}
