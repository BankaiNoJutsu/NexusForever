using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.EventInstances.ShadesEve
{
    [ScriptFilterOwnerId(597)]
    public class ShadesEveMainEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint ChoosePathVoteId = 64u;
        private const uint DefaultVoteChoice = 1u;
        private const ushort ShadesEveWorldId = 3044;
        private const uint ShadesEvePublicEventId = 597u;

        private const uint EttyWindsenEntityId = 1100300057u;
        private const uint EttyWindsenCreatureId = 62747u;
        private const ushort EttyWindsenAreaId = 0;
        private const uint EttyWindsenPhase = 1u;
        private const uint EttyWindsenDisplayInfo = 23053u;
        private const ushort EttyWindsenOutfitInfo = 7913;
        private const ushort EttyWindsenFactionId = 219;

        private const uint FountainGatheringCircleEntityId = 1100300058u;
        private const uint FountainGatheringCircleCreatureId = 64820u;
        private const ushort FountainGatheringCircleAreaId = 0;
        private const uint FountainGatheringCirclePhase = 0u;
        private const uint FountainGatheringCircleDisplayInfo = 30327u;
        private const ushort FountainGatheringCircleFactionId = 219;

        private static readonly Vector3 EttyWindsenPosition = new(340.9081f, -869.39844f, -256.90118f);
        private static readonly Vector3 EttyWindsenRotation = new(2.9192612f, 0f, 0f);
        private static readonly Vector3 FountainGatheringCirclePosition = new(333.41528f, -871.36365f, -242.43497f);
        private static readonly Vector3 FountainGatheringCircleRotation = new(-3.1415925f, 0f, 0f);

        private readonly IGlobalQuestManager globalQuestManager;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool ettyWindsenSpawned;
        private bool fountainGatheringCircleSpawned;

        public ShadesEveMainEventScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Shades Eve requires a map instance.");

            ettyWindsenSpawned = false;
            fountainGatheringCircleSpawned = false;
            publicEvent.SetPhase(PublicEventPhase.FindTheFountain);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.FindTheFountain:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheFountain, mapInstance.PlayerCount);
                    SpawnReviewedFountainGatheringCircle();
                    break;
                case PublicEventPhase.SpeakWithEttyWindsen:
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.TheAngel5);
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithEttyWindsen);
                    SpawnReviewedEttyWindsen();
                    break;
                case PublicEventPhase.SpeakWithTheMayor:
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithTheMayor);
                    break;
                case PublicEventPhase.TalkWithTheLocals:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkWithTheLocals);
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
                case PublicEventObjective.FindTheFountain:
                    publicEvent.SetPhase(PublicEventPhase.SpeakWithEttyWindsen);
                    break;
                case PublicEventObjective.SpeakWithEttyWindsen:
                    publicEvent.SetPhase(PublicEventPhase.SpeakWithTheMayor);
                    break;
                case PublicEventObjective.SpeakWithTheMayor:
                    publicEvent.SetPhase(PublicEventPhase.TalkWithTheLocals);
                    break;
                case PublicEventObjective.TalkWithTheLocals:
                    publicEvent.StartVote(PublicEventTeam.PublicTeam, ChoosePathVoteId, DefaultVoteChoice);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a cinematic for <see cref="IPlayer"/> has finished.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            if (publicEvent.Phase != (uint)PublicEventPhase.FindTheFountain)
                return;

            SendWipCommunicatorMessage(player, CommunicatorMessage.TheAngel1);
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Shade's Eve
            // communicator ids with phase/cinematic handoffs, but exact cinematic id gating,
            // town-gate timing, and vote follow-up remain blocked pending retail smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void SpawnReviewedFountainGatheringCircle()
        {
            if (fountainGatheringCircleSpawned)
                return;

            // Build 16042 reviewed LaughingWS instance entity 1100300058 binds
            // this Simple gathering circle to event 597 phase 0. Objective
            // gather cleanup remains blocked pending client smoke.
            ISimpleEntity entity = publicEvent.CreateEntity<ISimpleEntity>();
            entity.Initialise(CreateFountainGatheringCircleEntityModel());
            AddToMap(entity, FountainGatheringCirclePosition);
            fountainGatheringCircleSpawned = true;
        }

        private static EntityModel CreateFountainGatheringCircleEntityModel()
        {
            return new EntityModel
            {
                Id          = FountainGatheringCircleEntityId,
                Type        = EntityType.Simple,
                Creature    = FountainGatheringCircleCreatureId,
                World       = ShadesEveWorldId,
                Area        = FountainGatheringCircleAreaId,
                X           = FountainGatheringCirclePosition.X,
                Y           = FountainGatheringCirclePosition.Y,
                Z           = FountainGatheringCirclePosition.Z,
                Rx          = FountainGatheringCircleRotation.X,
                Ry          = FountainGatheringCircleRotation.Y,
                Rz          = FountainGatheringCircleRotation.Z,
                DisplayInfo = FountainGatheringCircleDisplayInfo,
                Faction1    = FountainGatheringCircleFactionId,
                Faction2    = FountainGatheringCircleFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ShadesEvePublicEventId,
                    Phase   = FountainGatheringCirclePhase
                }
            };
        }

        private void SpawnReviewedEttyWindsen()
        {
            if (ettyWindsenSpawned)
                return;

            // Build 16042 reviewed LaughingWS instance entity 1100300057 places
            // Etty Windsen in event 597 phase 1 with source health/level
            // overrides. Exact TalkTo/cinematic timing remains smoke-blocked.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEttyWindsenEntityModel());
            AddToMap(entity, EttyWindsenPosition);
            ettyWindsenSpawned = true;
        }

        private static EntityModel CreateEttyWindsenEntityModel()
        {
            return new EntityModel
            {
                Id          = EttyWindsenEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = EttyWindsenCreatureId,
                World       = ShadesEveWorldId,
                Area        = EttyWindsenAreaId,
                X           = EttyWindsenPosition.X,
                Y           = EttyWindsenPosition.Y,
                Z           = EttyWindsenPosition.Z,
                Rx          = EttyWindsenRotation.X,
                Ry          = EttyWindsenRotation.Y,
                Rz          = EttyWindsenRotation.Z,
                DisplayInfo = EttyWindsenDisplayInfo,
                OutfitInfo  = EttyWindsenOutfitInfo,
                Faction1    = EttyWindsenFactionId,
                Faction2    = EttyWindsenFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ShadesEvePublicEventId,
                    Phase   = EttyWindsenPhase
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

        private void SendWipCommunicatorMessage(IPlayer player, CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more; keep the cinematic follow-up
            // phase-gated until the exact retail cinematic id and replay timing are proven.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            communicatorMessage?.Send(player.Session);
        }
    }
}
