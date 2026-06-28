using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness
{
    [ScriptFilterOwnerId(390)]
    public class SpaceMadnessEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool captainTeroSpawned;
        private bool majorLeeBarmySpawned;
        private bool observationDeckComputerSpawned;
        private bool panickedWorkerNightmaresSpawned;
        private bool crewDatapadsSpawned;
        private bool escapedExperimentsSpawned;
        private bool hazmatControlPanelSpawned;
        private bool hazmatSuitSpawned;
        private bool airScrubberControlsSpawned;
        private bool engineeringComputerSpawned;
        private bool hallucinatingLivestockSpawned;

        private const uint EnterAirlockWorldLocationId = 37702u;
        private const uint EnterAirlockObjectiveObjectId = 5512u;
        private static readonly Vector3 EnterAirlockTriggerPosition = new(-35.28216f, 4.942017f, 270.0408f);

        private const uint EnterResearchLaboratoryWorldLocationId = 37621u;
        private const uint EnterResearchLaboratoryObjectiveObjectId = 5531u;
        private static readonly Vector3 EnterResearchLaboratoryTriggerPosition = new(-55.08974f, -0.05234623f, 137.5821f);

        private const ushort SpaceMadnessWorldId = 2149;
        private const uint SpaceMadnessPublicEventId = 390u;

        private static readonly SpaceMadnessSpawnModel CaptainTeroSpawn = new(
            1100300005u,
            45900u,
            2483,
            0u,
            new Vector3(-14.70037f, 4.950619f, 337.0874f),
            Vector3.Zero,
            28578u,
            0,
            466,
            new SpaceMadnessStat(Stat.Health, 1f));

        private static readonly SpaceMadnessSpawnModel MajorLeeBarmySpawn = new(
            1100300006u,
            45812u,
            2482,
            1u,
            new Vector3(-40.0482f, 4.858062f, 310.5947f),
            Vector3.Zero,
            29020u,
            9128,
            219,
            new SpaceMadnessStat(Stat.Health, 1f));

        private static readonly SpaceMadnessSimpleSpawnModel ObservationDeckComputerSpawn = new(
            1100300007u,
            45972u,
            2415,
            2u,
            new Vector3(-39.72298f, 7.399887f, 158.1838f),
            Vector3.Zero,
            23951u,
            0,
            219);

        private static readonly SpaceMadnessSimpleSpawnModel[] CrewDatapadSpawns =
        [
            new(1100300014u, 45993u, 2416, 2u, new Vector3(-83f, 0f, 121f), Vector3.Zero, 27128u, 0, 219),
            new(1100300015u, 45993u, 2416, 2u, new Vector3(-61f, 4f, 190f), Vector3.Zero, 27128u, 0, 219),
            new(1100300016u, 45993u, 2416, 2u, new Vector3(-9f, 4f, 231f), Vector3.Zero, 27128u, 0, 219),
            new(1100300017u, 45993u, 2416, 2u, new Vector3(-23f, 7f, 166f), Vector3.Zero, 27128u, 0, 219),
            new(1100300018u, 45993u, 2416, 2u, new Vector3(-62f, 0f, 126f), Vector3.Zero, 27128u, 0, 219),
            new(1100300019u, 45993u, 2416, 2u, new Vector3(-65f, 6f, 249f), Vector3.Zero, 27128u, 0, 219),
            new(1100300020u, 45993u, 2416, 2u, new Vector3(22f, -1f, 101f), Vector3.Zero, 27128u, 0, 219),
            new(1100300021u, 45993u, 2416, 2u, new Vector3(-11f, -2f, 134f), Vector3.Zero, 27128u, 0, 219)
        ];

        private static readonly SpaceMadnessSpawnModel[] SavePanickedWorkersNightmareSpawns =
        [
            new(
                1100300026u,
                46123u,
                2413,
                2u,
                new Vector3(-68f, 4f, 231f),
                Vector3.Zero,
                21629u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 13621f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300027u,
                46124u,
                2413,
                2u,
                new Vector3(-14f, 4f, 233f),
                Vector3.Zero,
                28166u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 10925f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300028u,
                46126u,
                2414,
                2u,
                new Vector3(-47f, 4f, 205f),
                Vector3.Zero,
                26363u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 10925f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300029u,
                46127u,
                2414,
                2u,
                new Vector3(-50f, 4f, 206f),
                Vector3.Zero,
                22875u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 13382f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300030u,
                46128u,
                2414,
                2u,
                new Vector3(-92f, 4f, 199f),
                Vector3.Zero,
                22961u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 13621f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300031u,
                46130u,
                2414,
                2u,
                new Vector3(-87f, 0f, 124f),
                Vector3.Zero,
                28257u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 16213f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300032u,
                46132u,
                2416,
                2u,
                new Vector3(-62f, 0f, 133f),
                Vector3.Zero,
                26120u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 16213f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300033u,
                46133u,
                2415,
                2u,
                new Vector3(-29f, 8f, 161f),
                Vector3.Zero,
                28775u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 10925f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300034u,
                46135u,
                2416,
                2u,
                new Vector3(-62f, 0f, 145f),
                Vector3.Zero,
                21537u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 15929f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300035u,
                46721u,
                2416,
                2u,
                new Vector3(9f, -2f, 121f),
                Vector3.Zero,
                28257u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 10925f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300036u,
                58783u,
                2414,
                2u,
                new Vector3(-95f, 0f, 149f),
                Vector3.Zero,
                21688u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 13621f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300037u,
                58798u,
                2416,
                2u,
                new Vector3(-44f, -1f, 124f),
                Vector3.Zero,
                23468u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 8630f),
                new SpaceMadnessStat(Stat.Level, 32f))
        ];

        private static readonly SpaceMadnessSimpleSpawnModel HazmatControlPanelSpawn = new(
            1100300008u,
            46092u,
            2415,
            3u,
            new Vector3(7.77f, 7.399887f, 84.36f),
            Vector3.Zero,
            23951u,
            0,
            219);

        private static readonly SpaceMadnessSimpleSpawnModel AirScrubberControlsSpawn = new(
            1100300022u,
            46437u,
            2421,
            (uint)PublicEventPhase.ActivateAirScrubberControls,
            new Vector3(211f, -1f, 342f),
            Vector3.Zero,
            28014u,
            0,
            219);

        private static readonly SpaceMadnessSimpleSpawnModel HazmatSuitSpawn = new(
            1100300012u,
            45981u,
            2415,
            5u,
            new Vector3(4.41f, 0f, 72.89f),
            Vector3.Zero,
            23951u,
            0,
            219);

        private static readonly SpaceMadnessSimpleSpawnModel EngineeringComputerSpawn = new(
            1100300013u,
            45973u,
            2421,
            (uint)PublicEventPhase.SendTheAllClearSignal,
            new Vector3(241.969f, 3.34274f, 372.253f),
            Vector3.Zero,
            23951u,
            0,
            219);

        private static readonly SpaceMadnessSpawnModel[] HallucinatingLivestockSpawns =
        [
            new(
                1100300009u,
                46483u,
                2415,
                4u,
                new Vector3(25.23f, -2.91f, 120.42f),
                new Vector3(1.57079f, 0f, 0f),
                27827u,
                0,
                218,
                new SpaceMadnessStat(Stat.Health, 1f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300010u,
                46714u,
                2415,
                4u,
                new Vector3(7.12621f, -2.9093f, 112.0714f),
                new Vector3(2.64731f, 0f, 0f),
                26022u,
                9347,
                218,
                new SpaceMadnessStat(Stat.Health, 1f),
                new SpaceMadnessStat(Stat.Level, 32f)),
            new(
                1100300011u,
                46714u,
                2415,
                4u,
                new Vector3(12.919f, -2.91f, 124.5722f),
                new Vector3(2.64731f, 0f, 0f),
                26022u,
                9347,
                218,
                new SpaceMadnessStat(Stat.Health, 1f),
                new SpaceMadnessStat(Stat.Level, 32f))
        ];

        private static readonly SpaceMadnessSpawnModel[] EscapedExperimentSpawns =
        [
            new(
                1100300023u,
                69899u,
                2413,
                2u,
                new Vector3(-6f, 4f, 252f),
                Vector3.Zero,
                21689u,
                0,
                219,
                new SpaceMadnessStat(Stat.Health, 1f),
                new SpaceMadnessStat(Stat.Level, 1f)),
            new(
                1100300024u,
                69900u,
                2414,
                2u,
                new Vector3(-37f, 4f, 211f),
                Vector3.Zero,
                24917u,
                0,
                219,
                new SpaceMadnessStat(Stat.Health, 1f),
                new SpaceMadnessStat(Stat.Level, 1f)),
            new(
                1100300025u,
                69901u,
                2414,
                2u,
                new Vector3(-75f, 4f, 193f),
                Vector3.Zero,
                22787u,
                0,
                219,
                new SpaceMadnessStat(Stat.Health, 1f),
                new SpaceMadnessStat(Stat.Level, 1f))
        ];

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Space Madness requires a map instance.");

            captainTeroSpawned = false;
            majorLeeBarmySpawned = false;
            observationDeckComputerSpawned = false;
            crewDatapadsSpawned = false;
            escapedExperimentsSpawned = false;
            hazmatControlPanelSpawned = false;
            hazmatSuitSpawned = false;
            airScrubberControlsSpawned = false;
            engineeringComputerSpawned = false;
            hallucinatingLivestockSpawned = false;
            // Build 16042 objective 4705 is a zero-count ScriptWithoutMax row
            // with a 35-minute failure timer for Gold medal eligibility.
            publicEvent.ActivateObjective(PublicEventObjective.GoldMedalTimer);
            publicEvent.SetPhase(PublicEventPhase.TalkToCaptainTero);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToCaptainTero:
                    OnPhaseTalkToCaptainTero();
                    break;
                case PublicEventPhase.TalkToMajorLeeBarmy:
                    OnPhaseTalkToMajorLeeBarmy();
                    break;
                case PublicEventPhase.EnterTheAirlock:
                    OnPhaseEnterTheAirlock();
                    break;
                case PublicEventPhase.AccessTheObservationDeckComputer:
                    OnPhaseAccessTheObservationDeckComputer();
                    break;
                case PublicEventPhase.EnterTheResearchLaboratory:
                    OnPhaseEnterTheResearchLaboratory();
                    break;
                case PublicEventPhase.OpenHazmatStorageCloset:
                    OnPhaseOpenHazmatStorageCloset();
                    break;
                case PublicEventPhase.SurviveYourNightmares:
                    OnPhaseSurviveYourNightmares();
                    break;
                case PublicEventPhase.EquipAHazmatSuit:
                    OnPhaseEquipAHazmatSuit();
                    break;
                case PublicEventPhase.KillHallucinatingLivestock:
                    OnPhaseKillHallucinatingLivestock();
                    break;
                case PublicEventPhase.ActivateAirScrubberControls:
                    OnPhaseActivateAirScrubberControls();
                    break;
                case PublicEventPhase.SurviveTheAirScrubbingProcess:
                    OnPhaseSurviveTheAirScrubbingProcess();
                    break;
                case PublicEventPhase.SendTheAllClearSignal:
                    OnPhaseSendTheAllClearSignal();
                    break;
            }
        }

        private void OnPhaseTalkToCaptainTero()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToCaptainTero);
            SpawnReviewedTalkNpc(CaptainTeroSpawn, ref captainTeroSpawned);
        }

        private void OnPhaseTalkToMajorLeeBarmy()
        {
            publicEvent.ActivateObjective(PublicEventObjective.TalkToMajorLeeBarmy);
            SpawnReviewedTalkNpc(MajorLeeBarmySpawn, ref majorLeeBarmySpawned);
        }

        private void OnPhaseEnterTheAirlock()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EnterTheAirlock, mapInstance.PlayerCount);
            SpawnWipGuessedWorldLocationTrigger(
                EnterAirlockWorldLocationId,
                EnterAirlockObjectiveObjectId,
                EnterAirlockTriggerPosition);
        }

        private void OnPhaseAccessTheObservationDeckComputer()
        {
            publicEvent.ActivateObjective(PublicEventObjective.AccessTheObservationDeckComputer);
            SpawnReviewedSimplePlacement(ObservationDeckComputerSpawn, ref observationDeckComputerSpawned);
            publicEvent.ActivateObjective(PublicEventObjective.SavePanickedWorkers);
            SpawnReviewedNpcPlacements(SavePanickedWorkersNightmareSpawns, ref panickedWorkerNightmaresSpawned);
            publicEvent.ActivateObjective(PublicEventObjective.AccessCrewDatapads);
            SpawnReviewedSimplePlacements(CrewDatapadSpawns, ref crewDatapadsSpawned);
            publicEvent.ActivateObjective(PublicEventObjective.CollectAnExperimentalSlank);
            publicEvent.ActivateObjective(PublicEventObjective.CollectAnExperimentalRockmite);
            publicEvent.ActivateObjective(PublicEventObjective.CollectAPartyDowngrazer);
            publicEvent.ActivateObjective(PublicEventObjective.CollectEscapedCreatures, 3);
            SpawnReviewedNpcPlacements(EscapedExperimentSpawns, ref escapedExperimentsSpawned);
        }

        private void OnPhaseEnterTheResearchLaboratory()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EnterTheResearchLaboratory, mapInstance.PlayerCount);
            SpawnWipGuessedWorldLocationTrigger(
                EnterResearchLaboratoryWorldLocationId,
                EnterResearchLaboratoryObjectiveObjectId,
                EnterResearchLaboratoryTriggerPosition);
        }

        private void OnPhaseOpenHazmatStorageCloset()
        {
            publicEvent.ActivateObjective(PublicEventObjective.OpenHazmatStorageCloset);
            SpawnReviewedSimplePlacement(HazmatControlPanelSpawn, ref hazmatControlPanelSpawned);
        }

        private void OnPhaseSurviveYourNightmares()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SurviveYourNightmares);
        }

        private void OnPhaseEquipAHazmatSuit()
        {
            publicEvent.ActivateObjective(PublicEventObjective.EquipAHazmatSuit);
            SpawnReviewedSimplePlacement(HazmatSuitSpawn, ref hazmatSuitSpawned);
        }

        private void OnPhaseKillHallucinatingLivestock()
        {
            publicEvent.ActivateObjective(PublicEventObjective.KillHallucinatingLivestock);
            SpawnReviewedHallucinatingLivestock();
            publicEvent.ActivateObjective(PublicEventObjective.GiveAirHelmsToWorkers);
            publicEvent.ActivateObjective(PublicEventObjective.AvoidExplodingRowsdowers);
        }

        private void OnPhaseActivateAirScrubberControls()
        {
            publicEvent.ActivateObjective(PublicEventObjective.ActivateAirScrubberControls);
            SpawnReviewedSimplePlacement(AirScrubberControlsSpawn, ref airScrubberControlsSpawned);
        }

        private void OnPhaseSurviveTheAirScrubbingProcess()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SurviveTheAirScrubbingProcess);
        }

        private void OnPhaseSendTheAllClearSignal()
        {
            publicEvent.ActivateObjective(PublicEventObjective.SendTheAllClearSignal);

            // PublicEventObjective 1603, TargetGroup 6372 -> Creature2 45973,
            // WorldLocation2 37714, Jabbithole objective row 6196, and
            // DataMapping rows 146/16538 back this final-signal slice.
            SpawnReviewedSimplePlacement(EngineeringComputerSpawn, ref engineeringComputerSpawned);
        }

        private void SpawnWipGuessedWorldLocationTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            // WIP-GUESSED: LaughingWS supplies these Space Madness trigger ids/coordinates without
            // 16042 capture proof. Keep the guess scoped to participant-gather triggers until the
            // retail world-location rows and phase timing are mapped.
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(worldLocationId, objectId);
            AddToMap(triggerEntity, position);
        }

        private void SpawnReviewedTalkNpc(SpaceMadnessSpawnModel spawn, ref bool spawned)
        {
            if (spawned)
                return;

            // Build 16042 reviewed instance rows place the two opening talk NPCs
            // in event 390 phases 0 and 1. Their interaction scripts bind by
            // Creature2 and already credit the mapped TalkTo target groups.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(spawn));
            AddToMap(entity, spawn.Position);
            spawned = true;
        }

        private void SpawnReviewedSimplePlacement(SpaceMadnessSimpleSpawnModel spawn, ref bool spawned)
        {
            if (spawned)
                return;

            // Build 16042 reviewed rows bind these interactable Simple entities to
            // event 390 phases. Coordinates remain WIP/GUESSED per source notes,
            // so this only promotes placement/model visibility until client smoke.
            ISimpleEntity entity = publicEvent.CreateEntity<ISimpleEntity>();
            entity.Initialise(CreateSimpleEntityModel(spawn));
            AddToMap(entity, spawn.Position);
            spawned = true;
        }

        private void SpawnReviewedSimplePlacements(SpaceMadnessSimpleSpawnModel[] spawns, ref bool spawned)
        {
            if (spawned)
                return;

            foreach (SpaceMadnessSimpleSpawnModel spawn in spawns)
            {
                ISimpleEntity entity = publicEvent.CreateEntity<ISimpleEntity>();
                entity.Initialise(CreateSimpleEntityModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            spawned = true;
        }

        private void SpawnReviewedNpcPlacements(SpaceMadnessSpawnModel[] spawns, ref bool spawned)
        {
            if (spawned)
                return;

            foreach (SpaceMadnessSpawnModel spawn in spawns)
            {
                INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
                entity.Initialise(CreateEntityModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            spawned = true;
        }

        private void SpawnReviewedHallucinatingLivestock()
        {
            if (hallucinatingLivestockSpawned)
                return;

            // Build 16042 reviewed rows place these phase-4 targets inside the
            // livestock branch. Generic event-bound kill credit remains blocked
            // pending combat smoke, so this only promotes spawn/model evidence.
            foreach (SpaceMadnessSpawnModel spawn in HallucinatingLivestockSpawns)
            {
                INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
                entity.Initialise(CreateEntityModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            hallucinatingLivestockSpawned = true;
        }

        private static EntityModel CreateEntityModel(SpaceMadnessSpawnModel spawn)
        {
            EntityModel model = new()
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = SpaceMadnessWorldId,
                Area        = spawn.AreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                Rx          = spawn.Rotation.X,
                Ry          = spawn.Rotation.Y,
                Rz          = spawn.Rotation.Z,
                DisplayInfo = spawn.DisplayInfo,
                OutfitInfo  = spawn.OutfitInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = SpaceMadnessPublicEventId,
                    Phase   = spawn.Phase
                }
            };

            foreach (SpaceMadnessStat stat in spawn.Stats)
            {
                model.EntityStat.Add(new EntityStatModel
                {
                    Stat  = (byte)stat.Stat,
                    Value = stat.Value
                });
            }

            return model;
        }

        private static EntityModel CreateSimpleEntityModel(SpaceMadnessSimpleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = spawn.CreatureId,
                World       = SpaceMadnessWorldId,
                Area        = spawn.AreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                Rx          = spawn.Rotation.X,
                Ry          = spawn.Rotation.Y,
                Rz          = spawn.Rotation.Z,
                DisplayInfo = spawn.DisplayInfo,
                OutfitInfo  = spawn.OutfitInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
                QuestChecklistIdx = spawn.QuestChecklistIdx,
                EntityEvent = new EntityEventModel
                {
                    EventId = SpaceMadnessPublicEventId,
                    Phase   = spawn.Phase
                }
            };
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

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.TalkToCaptainTero:
                    publicEvent.SetPhase(PublicEventPhase.TalkToMajorLeeBarmy);
                    break;
                case PublicEventObjective.TalkToMajorLeeBarmy:
                    publicEvent.SetPhase(PublicEventPhase.EnterTheAirlock);
                    break;
                case PublicEventObjective.EnterTheAirlock:
                    publicEvent.SetPhase(PublicEventPhase.AccessTheObservationDeckComputer);
                    break;
                case PublicEventObjective.AccessTheObservationDeckComputer:
                    publicEvent.SetPhase(PublicEventPhase.EnterTheResearchLaboratory);
                    break;
                case PublicEventObjective.EnterTheResearchLaboratory:
                    publicEvent.SetPhase(PublicEventPhase.OpenHazmatStorageCloset);
                    break;
                case PublicEventObjective.OpenHazmatStorageCloset:
                    publicEvent.SetPhase(PublicEventPhase.SurviveYourNightmares);
                    break;
                case PublicEventObjective.SurviveYourNightmares:
                    publicEvent.SetPhase(PublicEventPhase.EquipAHazmatSuit);
                    break;
                case PublicEventObjective.EquipAHazmatSuit:
                    publicEvent.SetPhase(PublicEventPhase.KillHallucinatingLivestock);
                    break;
                case PublicEventObjective.KillHallucinatingLivestock:
                    publicEvent.SetPhase(PublicEventPhase.ActivateAirScrubberControls);
                    break;
                case PublicEventObjective.ActivateAirScrubberControls:
                    publicEvent.SetPhase(PublicEventPhase.SurviveTheAirScrubbingProcess);
                    break;
                case PublicEventObjective.SurviveTheAirScrubbingProcess:
                    publicEvent.SetPhase(PublicEventPhase.SendTheAllClearSignal);
                    break;
                case PublicEventObjective.SendTheAllClearSignal:
                    publicEvent.UpdateObjective(PublicEventObjective.GoldMedalTimer, 0);
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private sealed record SpaceMadnessSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            uint Phase,
            Vector3 Position,
            Vector3 Rotation,
            uint DisplayInfo,
            ushort OutfitInfo,
            ushort FactionId,
            params SpaceMadnessStat[] Stats);

        private sealed record SpaceMadnessSimpleSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            uint Phase,
            Vector3 Position,
            Vector3 Rotation,
            uint DisplayInfo,
            ushort OutfitInfo,
            ushort FactionId,
            byte QuestChecklistIdx = 0);

        private sealed record SpaceMadnessStat(
            Stat Stat,
            float Value);
    }
}
