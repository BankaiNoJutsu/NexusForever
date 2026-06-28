using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.Datascape
{
    [ScriptFilterOwnerId(157)]
    public class DatascapeEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ICinematicFactory cinematicFactory;
        private readonly HashSet<PublicEventObjective> retrievedDatacores = [];

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private readonly HashSet<uint> spawnedReviewedEntityIds = [];
        private bool openingEnemiesSpawned;

        private const ushort DatascapeWorldId = 1333;
        private const uint DatascapePublicEventId = 157u;
        private const uint OpeningPhase = 0u;
        private const uint FirstFrostBoulderPhase = 3u;
        private const uint SecondFrostBoulderPhase = 4u;
        private const uint FrostbringerWarlockPhase = 5u;

        private static readonly DatascapeSpawnModel[] OpeningSpawns =
        [
            new(1100300059u, 61819u, 1301, OpeningPhase, new Vector3(618.551f, -215.61023f, 75f), Vector3.Zero, 30425u, 1171, "OptimizedMemoryProbeED1EntityScript", 15900000f),
            new(1100300060u, 61818u, 1301, OpeningPhase, new Vector3(865f, -215.6028f, -173.5f), new Vector3(1.55485f, 0f, 0f), 30425u, 1171, "OptimizedMemoryProbeTX-67EntityScript", 15900000f),
            new(1100300061u, 31667u, 1301, OpeningPhase, new Vector3(618f, -216.15657f, -421f), new Vector3(3.10206f, 0f, 0f), 30425u, 1171, "OptimizedMemoryProbeP2ZEntityScript", 15900000f),
            new(1100300062u, 30495u, 1349, OpeningPhase, new Vector3(132.5f, -226.5f, -67f), Vector3.Zero, 33172u, 1171, "NullSystemDaemonEntityScript", 14400000f),
            new(1100300063u, 30496u, 1349, OpeningPhase, new Vector3(132.5f, -226.5f, -263f), new Vector3(3.08953f, 0f, 0f), 33172u, 1171, "BinarySystemDaemonEntityScript", 14400000f)
        ];

        private static readonly DatascapeSpawnModel FirstFrostBoulderSpawn = new(
            1100300064u,
            31677u,
            4475,
            FirstFrostBoulderPhase,
            new Vector3(3356.48f, -765.57f, -3246.29f),
            new Vector3(0.17427f, 0f, 0f),
            27434u,
            1171,
            "FrostBoulderAvalancheFirstEntityScript",
            14500000f);

        private static readonly DatascapeSpawnModel SecondFrostBoulderSpawn = new(
            1100300065u,
            56200u,
            4476,
            SecondFrostBoulderPhase,
            new Vector3(3635.073f, -745.20f, -3373.29f),
            new Vector3(1.11529f, 0f, 0f),
            27434u,
            1171,
            "FrostBoulderAvalancheSecondEntityScript",
            14500000f);

        private static readonly DatascapeSpawnModel FrostbringerWarlockSpawn = new(
            1100300066u,
            31674u,
            4476,
            FrostbringerWarlockPhase,
            new Vector3(3328.50f, -696.86f, -3639.44f),
            new Vector3(-1.42842f, 0f, 0f),
            23490u,
            1171,
            "FrostbringerWarlockEntityScript",
            15900000f);

        private static readonly DatascapeSpawnModel BioEnhancedBroodmotherSpawn = new(
            1100300067u,
            31885u,
            1590,
            OpeningPhase,
            new Vector3(2985.78f, -794.348f, 3396.41f),
            new Vector3(-0.693315f, 0f, 0f),
            27107u,
            1171,
            "BioEnhancedBroodmotherEntityScript",
            18000000f);

        private static readonly DatascapeSpawnModel GloomclawSpawn = new(
            1100300068u,
            30498u,
            1609,
            (uint)PublicEventPhase.Gloomclaw,
            new Vector3(4310f, -567.817f, -16812f),
            new Vector3(-3.08841f, 0f, 0f),
            32992u,
            1171,
            "GloomclawEntityScript",
            21000000f);

        private static readonly DatascapeSpawnModel HyperAcceleratedSkeledroidSpawn = new(
            1100300069u,
            48065u,
            2371,
            (uint)PublicEventPhase.LogicWingRoom1,
            new Vector3(-22076.3f, 598.146f, -15934.1f),
            new Vector3(1f, 0f, 0f),
            28678u,
            1171,
            "HyperAcceleratedSkeledroidEntityScript",
            21300000f);

        private static readonly DatascapeSpawnModel AugmentedHeraldOfAvatusSpawn = new(
            1100300070u,
            48374u,
            2373,
            (uint)PublicEventPhase.LogicWingRoom3,
            new Vector3(-22050.2f, 619.71f, -14309.2f),
            new Vector3(2.56983f, 0f, 0f),
            28901u,
            1171,
            "AugmentedHeraldOfAvatusEntityScript",
            9100000f);

        private static readonly DatascapeSpawnModel WarmongerAgrathaSpawn = new(
            1100300071u,
            48295u,
            1594,
            (uint)PublicEventPhase.WarmongerAgratha,
            new Vector3(-4018.35f, -905.709f, 2575.45f),
            new Vector3(-1.7226f, 0f, 0f),
            36939u,
            1171,
            "WarmongerAgrathaEntityScript",
            12500000f);

        private static readonly DatascapeSpawnModel WarmongerChunaSpawn = new(
            1100300072u,
            48916u,
            4478,
            (uint)PublicEventPhase.WarmongerChuna,
            new Vector3(-4350.04f, -913.187f, 2404.41f),
            new Vector3(-1.44454f, 0f, 0f),
            36939u,
            1171,
            "WarmongerChunaEntityScript",
            12500000f);

        private static readonly DatascapeSpawnModel WarmongerTalariiSpawn = new(
            1100300073u,
            48917u,
            4479,
            (uint)PublicEventPhase.WarmongerTalarii,
            new Vector3(-4359.49f, -903.383f, 2590.74f),
            new Vector3(0.19367f, 0f, 0f),
            36939u,
            1171,
            "WarmongerTalariiEntityScript",
            12500000f);

        private static readonly DatascapeSpawnModel GrandWarmongerTargreshSpawn = new(
            1100300074u,
            48177u,
            1479,
            (uint)PublicEventPhase.GrandWarmongerTargresh,
            new Vector3(-4403.72f, -821.913f, 2679.14f),
            new Vector3(-1.63275f, 0f, 0f),
            36938u,
            1171,
            "GrandWarmongerTargreshEntityScript",
            14500000f);

        private static readonly DatascapeSpawnModel AvatusSpawn = new(
            1100300075u,
            30505u,
            1301,
            (uint)PublicEventPhase.Avatus,
            new Vector3(618f, -198.7f, -174f),
            new Vector3(1.61258f, 0f, 0f),
            28937u,
            1171,
            "DatascapeAvatusEntityScript",
            72000000f);

        public DatascapeEventScript(
            IGlobalQuestManager globalQuestManager,
            ICinematicFactory cinematicFactory)
        {
            this.globalQuestManager = globalQuestManager;
            this.cinematicFactory   = cinematicFactory;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Datascape requires a map instance.");

            retrievedDatacores.Clear();
            spawnedReviewedEntityIds.Clear();
            openingEnemiesSpawned = false;
            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                case PublicEventPhase.HallsOfTheInfiniteMind:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheSystemDaemons);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOptimizedMemoryProbeED1);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOptimizedMemoryProbeP2Z);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOptimizedMemoryProbeTX67);
                    SpawnOpeningEnemies();
                    break;
                case PublicEventPhase.TheOculus:
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker111);
                    break;
                case PublicEventPhase.FirstFrostBoulder:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche);
                    SpawnFirstFrostBoulder();
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker112);
                    break;
                case PublicEventPhase.SecondFrostBoulder:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche);
                    SpawnSecondFrostBoulder();
                    break;
                case PublicEventPhase.FrostbringerWarlock:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFrostbringerWarlock);
                    SpawnFrostbringerWarlock();
                    break;
                case PublicEventPhase.MaelstromAuthority:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMaelstromAuthority);
                    break;
                case PublicEventPhase.AlphaElementalGuardians:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheElementalGuardians1);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker113);
                    break;
                case PublicEventPhase.AlphaPersonalityDatacore:
                    publicEvent.ActivateObjective(PublicEventObjective.RetrieveTheFirstPersonalityDatacore);
                    break;
                case PublicEventPhase.EarthRoomCanimid:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFullyOptimizedCanimid);
                    break;
                case PublicEventPhase.EarthRoomRock:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheLogicGuidedRockslide);
                    break;
                case PublicEventPhase.Gloomclaw:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGloomclaw);
                    SpawnGloomclaw();
                    break;
                case PublicEventPhase.LogicWingRoom1:
                    publicEvent.ActivateObjective(PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge1);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge2);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge3);
                    break;
                case PublicEventPhase.LogicWingRoom2:
                    publicEvent.ActivateObjective(PublicEventObjective.PowerUpTheEldanPowerGenerators);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge4);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge5);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge6);
                    break;
                case PublicEventPhase.LogicWingRoom3:
                    publicEvent.ActivateObjective(PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators3);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge7);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge8);
                    publicEvent.ActivateObjective(PublicEventObjective.GeneratorCharge9);
                    break;
                case PublicEventPhase.LogicWingLogicElemental:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheAbstractAugmentationAlgorithm);
                    break;
                case PublicEventPhase.DeltaElementalGuardians:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheElementalGuardians3);
                    break;
                case PublicEventPhase.DeltaPersonalityDatacore:
                    publicEvent.ActivateObjective(PublicEventObjective.RetrieveTheSecondPersonalityDatacore);
                    break;
                case PublicEventPhase.VolatilityLattice:
                    publicEvent.ActivateObjective(PublicEventObjective.EscapeAvatusAttention);
                    publicEvent.ActivateObjective(PublicEventObjective.TimedOut);
                    break;
                case PublicEventPhase.WarmongerAgratha:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWarmongerAgratha);
                    SpawnWarmongerAgratha();
                    break;
                case PublicEventPhase.WarmongerChuna:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWarmongerChuna);
                    SpawnWarmongerChuna();
                    break;
                case PublicEventPhase.WarmongerTalarii:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWarmongerTalarii);
                    SpawnWarmongerTalarii();
                    break;
                case PublicEventPhase.GrandWarmongerTargresh:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGrandWarmongerTargresh);
                    SpawnGrandWarmongerTargresh();
                    break;
                case PublicEventPhase.BetaElementalGuardians:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheElementalGuardians2);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.Caretaker114);
                    break;
                case PublicEventPhase.BetaPersonalityDatacore:
                    publicEvent.ActivateObjective(PublicEventObjective.RetrieveTheThirdPersonalityDatacore);
                    break;
                case PublicEventPhase.MemoryCores:
                    publicEvent.ActivateObjective(PublicEventObjective.PlaceTheDatacoresInTheOculus);
                    break;
                case PublicEventPhase.Avatus:
                    QueueWipGuessedCinematic<IDatascapeAvatusSpawn>();
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatAvatus);
                    SpawnAvatus();
                    break;
            }
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
                case PublicEventObjective.DefeatTheSystemDaemons:
                    publicEvent.SetPhase(PublicEventPhase.TheOculus);
                    break;
                case PublicEventObjective.DefeatOptimizedMemoryProbeP2Z:
                    publicEvent.ActivateObjective(PublicEventObjective.EscapeTheLimboInfomatrix);
                    break;
                case PublicEventObjective.EscapeTheLimboInfomatrix:
                    publicEvent.SetPhase(PublicEventPhase.FirstFrostBoulder);
                    break;
                case PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche:
                    publicEvent.SetPhase(PublicEventPhase.SecondFrostBoulder);
                    break;
                case PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche:
                    publicEvent.SetPhase(PublicEventPhase.FrostbringerWarlock);
                    break;
                case PublicEventObjective.DefeatTheFrostbringerWarlock:
                    publicEvent.SetPhase(PublicEventPhase.MaelstromAuthority);
                    break;
                case PublicEventObjective.DefeatTheMaelstromAuthority:
                    publicEvent.SetPhase(PublicEventPhase.AlphaPersonalityDatacore);
                    break;
                case PublicEventObjective.DefeatOptimizedMemoryProbeED1:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheBioEnhancedBroodmother);
                    SpawnBioEnhancedBroodmother();
                    break;
                case PublicEventObjective.DefeatTheBioEnhancedBroodmother:
                    publicEvent.SetPhase(PublicEventPhase.EarthRoomCanimid);
                    break;
                case PublicEventObjective.DefeatTheFullyOptimizedCanimid:
                    publicEvent.SetPhase(PublicEventPhase.EarthRoomRock);
                    break;
                case PublicEventObjective.DefeatTheLogicGuidedRockslide:
                    publicEvent.SetPhase(PublicEventPhase.Gloomclaw);
                    break;
                case PublicEventObjective.DefeatGloomclaw:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingRoom1);
                    break;
                case PublicEventObjective.GeneratorCharge2:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid);
                    SpawnHyperAcceleratedSkeledroid();
                    break;
                case PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid:
                    publicEvent.ActivateObjective(PublicEventObjective.DefyPerspective);
                    break;
                case PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingRoom2);
                    break;
                case PublicEventObjective.GeneratorCharge8:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheAugmentedHeraldOfAvatus);
                    SpawnAugmentedHeraldOfAvatus();
                    break;
                case PublicEventObjective.PowerUpTheEldanPowerGenerators:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingRoom3);
                    break;
                case PublicEventObjective.PowerUpAllOfTheEldanPowerGenerators3:
                    publicEvent.SetPhase(PublicEventPhase.LogicWingLogicElemental);
                    break;
                case PublicEventObjective.DefeatTheAbstractAugmentationAlgorithm:
                    publicEvent.SetPhase(PublicEventPhase.DeltaElementalGuardians);
                    break;
                case PublicEventObjective.DefeatTheElementalGuardians3:
                    publicEvent.SetPhase(PublicEventPhase.DeltaPersonalityDatacore);
                    break;
                case PublicEventObjective.DefeatOptimizedMemoryProbeTX67:
                    publicEvent.SetPhase(PublicEventPhase.VolatilityLattice);
                    break;
                case PublicEventObjective.EscapeAvatusAttention:
                    publicEvent.SetPhase(PublicEventPhase.WarmongerAgratha);
                    break;
                case PublicEventObjective.DefeatWarmongerAgratha:
                    publicEvent.SetPhase(PublicEventPhase.WarmongerChuna);
                    break;
                case PublicEventObjective.DefeatWarmongerChuna:
                    publicEvent.SetPhase(PublicEventPhase.WarmongerTalarii);
                    break;
                case PublicEventObjective.DefeatWarmongerTalarii:
                    publicEvent.SetPhase(PublicEventPhase.GrandWarmongerTargresh);
                    break;
                case PublicEventObjective.DefeatGrandWarmongerTargresh:
                    publicEvent.SetPhase(PublicEventPhase.BetaElementalGuardians);
                    break;
                case PublicEventObjective.DefeatTheElementalGuardians2:
                    publicEvent.SetPhase(PublicEventPhase.BetaPersonalityDatacore);
                    break;
                case PublicEventObjective.RetrieveTheFirstPersonalityDatacore:
                case PublicEventObjective.RetrieveTheSecondPersonalityDatacore:
                case PublicEventObjective.RetrieveTheThirdPersonalityDatacore:
                    OnDatacoreRetrieved((PublicEventObjective)objective.Entry.Id);
                    break;
                case PublicEventObjective.PlaceTheDatacoresInTheOculus:
                    publicEvent.SetPhase(PublicEventPhase.Avatus);
                    break;
                case PublicEventObjective.DefeatAvatus:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void OnDatacoreRetrieved(PublicEventObjective objective)
        {
            retrievedDatacores.Add(objective);

            if (retrievedDatacores.Count == 3)
                publicEvent.SetPhase(PublicEventPhase.MemoryCores);
        }

        private void SpawnOpeningEnemies()
        {
            if (openingEnemiesSpawned)
                return;

            // Build 16042 reviewed instance rows place the two daemon variants and
            // three Optimized Memory Probes in Datascape phase 0. Their exact
            // combat choreography remains blocked, but the spawn/model/script
            // bindings are enough to make the opening objectives interactable.
            foreach (DatascapeSpawnModel spawn in OpeningSpawns)
            {
                INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
                entity.Initialise(CreateEntityModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            openingEnemiesSpawned = true;
        }

        private void SpawnFirstFrostBoulder()
        {
            // The branch gives a reviewed placed First Frost Boulder row for
            // Datascape phase 3. Exact avalanche mechanics/choreography remain
            // blocked, but the entity placement and credit hook are now wired.
            SpawnReviewedEntityOnce(FirstFrostBoulderSpawn);
        }

        private void SpawnSecondFrostBoulder()
        {
            // The branch gives a reviewed placed Second Frost Boulder row for
            // Datascape phase 4. Exact avalanche mechanics/choreography remain
            // blocked, but the entity placement and credit hook are now wired.
            SpawnReviewedEntityOnce(SecondFrostBoulderSpawn);
        }

        private void SpawnFrostbringerWarlock()
        {
            // The branch gives a reviewed placed Frostbringer Warlock row for
            // Datascape phase 5. Exact encounter mechanics/choreography remain
            // blocked, but the entity placement and credit hook are now wired.
            SpawnReviewedEntityOnce(FrostbringerWarlockSpawn);
        }

        private void SpawnBioEnhancedBroodmother()
        {
            // The reviewed Broodmother row is bound to phase 0, while the
            // current script activates its objective after ED-1 dies.
            SpawnReviewedEntityOnce(BioEnhancedBroodmotherSpawn);
        }

        private void SpawnGloomclaw()
        {
            SpawnReviewedEntityOnce(GloomclawSpawn);
        }

        private void SpawnHyperAcceleratedSkeledroid()
        {
            // GeneratorCharge2 is the mapped trigger that exposes this
            // Logic Wing sub-objective.
            SpawnReviewedEntityOnce(HyperAcceleratedSkeledroidSpawn);
        }

        private void SpawnAugmentedHeraldOfAvatus()
        {
            // GeneratorCharge8 is the mapped trigger that exposes this
            // Logic Wing sub-objective.
            SpawnReviewedEntityOnce(AugmentedHeraldOfAvatusSpawn);
        }

        private void SpawnWarmongerAgratha()
        {
            SpawnReviewedEntityOnce(WarmongerAgrathaSpawn);
        }

        private void SpawnWarmongerChuna()
        {
            SpawnReviewedEntityOnce(WarmongerChunaSpawn);
        }

        private void SpawnWarmongerTalarii()
        {
            SpawnReviewedEntityOnce(WarmongerTalariiSpawn);
        }

        private void SpawnGrandWarmongerTargresh()
        {
            SpawnReviewedEntityOnce(GrandWarmongerTargreshSpawn);
        }

        private void SpawnAvatus()
        {
            SpawnReviewedEntityOnce(AvatusSpawn);
        }

        private void SpawnReviewedEntityOnce(DatascapeSpawnModel spawn)
        {
            if (!spawnedReviewedEntityIds.Add(spawn.EntityId))
                return;

            SpawnReviewedEntity(spawn);
        }

        private void SpawnReviewedEntity(DatascapeSpawnModel spawn)
        {
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(spawn));
            AddToMap(entity, spawn.Position);
        }

        private static EntityModel CreateEntityModel(DatascapeSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = DatascapeWorldId,
                Area        = spawn.AreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                Rx          = spawn.Rotation.X,
                Ry          = spawn.Rotation.Y,
                Rz          = spawn.Rotation.Z,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = DatascapePublicEventId,
                    Phase   = spawn.EventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = spawn.ScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = spawn.Health
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 50f
                    }
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

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Caretaker
            // broadcasts with Datascape wing handoffs, but exact wing order, cinematic timing,
            // encounter choreography, and door/trigger placement remain blocked pending proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // Avatus cinematic at final phase entry, but exact spawn choreography,
            // challenge timing, and cinematic payload remain blocked pending proof.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }

        private sealed record DatascapeSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            uint EventPhase,
            Vector3 Position,
            Vector3 Rotation,
            uint DisplayInfo,
            ushort FactionId,
            string ScriptName,
            float Health);
    }
}
