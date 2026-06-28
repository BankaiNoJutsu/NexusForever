using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther
{
    [ScriptFilterOwnerId(781)]
    public class EvilFromTheEtherEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private uint gatherRingGuid;
        private uint gatherRingTriggerGuid;
        private bool driveDiagnosticsComplete;
        private bool driveSchematicsComplete;
        private uint medbayDoorControlGuid;
        private uint medbayDoorGuid;
        private uint securityChiefKondovichDoorGuid;
        private uint primaryPowerPlantDoorGuid;
        private uint upperDeckDoor1Guid;
        private uint upperDeckDoor2Guid;
        private uint upperDeckDoor3Guid;
        private uint gatherMarker1Guid;
        private uint gatherMarker2Guid;
        private uint floatingKatjaGuid;
        private bool driveSparksSpawned;
        private bool katjaZarkhovDefeated;
        private bool etherChargedRavenousKilled;

        private readonly List<uint> ravenousRefugeeGuids = [];

        private const ushort EvilFromTheEtherWorldId = 3404;
        private const uint EvilFromTheEtherPublicEventId = 781u;
        private const uint DriveSparkCreatureId = 71847u;
        // Reviewed entity_event rows store phase 23; keep this raw evidence value
        // even though the current script phase method is PickUpDriveSchematics.
        private const uint DriveSparkEventPhase = 23u;
        private const uint DriveSparkDisplayInfo = 24324u;
        private const ushort DriveSparkAreaId = 0;
        private const ushort DriveSparkOutfitInfo = 0;
        private const ushort DriveSparkFactionId = 219;

        private static readonly Vector3 DriveSparkRotation = new(-3.1415925f, 0f, 0f);

        private static readonly DriveSparkSpawnModel[] DriveSparkSpawns =
        [
            new(1100300018u, new Vector3(-62.29986f, -826.7423f, 315.39798f)),
            new(1100300019u, new Vector3(-42.11708f, -819.4943f, 316.413f)),
            new(1100300020u, new Vector3(-50.01624f, -826.74396f, 309.8f)),
            new(1100300021u, new Vector3(-63.714478f, -817.7603f, 312.456f)),
            new(1100300022u, new Vector3(-43.03138f, -826.57556f, 313.40698f)),
            new(1100300023u, new Vector3(-51.01272f, -819.90674f, 320.367f)),
            new(1100300024u, new Vector3(-59.60385f, -818.0261f, 320.757f)),
            new(1100300025u, new Vector3(-47.63262f, -813.7216f, 320.81198f)),
            new(1100300026u, new Vector3(-55.35622f, -815.50745f, 306.914f)),
            new(1100300027u, new Vector3(-58.10405f, -818.83215f, 310.94598f)),
            new(1100300028u, new Vector3(-51.504288f, -820.1912f, 304.748f))
        ];

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;
        private readonly IGlobalQuestManager globalQuestManager;

        public EvilFromTheEtherEventScript(
            ICinematicFactory cinematicFactory,
            IGlobalQuestManager globalQuestManager)
        {
            this.cinematicFactory   = cinematicFactory;
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
                ?? throw new InvalidOperationException("Evil from the Ether requires a map instance.");

            driveSparksSpawned = false;
            // Build 16042 objective 4944 is a zero-count ScriptWithoutMax row
            // with a 20-minute failure timer for Gold medal eligibility.
            publicEvent.ActivateObjective(PublicEventObjective.CompleteWithinGoldTimer);
            publicEvent.SetPhase(PublicEventPhase.TalkToCaptainWeir);
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
            switch ((PublicEventCreature)worldEntity.CreatureId)
            {
                case PublicEventCreature.GatherRing:
                    gatherRingGuid = worldEntity.Guid;
                    break;
                case PublicEventCreature.RavenousRefugeeM:
                case PublicEventCreature.RavenousRefugeeF:
                    ravenousRefugeeGuids.Add(worldEntity.Guid);
                    break;
                case PublicEventCreature.FloatingKatja:
                    floatingKatjaGuid = worldEntity.Guid;
                    break;
                case PublicEventCreature.UpperDeckDoor1:
                    upperDeckDoor1Guid = worldEntity.Guid;
                    break;
                case PublicEventCreature.UpperDeckDoor2:
                    upperDeckDoor2Guid = worldEntity.Guid;
                    break;
                case PublicEventCreature.UpperDeckDoor3:
                    upperDeckDoor3Guid = worldEntity.Guid;
                    break;
                case PublicEventCreature.GatherMarker1:
                    gatherMarker1Guid = worldEntity.Guid;
                    break;
                case PublicEventCreature.GatherMarker2:
                    gatherMarker2Guid = worldEntity.Guid;
                    break;
                case PublicEventCreature.MedbayDoorControl:
                    medbayDoorControlGuid = worldEntity.Guid;
                    break;
                case PublicEventCreature.Door:
                    OnAddToMapDoor(worldEntity);
                    break;
            }
        }

        private void OnAddToMapDoor(IWorldEntity entity)
        {
            switch (entity.ActivePropId)
            {
                case 7024518:
                    primaryPowerPlantDoorGuid = entity.Guid;
                    break;
                case 7059788:
                    securityChiefKondovichDoorGuid = entity.Guid;
                    break;
                case 7059789:
                    medbayDoorGuid = entity.Guid;
                    break;
            }
        }

        private void OnAddToMapWorldLocationEntity(IWorldLocationVolumeGridTriggerEntity worldLocationEntity)
        {
            if (worldLocationEntity.Entry.Id == 50278)
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
                    if (worldLocationEntity.Entry.Id == 50278)
                        gatherRingTriggerGuid = 0;
                    break;
                case IWorldEntity worldEntity:
                    OnRemoveFromMapWorldEntity(worldEntity);
                    break;
            }
        }

        private void OnRemoveFromMapWorldEntity(IWorldEntity worldEntity)
        {
            switch ((PublicEventCreature)worldEntity.CreatureId)
            {
                case PublicEventCreature.GatherRing:
                    gatherRingGuid = 0;
                    break;
                case PublicEventCreature.FloatingKatja:
                    floatingKatjaGuid = 0;
                    publicEvent.SetPhase(PublicEventPhase.DefeatKatjaZarkhovFight);
                    break;
                case PublicEventCreature.GatherMarker1:
                    gatherMarker1Guid = 0;
                    break;
                case PublicEventCreature.GatherMarker2:
                    gatherMarker2Guid = 0;
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
                case PublicEventPhase.TalkToCaptainWeir:
                    OnPhaseTalkToCaptainWeir();
                    break;
                case PublicEventPhase.GoToAirlock:
                    OnPhaseGoToAirlock();
                    break;
                case PublicEventPhase.OpenMedbay:
                    OnPhaseOpenMedbay();
                    break;
                case PublicEventPhase.ScavengeSpareParts:
                    OnPhaseScavengeSpareParts();
                    break;
                case PublicEventPhase.RepairDoor:
                    OnPhaseRepairDoor();
                    break;
                case PublicEventPhase.ActivateMedbayGenerator:
                    OnPhaseActivateMedbayGenerator();
                    break;
                case PublicEventPhase.GoToPrimaryPowerPlant:
                    OnPhaseGoToPrimaryPowerPlant();
                    break;
                case PublicEventPhase.KillSecurityChiefKondovich:
                    OnPhaseKillSecurityChiefKondovich();
                    break;
                case PublicEventPhase.GoToPrimaryPowerPlant2:
                    OnPhaseGoToPrimaryPowerPlant2();
                    break;
                case PublicEventPhase.RestartMainGenerators:
                    OnPhaseRestartMainGenerators();
                    break;
                case PublicEventPhase.EnterCrewQuarters:
                    OnPhaseEnterCrewQuarters();
                    break;
                case PublicEventPhase.DefeatEthericOrganisms:
                    OnPhaseDefeatEthericOrganisms();
                    break;
                case PublicEventPhase.RestoreTeleporter:
                    OnPhaseRestoreTeleporter();
                    break;
                case PublicEventPhase.FindTeleporter:
                    OnPhaseFindTeleporter();
                    break;
                case PublicEventPhase.DefeatEthericOrganisms2:
                    OnPhaseDefeatEthericOrganisms2();
                    break;
                case PublicEventPhase.GatherAroundTeleporter:
                    OnPhaseGatherAroundTeleporter();
                    break;
                case PublicEventPhase.TeleportToUpperDeck:
                    OnPhaseTeleportToUpperDeck();
                    break;
                case PublicEventPhase.GatherInBridgeAccessHall:
                    OnPhaseGatherInBridgeAccessHall();
                    break;
                case PublicEventPhase.DefeatTetheredOrganisms:
                    OnPhaseDefeatTetheredOrganisms();
                    break;
                case PublicEventPhase.GatherOnTheShadesBridge:
                    OnPhaseGatherOnTheShadesBridge();
                    break;
                case PublicEventPhase.DefeatTetheredOrganisms2:
                    OnPhaseDefeatTetheredOrganisms2();
                    break;
                case PublicEventPhase.ActivateSelfDestruct:
                    OnPhaseActivateSelfDestruct();
                    break;
                case PublicEventPhase.DefeatKatjaZarkhov:
                    OnPhaseDefeatKatjaZarkhov();
                    break;
                case PublicEventPhase.PickUpDriveSchematics:
                    OnPhasePickUpDriveSchematics();
                    break;
                case PublicEventPhase.EscapeToTheTeleporter:
                    OnPhaseEscapeToTheTeleporter();
                    break;
                case PublicEventPhase.TalkToCaptainWeir2:
                    OnPhaseTalkToCaptainWeir2();
                    break;
            }
        }

        private void OnPhaseTalkToCaptainWeir()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToCaptainWeir);
        }

        private void OnPhaseGoToAirlock()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GoToAirlock, mapInstance.PlayerCount);

            var triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(50278, 8242);
            AddToMap(triggerEntity, new Vector3(-396.43555f, -840.7188f, 119.74138f));
        }

        private void OnPhaseOpenMedbay()
        {
            publicEvent.ActivateObjective(PublicEventObjective.OpenMedbay);
            publicEvent.ActivateObjective(PublicEventObjective.DownloadCrewLogs);

            // Current fallback teleports players to (53.18, -852.86, -91.42) after the
            // medbay objectives activate, then removes the gather ring before the
            // CaptainWeir2 communicator follow-up; the retail transition trigger is
            // still unmapped.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.TeleportToLocal(new Vector3(53.180374f, -852.86273f, -91.41684f));

            mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(gatherRingTriggerGuid)?.RemoveFromMap();
            mapInstance.GetEntity<IWorldEntity>(gatherRingGuid)?.RemoveFromMap();

            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir2);
        }

        private void OnPhaseScavengeSpareParts()
        {
            publicEvent.ActivateObjective(PublicEventObjective.ScavengeSpareParts);

            // This slice broadcasts CaptainWeir3, queues IEvilFromTheEtherOnOpenMedbay,
            // and awakens each RavenousRefugeeEntityScript after the spare-parts
            // objective activates; retail timing still needs smoke.
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir3);

            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IEvilFromTheEtherOnOpenMedbay>());

            foreach (uint guid in ravenousRefugeeGuids)
                mapInstance.GetEntity<INonPlayerEntity>(guid)?.InvokeScriptCollection<RavenousRefugeeEntityScript>(s => s.Awaken());
        }

        private void OnPhaseRepairDoor()
        {
            publicEvent.ActivateObjective(PublicEventObjective.RepairDoor);
            mapInstance.GetEntity<IWorldEntity>(medbayDoorControlGuid)?.RemoveFromMap();
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir5);
        }

        private void OnPhaseActivateMedbayGenerator()
        {
            publicEvent.ActivateObjective(PublicEventObjective.ActivateMedbayGenerator);
            mapInstance.GetEntity<IDoorEntity>(medbayDoorGuid)?.OpenDoor();
        }

        private void OnPhaseGoToPrimaryPowerPlant()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GoToPrimaryPowerPlant, mapInstance.PlayerCount);
            SeedParticipantsInRangeObjective(PublicEventObjective.GoToPrimaryPowerPlant, securityChiefKondovichDoorGuid, primaryPowerPlantDoorGuid);
        }

        private void OnPhaseKillSecurityChiefKondovich()
        {
            publicEvent.ActivateObjective(PublicEventObjective.KillSecurityChiefKondovich);
            mapInstance.GetEntity<IDoorEntity>(securityChiefKondovichDoorGuid)?.OpenDoor();
        }

        private void OnPhaseGoToPrimaryPowerPlant2()
        {
            publicEvent.ResetObjective(PublicEventObjective.GoToPrimaryPowerPlant);
            publicEvent.ActivateObjective(PublicEventObjective.GoToPrimaryPowerPlant);
            SeedParticipantsInRangeObjective(PublicEventObjective.GoToPrimaryPowerPlant, securityChiefKondovichDoorGuid, primaryPowerPlantDoorGuid);
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir8);
        }

        private void OnPhaseRestartMainGenerators()
        {
            publicEvent.ActivateObjective(PublicEventObjective.RestartMainGenerators);
            publicEvent.ActivateObjective(PublicEventObjective.RestoreGeneratorAlphaPower);
            publicEvent.ActivateObjective(PublicEventObjective.RestoreGeneratorBetaPower);
            mapInstance.GetEntity<IDoorEntity>(primaryPowerPlantDoorGuid)?.OpenDoor();
            BroadcastCommunicatorMessage(CommunicatorMessage.InsaneCrewChief);
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir10);
        }

        private void OnPhaseEnterCrewQuarters()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EnterCrewQuarters);
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir11);
        }

        private void OnPhaseDefeatEthericOrganisms()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatEthericOrganisms);

            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IEvilFromTheEtherOnEthericOrganisms>());
        }

        private void OnPhaseRestoreTeleporter()
        {
            publicEvent.ActivateObjective(PublicEventObjective.RestoreTeleporter);
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir13);
        }

        private void OnPhaseFindTeleporter()
        {
            publicEvent.ActivateObjective(PublicEventObjective.FindTeleporter);

            var triggerEntity = publicEvent.CreateEntity<ITurnstileGridTriggerEntity>();
            triggerEntity.Initialise(8307, 15f, 8307);
            AddToMap(triggerEntity, new Vector3(-15.32f, -840.73f, 150.96f));

            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir14);
        }

        private void OnPhaseDefeatEthericOrganisms2()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatEthericOrganisms2);
        }

        private void OnPhaseGatherAroundTeleporter()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GatherAroundTeleporter, mapInstance.PlayerCount);

            // Build 16042 maps objective 4979 to ParticipantsInTriggerVolume
            // object 8312 at WorldLocation2 50632 in Auxiliary Power Plant.
            var triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(50632, 8312);
            AddToMap(triggerEntity, new Vector3(24.1576f, -840.142f, 173.326f));
        }

        private void OnPhaseTeleportToUpperDeck()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TeleportToUpperDeck, mapInstance.PlayerCount);

            var triggerEntity = publicEvent.CreateEntity<IGridTriggerEntity>();
            triggerEntity.Initialise(8242, 3f);
            AddToMap(triggerEntity, new Vector3(37.27052f, -840.065f, 173.36299f));

            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir15);
        }

        private void OnPhaseGatherInBridgeAccessHall()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GatherInBridgeAccessHall, mapInstance.PlayerCount);

            mapInstance.GetEntity<IDoorEntity>(upperDeckDoor1Guid)?.OpenDoor();

            // Spawn the bridge-access gather volume using world-location 50348 and
            // objective object 8260; exact retail placement still needs live verification.
            var triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(50348, 8260);
            AddToMap(triggerEntity, new Vector3(-53.3373f, -845.091f, 215.584f));

            BroadcastCommunicatorMessage(CommunicatorMessage.KatjaZarkov2);
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir16);
        }

        private void OnPhaseDefeatTetheredOrganisms()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatTetheredOrganisms);
            mapInstance.GetEntity<IWorldEntity>(gatherMarker1Guid)?.RemoveFromMap();
        }

        private void OnPhaseGatherOnTheShadesBridge()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GatherOnTheShadesBridge, mapInstance.PlayerCount);

            mapInstance.GetEntity<IDoorEntity>(upperDeckDoor2Guid)?.OpenDoor();
            mapInstance.GetEntity<IDoorEntity>(upperDeckDoor3Guid)?.OpenDoor();

            // Spawn the Shades Bridge gather volume using world-location 50349 and
            // objective object 8261; exact retail placement still needs live verification.
            var triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(50349, 8261);
            AddToMap(triggerEntity, new Vector3(-53.3725f, -842.341f, 282.618f));
        }

        private void OnPhaseDefeatTetheredOrganisms2()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatTetheredOrganisms2, 3);

            mapInstance.GetEntity<IWorldEntity>(gatherMarker2Guid)?.RemoveFromMap();
            mapInstance.GetEntity<INonPlayerEntity>(floatingKatjaGuid)?.InvokeScriptCollection<KatjaZarkhovFloatingEntityScript>(s => s.StartMoveToPortal());

            BroadcastCommunicatorMessage(CommunicatorMessage.KatjaZarkov3);
        }

        private void OnPhaseActivateSelfDestruct()
        {
            publicEvent.ActivateObjective(PublicEventObjective.ActivateSelfDestruct);
            BroadcastCommunicatorMessage(CommunicatorMessage.KatjaZarkov4);
            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir17);
        }

        private void OnPhaseDefeatKatjaZarkhov()
        {
            katjaZarkhovDefeated       = false;
            etherChargedRavenousKilled = false;

            publicEvent.ActivateObjective(PublicEventObjective.DefeatKatjaZarkhov);
            publicEvent.ActivateObjective(PublicEventObjective.KillEtherChargedRavenous);
            mapInstance.GetEntity<INonPlayerEntity>(floatingKatjaGuid)?.InvokeScriptCollection<KatjaZarkhovFloatingEntityScript>(s => s.KnockbackToFloor());
            BroadcastCommunicatorMessage(CommunicatorMessage.KatjaZarkov5);
        }

        private void OnPhasePickUpDriveSchematics()
        {
            driveDiagnosticsComplete = false;
            driveSchematicsComplete  = false;

            publicEvent.ActivateObjective(PublicEventObjective.DriveDiagnostics);
            publicEvent.ActivateObjective(PublicEventObjective.PickUpDriveSchematics);

            SpawnReviewedDriveSparks();
        }

        private void OnPhaseEscapeToTheTeleporter()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EscapeToTheTeleporter);

            var triggerEntity = publicEvent.CreateEntity<IGridTriggerEntity>();
            triggerEntity.Initialise(8243, 3f);
            AddToMap(triggerEntity, new Vector3(-53.353714f, -845.00726f, 164.51099f));

            BroadcastCommunicatorMessage(CommunicatorMessage.CaptainWeir18);
        }

        private void OnPhaseTalkToCaptainWeir2()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToCaptainWeir2);
        }

        private void SpawnReviewedDriveSparks()
        {
            if (driveSparksSpawned)
                return;

            foreach (DriveSparkSpawnModel spawn in DriveSparkSpawns)
            {
                INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
                entity.Initialise(CreateDriveSparkModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            driveSparksSpawned = true;
        }

        private static EntityModel CreateDriveSparkModel(DriveSparkSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = DriveSparkCreatureId,
                World       = EvilFromTheEtherWorldId,
                Area        = DriveSparkAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                Rx          = DriveSparkRotation.X,
                Ry          = DriveSparkRotation.Y,
                Rz          = DriveSparkRotation.Z,
                DisplayInfo = DriveSparkDisplayInfo,
                OutfitInfo  = DriveSparkOutfitInfo,
                Faction1    = DriveSparkFactionId,
                Faction2    = DriveSparkFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = EvilFromTheEtherPublicEventId,
                    Phase   = DriveSparkEventPhase
                }
            };
        }

        private void AddToMap(IGridEntity entity, Vector3 position)
        {
            // Enqueues a caller-configured grid entity at an explicit script position;
            // retail spawn timing and placement still need live verification.
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

        private void SeedParticipantsInRangeObjective(PublicEventObjective objectiveId, params uint[] entityGuids)
        {
            // Seed progress for players already inside the supplied entity ranges before
            // the objective phase starts.
            HashSet<ulong> characterIds = [];
            foreach (uint entityGuid in entityGuids)
            {
                if (entityGuid == 0u)
                    continue;

                IGridEntity entity = mapInstance.GetEntity<IGridEntity>(entityGuid);
                if (entity == null)
                    continue;

                foreach (IPlayer player in entity.GetInRange<IPlayer>(0u))
                    characterIds.Add(player.CharacterId);
            }

            if (characterIds.Count > 0)
                publicEvent.UpdateObjective(objectiveId, characterIds.Count);
        }

        private void BroadcastCommunicatorMessage(CommunicatorMessage message)
        {
            // Broadcast the selected communicator message to every player currently in
            // the instance.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void SendCommunicatorMessage(IPlayer player, CommunicatorMessage message)
        {
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            communicatorMessage?.Send(player.Session);
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.TalkToCaptainWeir:
                    publicEvent.SetPhase(PublicEventPhase.GoToAirlock);
                    break;
                case PublicEventObjective.GoToAirlock:
                    publicEvent.SetPhase(PublicEventPhase.OpenMedbay);
                    break;
                case PublicEventObjective.OpenMedbay:
                    publicEvent.SetPhase(PublicEventPhase.ScavengeSpareParts);
                    break;
                case PublicEventObjective.ScavengeSpareParts:
                    publicEvent.SetPhase(PublicEventPhase.RepairDoor);
                    break;
                case PublicEventObjective.RepairDoor:
                    publicEvent.SetPhase(PublicEventPhase.ActivateMedbayGenerator);
                    break;
                case PublicEventObjective.ActivateMedbayGenerator:
                    publicEvent.SetPhase(PublicEventPhase.GoToPrimaryPowerPlant);
                    break;
                case PublicEventObjective.GoToPrimaryPowerPlant:
                    publicEvent.SetPhase(publicEvent.Phase == (uint)PublicEventPhase.GoToPrimaryPowerPlant
                        ? PublicEventPhase.KillSecurityChiefKondovich
                        : PublicEventPhase.RestartMainGenerators);
                    break;
                case PublicEventObjective.KillSecurityChiefKondovich:
                    publicEvent.SetPhase(PublicEventPhase.GoToPrimaryPowerPlant2);
                    break;
                case PublicEventObjective.RestartMainGenerators:
                    publicEvent.SetPhase(PublicEventPhase.EnterCrewQuarters);
                    break;
                case PublicEventObjective.EnterCrewQuarters:
                    publicEvent.SetPhase(PublicEventPhase.DefeatEthericOrganisms);
                    break;
                case PublicEventObjective.DefeatEthericOrganisms:
                    publicEvent.SetPhase(PublicEventPhase.RestoreTeleporter);
                    break;
                case PublicEventObjective.RestoreTeleporter:
                    publicEvent.SetPhase(PublicEventPhase.FindTeleporter);
                    break;
                case PublicEventObjective.FindTeleporter:
                    publicEvent.SetPhase(PublicEventPhase.DefeatEthericOrganisms2);
                    break;
                case PublicEventObjective.DefeatEthericOrganisms2:
                    publicEvent.SetPhase(PublicEventPhase.GatherAroundTeleporter);
                    break;
                case PublicEventObjective.GatherAroundTeleporter:
                    publicEvent.SetPhase(PublicEventPhase.TeleportToUpperDeck);
                    break;
                case PublicEventObjective.TeleportToUpperDeck:
                    publicEvent.SetPhase(PublicEventPhase.GatherInBridgeAccessHall);
                    break;
                case PublicEventObjective.GatherInBridgeAccessHall:
                    publicEvent.SetPhase(PublicEventPhase.DefeatTetheredOrganisms);
                    break;
                case PublicEventObjective.DefeatTetheredOrganisms:
                    publicEvent.SetPhase(PublicEventPhase.GatherOnTheShadesBridge);
                    break;
                case PublicEventObjective.GatherOnTheShadesBridge:
                    publicEvent.SetPhase(PublicEventPhase.DefeatTetheredOrganisms2);
                    break;
                case PublicEventObjective.DefeatTetheredOrganisms2:
                    publicEvent.SetPhase(PublicEventPhase.ActivateSelfDestruct);
                    break;
                case PublicEventObjective.ActivateSelfDestruct:
                    publicEvent.SetPhase(PublicEventPhase.DefeatKatjaZarkhov);
                    break;
                case PublicEventObjective.DefeatKatjaZarkhov:
                case PublicEventObjective.KillEtherChargedRavenous:
                    OnKatjaFightObjectiveComplete((PublicEventObjective)objective.Entry.Id);
                    break;
                case PublicEventObjective.DriveDiagnostics:
                case PublicEventObjective.PickUpDriveSchematics:
                    OnEndPhasePickupObjectiveComplete((PublicEventObjective)objective.Entry.Id);
                    break;
                case PublicEventObjective.EscapeToTheTeleporter:
                    publicEvent.SetPhase(PublicEventPhase.TalkToCaptainWeir2);
                    break;
                case PublicEventObjective.TalkToCaptainWeir2:
                    publicEvent.UpdateObjective(PublicEventObjective.CompleteWithinGoldTimer, 0);
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void OnKatjaFightObjectiveComplete(PublicEventObjective objective)
        {
            switch (objective)
            {
                case PublicEventObjective.DefeatKatjaZarkhov:
                    katjaZarkhovDefeated = true;
                    break;
                case PublicEventObjective.KillEtherChargedRavenous:
                    etherChargedRavenousKilled = true;
                    break;
            }

            if (katjaZarkhovDefeated && etherChargedRavenousKilled)
                publicEvent.SetPhase(PublicEventPhase.PickUpDriveSchematics);
        }

        private void OnEndPhasePickupObjectiveComplete(PublicEventObjective objective)
        {
            switch (objective)
            {
                case PublicEventObjective.DriveDiagnostics:
                    driveDiagnosticsComplete = true;
                    break;
                case PublicEventObjective.PickUpDriveSchematics:
                    driveSchematicsComplete = true;
                    break;
            }

            if (driveDiagnosticsComplete && driveSchematicsComplete)
                publicEvent.SetPhase(PublicEventPhase.EscapeToTheTeleporter);
        }

        /// <summary>
        /// Invoked when a cinematic for <see cref="IPlayer"/> has finished.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            // Use the current public-event phase to send the next communicator follow-up
            // to the player who finished the cinematic.
            switch ((PublicEventPhase)publicEvent.Phase)
            {
                case PublicEventPhase.TalkToCaptainWeir:
                    SendCommunicatorMessage(player, CommunicatorMessage.CaptainWeir1);
                    break;
                case PublicEventPhase.ScavengeSpareParts:
                    SendCommunicatorMessage(player, CommunicatorMessage.CaptainWeir4);
                    break;
                case PublicEventPhase.DefeatEthericOrganisms:
                    SendCommunicatorMessage(player, CommunicatorMessage.CaptainWeir12);
                    break;
            }
        }

        private sealed record DriveSparkSpawnModel(uint EntityId, Vector3 Position);
    }
}
