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

namespace NexusForever.Script.Instance.Raid.GeneticArchives
{
    [ScriptFilterOwnerId(159)]
    public class GeneticArchivesEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ICinematicFactory cinematicFactory;

        private static readonly GeneticArchivesSpawnModel ExperimentX89Spawn = new(
            1100300052u,
            49198u,
            new Vector3(-1147.055f, -111.3793f, -520.5323f),
            -2f,
            27899u,
            "ExperimentX-89EntityScript");

        private static readonly GeneticArchivesSpawnModel KuralakTheDefilerSpawn = new(
            1100300053u,
            52969u,
            new Vector3(169.4765f, -110.4199f, -489.5547f),
            2.093871f,
            30276u,
            "KuralakTheDefilerEntityScript");

        private static readonly GeneticArchivesSpawnModel KuralakPillarSpawn = new(
            1100300054u,
            53031u,
            new Vector3(133.965f, -111.45f, -505.34f),
            0f,
            27557u,
            "KuralakPillarEntityScript");

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool reviewedOpeningPlacementsSpawned;

        public GeneticArchivesEventScript(
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
                ?? throw new InvalidOperationException("Genetic Archives requires a map instance.");

            reviewedOpeningPlacementsSpawned = false;
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
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatExperimentX89);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatKuralakTheDefiler);
                    SpawnReviewedOpeningPlacements();
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.TheDreadphageOhmna1);
                    break;
                case PublicEventPhase.GetToNextLevel:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheFetidMiscreation);
                    break;
                case PublicEventPhase.SecondFloor:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPhagetechGuardianC148);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPhagetechGuardianC432);
                    break;
                case PublicEventPhase.ArchiveDefenseSystem:
                    publicEvent.ActivateObjective(PublicEventObjective.GetThroughTheArchiveDefenseSystem);
                    publicEvent.ActivateObjective(PublicEventObjective.DeathFromAbove);
                    break;
                case PublicEventPhase.PhagebornConvergence:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatThePhagebornConvergence);
                    break;
                case PublicEventPhase.Minibosses:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningPiston);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningBattery);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningGear);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheMalfunctioningDynamo);
                    break;
                case PublicEventPhase.Ohmna:
                    QueueWipGuessedCinematic<IGeneticArchivesOpenOhmnaDoor>();
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatDreadphageOhmna);
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
                case PublicEventObjective.DefeatExperimentX89:
                case PublicEventObjective.DefeatKuralakTheDefiler:
                    publicEvent.SetPhase(PublicEventPhase.GetToNextLevel);
                    break;
                case PublicEventObjective.DefeatTheFetidMiscreation:
                    publicEvent.SetPhase(PublicEventPhase.SecondFloor);
                    break;
                case PublicEventObjective.DefeatPhagetechGuardianC148:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatConstructsInTheCentrifuge);
                    break;
                case PublicEventObjective.DefeatConstructsInTheCentrifuge:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPhageMaw);
                    break;
                case PublicEventObjective.DefeatPhagetechGuardianC432:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheParagonsOfSymbiosis);
                    break;
                case PublicEventObjective.DefeatTheParagonsOfSymbiosis:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatThePhagetechPrototypes);
                    break;
                case PublicEventObjective.DefeatPhageMaw:
                case PublicEventObjective.DefeatThePhagetechPrototypes:
                    publicEvent.SetPhase(PublicEventPhase.ArchiveDefenseSystem);
                    break;
                case PublicEventObjective.GetThroughTheArchiveDefenseSystem:
                    publicEvent.SetPhase(PublicEventPhase.PhagebornConvergence);
                    break;
                case PublicEventObjective.DefeatThePhagebornConvergence:
                    publicEvent.SetPhase(PublicEventPhase.Minibosses);
                    break;
                case PublicEventObjective.DefeatTheMalfunctioningGear:
                case PublicEventObjective.DefeatTheMalfunctioningPiston:
                case PublicEventObjective.DefeatTheMalfunctioningDynamo:
                case PublicEventObjective.DefeatTheMalfunctioningBattery:
                    publicEvent.SetPhase(PublicEventPhase.Ohmna);
                    break;
                case PublicEventObjective.DefeatDreadphageOhmna:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void SpawnReviewedOpeningPlacements()
        {
            if (reviewedOpeningPlacementsSpawned)
                return;

            SpawnReviewedStaticPlacement(ExperimentX89Spawn);
            SpawnReviewedStaticPlacement(KuralakTheDefilerSpawn);
            SpawnReviewedStaticPlacement(KuralakPillarSpawn);
            reviewedOpeningPlacementsSpawned = true;
        }

        private void SpawnReviewedStaticPlacement(GeneticArchivesSpawnModel spawn)
        {
            // Reviewed build 16042 rows place the opening Genetic Archives NPCs
            // without entity_event phase bindings. Scripts/stat overrides are kept,
            // while exact raid mechanics and pillar behavior remain blocked.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(spawn));
            AddToMap(entity, spawn.Position);
        }

        private static EntityModel CreateEntityModel(GeneticArchivesSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = 1462,
                Area        = 0,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                Rx          = spawn.RotationX,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = 1209,
                Faction2    = 1209,
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
                        Value = 1f
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

        /// <summary>
        /// Invoked when a cinematic for <see cref="IPlayer"/> has finished.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            switch ((PublicEventPhase)publicEvent.Phase)
            {
                case PublicEventPhase.Enter:
                    SendWipCommunicatorMessage(player, CommunicatorMessage.TheDreadphageOhmna1);
                    break;
                case PublicEventPhase.Ohmna:
                    SendWipCommunicatorMessage(player, CommunicatorMessage.TheDreadphageOhmna8);
                    break;
            }
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch maps these Ohmna
            // communicator ids to phase/cinematic handoffs, but exact cinematic id gating,
            // door/elevator choreography, and encounter timing remain blocked pending proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void SendWipCommunicatorMessage(IPlayer player, CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more; keep this phase-gated until
            // the exact retail cinematic trigger and replay timing are proven.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            communicatorMessage?.Send(player.Session);
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // final Ohmna-door cinematic, but exact door/elevator choreography and
            // real cinematic payload remain blocked pending proof.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }

        private sealed record GeneticArchivesSpawnModel(
            uint EntityId,
            uint CreatureId,
            Vector3 Position,
            float RotationX,
            uint DisplayInfo,
            string ScriptName);
    }
}
