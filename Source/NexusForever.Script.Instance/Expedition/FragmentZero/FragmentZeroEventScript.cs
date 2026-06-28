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
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.FragmentZero
{
    [ScriptFilterOwnerId(680)]
    public class FragmentZeroEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly ICinematicFactory cinematicFactory;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool supervisorLolaSpawned;

        private const uint SearchCrewTurnstileTriggerId = 4397u;
        private const uint SearchCrewTurnstileObjectiveObjectId = 4397u;
        private const float SearchCrewTurnstileRange = 15f;
        private static readonly Vector3 SearchCrewTurnstilePosition = new(9865.48f, -765.58f, -5896.74f);

        private const uint FriendlySkeechWorldLocationId = 49225u;
        private const uint FriendlySkeechObjectiveObjectId = 7902u;
        private static readonly Vector3 FriendlySkeechTriggerPosition = new(9894.729f, -783.358f, -6032.538f);

        private const uint SearchContinuationWorldLocationId = 48711u;
        private const uint SearchContinuationObjectiveObjectId = 7903u;
        private static readonly Vector3 SearchContinuationTriggerPosition = new(9685.685f, -767.6072f, -6098.117f);

        private const float SearchChamberScriptTriggerRange = 1f;
        private const uint IncubationSearchScriptObjectId = 7892u;
        private const uint BiomaticsSearchScriptObjectId = 7919u;
        private static readonly Vector3 IncubationSearchTriggerPosition = new(9713.49f, -769.372f, -6096.29f);
        private static readonly Vector3 BiomaticsSearchTriggerPosition = new(10402.6f, -840.416f, -6171.84f);

        private const uint LifeOverseerAirlockWorldLocationId = 48682u;
        private const uint LifeOverseerAirlockObjectiveObjectId = 7893u;
        private static readonly Vector3 LifeOverseerAirlockTriggerPosition = new(9830.69f, -780.754f, -6315.97f);

        private const ushort FragmentZeroWorldId = 3180;
        private const uint SupervisorLolaEntityId = 1100300015u;
        private const uint SupervisorLolaCreatureId = 71762u;
        private const ushort SupervisorLolaAreaId = 4619;
        private const uint SupervisorLolaDisplayInfo = 32807u;
        private const ushort SupervisorLolaOutfitInfo = 0;
        private const ushort SupervisorLolaFactionId = 219;
        private static readonly Vector3 SupervisorLolaPosition = new(9873.75f, -765.9531f, -5848.58f);
        private static readonly Vector3 SupervisorLolaRotation = Vector3.Zero;

        public FragmentZeroEventScript(
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
                ?? throw new InvalidOperationException("Fragment Zero requires a map instance.");

            supervisorLolaSpawned = false;
            // Build 16042 objective 4690 is a zero-count ScriptWithoutCount row
            // with a 30-minute failure timer for Gold medal eligibility.
            publicEvent.ActivateObjective(PublicEventObjective.GoldMedalTimer);
            publicEvent.SetPhase(PublicEventPhase.ContinueTheSearchForTheMissingCrew);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.SearchForMissingCrew:
                    publicEvent.ActivateObjective(PublicEventObjective.SearchForMissingCrew, mapInstance.PlayerCount);
                    SpawnWipGuessedTurnstileTrigger(
                        SearchCrewTurnstileTriggerId,
                        SearchCrewTurnstileRange,
                        SearchCrewTurnstileObjectiveObjectId,
                        SearchCrewTurnstilePosition);
                    break;
                case PublicEventPhase.FollowTheFriendlySkeech:
                    publicEvent.ActivateObjective(PublicEventObjective.FollowTheFriendlySkeech, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.SupervisorLola);
                    SpawnWipGuessedWorldLocationTrigger(
                        FriendlySkeechWorldLocationId,
                        FriendlySkeechObjectiveObjectId,
                        FriendlySkeechTriggerPosition);
                    break;
                case PublicEventPhase.SurviveTheSkeechAmbush:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheSkeechAmbush);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.SupervisorLolax);
                    break;
                case PublicEventPhase.ContinueTheSearchForTheMissingCrew:
                    SpawnReviewedSupervisorLola();
                    publicEvent.ActivateObjective(PublicEventObjective.ContinueTheSearchForTheMissingCrew, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.LocateTheShipsBlackBox);
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateSkeech);
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea);
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea2);
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea3);
                    publicEvent.ActivateObjective(PublicEventObjective.DiscoverTheTwoLostRecordings, 2u);
                    publicEvent.ActivateObjective(PublicEventObjective.FindOhmnasHiddenRecording);
                    publicEvent.ActivateObjective(PublicEventObjective.FindLucentsHiddenRecording);
                    SpawnWipGuessedWorldLocationTrigger(
                        SearchContinuationWorldLocationId,
                        SearchContinuationObjectiveObjectId,
                        SearchContinuationTriggerPosition);
                    QueueWipGuessedCinematic<IFragmentZeroFirstWarning>();
                    break;
                case PublicEventPhase.SearchForCrewmateJo:
                    publicEvent.ActivateObjective(PublicEventObjective.SearchInsideTheIncubationComplexForJo);
                    publicEvent.ActivateObjective(PublicEventObjective.SmashXenobiteEggsInsideTheIncubationComplex);
                    SpawnWipGuessedScriptObjectiveTrigger(
                        IncubationSearchScriptObjectId,
                        SearchChamberScriptTriggerRange,
                        IncubationSearchTriggerPosition);
                    break;
                case PublicEventPhase.DefeatPrototypes:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPrototypeAlphansideTheIncubationComplex);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPrototypeBeta);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatPrototypeDelta);
                    break;
                case PublicEventPhase.SearchJoscorpse:
                    publicEvent.ActivateObjective(PublicEventObjective.SearchJoInsideTheIncubationComplex);
                    break;
                case PublicEventPhase.ReturnToTheAirlockOfTheIncubationComplex:
                    publicEvent.ActivateObjective(PublicEventObjective.ReturnToTheAirlockOfTheIncubationComplex, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.ContinueTheSearchForTheMissingCrewLifeOverseer:
                    publicEvent.ActivateObjective(PublicEventObjective.ContinueTheSearchForTheMissingCrewLifeOverseer, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.CollectCargoCrate);
                    publicEvent.ActivateObjective(PublicEventObjective.CollectCargoCrate2);
                    break;
                case PublicEventPhase.SearchInsideTheBiomaticsChamberForSyrus:
                    publicEvent.ActivateObjective(PublicEventObjective.SearchInsideTheBiomaticsChamberForSyrus);
                    publicEvent.ActivateObjective(PublicEventObjective.DeactivateAutomatedDefences);
                    SpawnWipGuessedScriptObjectiveTrigger(
                        BiomaticsSearchScriptObjectId,
                        SearchChamberScriptTriggerRange,
                        BiomaticsSearchTriggerPosition);
                    break;
                case PublicEventPhase.DefeatProjectMatron:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatProjectMatron);
                    break;
                case PublicEventPhase.SearchCrewmateSyrusCorpse:
                    publicEvent.ActivateObjective(PublicEventObjective.SearchCrewmateSyrusCorpse);
                    break;
                case PublicEventPhase.ReturnToTheAirlockOfTheBiomaticsChamber:
                    publicEvent.ActivateObjective(PublicEventObjective.ReturnToTheAirlockOfTheBiomaticsChamber, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.ContinueTheSearchForHugo:
                    publicEvent.ActivateObjective(PublicEventObjective.ContinueTheSearchForHugo, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.SearchForHugo:
                    publicEvent.ActivateObjective(PublicEventObjective.SearchForHugo, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.DefeatTheLifeOverseer:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheLifeOverseer);
                    break;
                case PublicEventPhase.ReturnToTheAirlockOfTheLifeOverseersCreche:
                    publicEvent.ActivateObjective(PublicEventObjective.ReturnToTheAirlockOfTheLifeOverseersCreche, mapInstance.PlayerCount);
                    SpawnWipGuessedWorldLocationTrigger(
                        LifeOverseerAirlockWorldLocationId,
                        LifeOverseerAirlockObjectiveObjectId,
                        LifeOverseerAirlockTriggerPosition);
                    break;
                case PublicEventPhase.LocateHugo:
                    publicEvent.ActivateObjective(PublicEventObjective.LocateHugo);
                    break;
                case PublicEventPhase.WaitForHugo:
                    publicEvent.ActivateObjective(PublicEventObjective.WaitForHugo);
                    // Build 16042 objective 4655 is a zero-count ScriptWithoutMax
                    // wait row. Credit it directly until exact Hugo timing is proven.
                    publicEvent.UpdateObjective(PublicEventObjective.WaitForHugo, 0);
                    break;
                case PublicEventPhase.SeeIfHugoHasAWayOutOfThisMess:
                    publicEvent.ActivateObjective(PublicEventObjective.SeeIfHugoHasAWayOutOfThisMess);
                    break;
                case PublicEventPhase.StayCloseToHugo:
                    publicEvent.ActivateObjective(PublicEventObjective.StayCloseToHugo);
                    // Build 16042 objective 4447 is a zero-count ScriptWithoutMax
                    // stay-close row bound to Hugo's final target group.
                    publicEvent.UpdateObjective(PublicEventObjective.StayCloseToHugo, 0);
                    break;
                case PublicEventPhase.SpeakWithHugo:
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithHugo);
                    break;
            }
        }

        private void SpawnWipGuessedTurnstileTrigger(uint triggerId, float range, uint objectId, Vector3 position)
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch marks the
            // early Fragment Zero trigger range/coordinates as needing proof. Keep it
            // scoped to objective progress until retail trigger rows or smoke logs
            // confirm the exact placement.
            ITurnstileGridTriggerEntity triggerEntity = publicEvent.CreateEntity<ITurnstileGridTriggerEntity>();
            triggerEntity.Initialise(triggerId, range, objectId);
            AddToMap(triggerEntity, position);
        }

        private void SpawnWipGuessedWorldLocationTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            // WIP-guessed from LaughingWS Instances-and-more: these Fragment Zero
            // world-location triggers are branch-mapped, but door/cinematic timing and
            // entity cleanup remain blocked pending retail smoke proof.
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(worldLocationId, objectId);
            AddToMap(triggerEntity, position);
        }

        private void SpawnWipGuessedScriptObjectiveTrigger(uint objectId, float range, Vector3 position)
        {
            // Build 16042 maps the Fragment Zero search objectives to zero-count
            // Script rows at one-unit WorldLocation2 anchors. Use a plain grid
            // trigger so the owner-bound script credits the Script objective,
            // not ParticipantsInTriggerVolume.
            IGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IGridTriggerEntity>();
            triggerEntity.Initialise(objectId, range);
            AddToMap(triggerEntity, position);
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

        private void SpawnReviewedSupervisorLola()
        {
            if (supervisorLolaSpawned)
                return;

            // Reviewed Fragment Zero instance rows place Freight Supervisor Lola as
            // a static NPC without an entity_event row; exact dialog/choreography
            // timing remains blocked pending client smoke.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateSupervisorLolaModel());
            AddToMap(entity, SupervisorLolaPosition);
            supervisorLolaSpawned = true;
        }

        private static EntityModel CreateSupervisorLolaModel()
        {
            return new EntityModel
            {
                Id          = SupervisorLolaEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = SupervisorLolaCreatureId,
                World       = FragmentZeroWorldId,
                Area        = SupervisorLolaAreaId,
                X           = SupervisorLolaPosition.X,
                Y           = SupervisorLolaPosition.Y,
                Z           = SupervisorLolaPosition.Z,
                Rx          = SupervisorLolaRotation.X,
                Ry          = SupervisorLolaRotation.Y,
                Rz          = SupervisorLolaRotation.Z,
                DisplayInfo = SupervisorLolaDisplayInfo,
                OutfitInfo  = SupervisorLolaOutfitInfo,
                Faction1    = SupervisorLolaFactionId,
                Faction2    = SupervisorLolaFactionId,
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = 1f
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = 0f
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Unknown22,
                        Value = 0f
                    }
                }
            };
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
                case PublicEventObjective.FollowTheFriendlySkeech:
                    publicEvent.SetPhase(PublicEventPhase.SurviveTheSkeechAmbush);
                    break;
                case PublicEventObjective.SurviveTheSkeechAmbush:
                    publicEvent.SetPhase(PublicEventPhase.ContinueTheSearchForTheMissingCrew);
                    break;
                case PublicEventObjective.ContinueTheSearchForTheMissingCrew:
                    publicEvent.SetPhase(PublicEventPhase.SearchForCrewmateJo);
                    break;
                case PublicEventObjective.SearchInsideTheIncubationComplexForJo:
                    publicEvent.SetPhase(PublicEventPhase.DefeatPrototypes);
                    break;
                case PublicEventObjective.DefeatPrototypeAlphansideTheIncubationComplex:
                case PublicEventObjective.DefeatPrototypeBeta:
                case PublicEventObjective.DefeatPrototypeDelta:
                    publicEvent.SetPhase(PublicEventPhase.SearchJoscorpse);
                    break;
                case PublicEventObjective.SearchJoInsideTheIncubationComplex:
                    publicEvent.SetPhase(PublicEventPhase.ReturnToTheAirlockOfTheIncubationComplex);
                    break;
                case PublicEventObjective.ReturnToTheAirlockOfTheIncubationComplex:
                    publicEvent.SetPhase(PublicEventPhase.ContinueTheSearchForTheMissingCrewLifeOverseer);
                    break;
                case PublicEventObjective.ContinueTheSearchForTheMissingCrewLifeOverseer:
                    publicEvent.SetPhase(PublicEventPhase.SearchInsideTheBiomaticsChamberForSyrus);
                    break;
                case PublicEventObjective.SearchInsideTheBiomaticsChamberForSyrus:
                    publicEvent.SetPhase(PublicEventPhase.DefeatProjectMatron);
                    break;
                case PublicEventObjective.DefeatProjectMatron:
                    publicEvent.SetPhase(PublicEventPhase.SearchCrewmateSyrusCorpse);
                    break;
                case PublicEventObjective.SearchCrewmateSyrusCorpse:
                    publicEvent.SetPhase(PublicEventPhase.ReturnToTheAirlockOfTheBiomaticsChamber);
                    break;
                case PublicEventObjective.ReturnToTheAirlockOfTheBiomaticsChamber:
                    publicEvent.SetPhase(PublicEventPhase.SearchForHugo);
                    break;
                case PublicEventObjective.SearchForHugo:
                    publicEvent.SetPhase(PublicEventPhase.DefeatTheLifeOverseer);
                    break;
                case PublicEventObjective.DefeatTheLifeOverseer:
                    publicEvent.SetPhase(PublicEventPhase.ReturnToTheAirlockOfTheLifeOverseersCreche);
                    break;
                case PublicEventObjective.ReturnToTheAirlockOfTheLifeOverseersCreche:
                    publicEvent.SetPhase(PublicEventPhase.LocateHugo);
                    break;
                case PublicEventObjective.LocateHugo:
                    publicEvent.SetPhase(PublicEventPhase.WaitForHugo);
                    break;
                case PublicEventObjective.WaitForHugo:
                    publicEvent.SetPhase(PublicEventPhase.SeeIfHugoHasAWayOutOfThisMess);
                    break;
                case PublicEventObjective.SeeIfHugoHasAWayOutOfThisMess:
                    publicEvent.SetPhase(PublicEventPhase.StayCloseToHugo);
                    break;
                case PublicEventObjective.StayCloseToHugo:
                    publicEvent.SetPhase(PublicEventPhase.SpeakWithHugo);
                    break;
                case PublicEventObjective.SpeakWithHugo:
                    publicEvent.UpdateObjective(PublicEventObjective.GoldMedalTimer, 0);
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a cinematic for <see cref="IPlayer"/> has finished.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            if (publicEvent.Phase != (uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew)
                return;

            SendWipCommunicatorMessage(player, CommunicatorMessage.CaptainHugo1);
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Fragment Zero
            // communicator ids with early phase/cinematic handoffs, but exact trigger placement,
            // cinematic id gating, and entity cleanup remain blocked pending retail smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void SendWipCommunicatorMessage(IPlayer player, CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. Keep this single-player cinematic
            // follow-up scoped to the branch continuation phase until the exact cinematic id is proven.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            communicatorMessage?.Send(player.Session);
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // phase cinematic for all players, but supplies only an abstract hook; the
            // concrete class is an immediate placeholder until retail payload data is mapped.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }
    }
}
