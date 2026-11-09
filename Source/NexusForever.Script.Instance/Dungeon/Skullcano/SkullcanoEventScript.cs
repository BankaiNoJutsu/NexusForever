using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Reputation;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano
{
    [ScriptFilterOwnerId(148)]
    public class SkullcanoEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint ThunderfootEntityId = 1100300049u;
        private const uint ThunderfootCreatureId = 24475u;
        private const ushort ThunderfootWorldId = 1263;
        private const ushort ThunderfootAreaId = 4793;
        private const uint ThunderfootPublicEventId = 148u;
        private const uint ThunderfootPublicEventPhase = 0u;
        private const uint ThunderfootDisplayInfo = 21318u;
        private const ushort ThunderfootFactionId = 242;
        private const string ThunderfootScriptName = "ThunderfootNormalEntityScript";

        private const uint TuggaEntityId = 1100300050u;
        private const uint TuggaCreatureId = 24493u;
        private const ushort TuggaWorldId = 1263;
        private const ushort TuggaAreaId = 1220;
        private const uint TuggaPublicEventId = 148u;
        private const uint TuggaPublicEventPhase = 0u;
        private const uint TuggaDisplayInfo = 27916u;
        private const ushort TuggaFactionId = 868;
        private const string TuggaScriptName = "StewShamanTuggaNormalEntityScript";

        private static readonly Vector3 ThunderfootPosition = new(115.26f, -923.71f, -491.56f);
        private static readonly Vector3 TuggaPosition = new(508.5147f, -978.8553f, -367.8973f);

        private readonly IGlobalQuestManager globalQuestManager;

        /// <summary>
        /// Seconds allowed for the "Timer Bonus" row (3203, "Complete Skullcano within 45 minutes for bonus points towards a medal").
        /// The objective row carries no failure time, so the limit is tracked here.
        /// </summary>
        private const double TimerBonusSeconds = 45d * 60d;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool playerDied;
        private double elapsedSeconds;

        public SkullcanoEventScript(
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
                ?? throw new InvalidOperationException("Skullcano requires a map instance.");

            playerDied = false;
            elapsedSeconds = 0d;
            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked each world tick; tracks the elapsed run time for the Timer Bonus row.
        /// </summary>
        public void Update(double lastTick)
        {
            elapsedSeconds += lastTick;
        }

        /// <summary>
        /// Invoked when a unit dies on the map; any player death fails "Deathless in the Dungeon".
        /// </summary>
        public void OnDeath(IUnitEntity entity)
        {
            if (entity is IPlayer)
                playerDied = true;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    // Tracker rows below were read from retail-era clips (see INSTANCE_SKULLCANO_TRACKER_PASS_20261109.md):
                    // Observed ordering only, no retail parity claim. "Deathless in the Dungeon" and the timer row
                    // are listed from the first phase onward (2015-09-04 live gold run; the later 2017 clip lists
                    // "Timer Bonus", the text 16042 uses for row 3203).
                    publicEvent.ActivateObjective(PublicEventObjective.DeathlessInSkullcano);
                    publicEvent.ActivateObjective(PublicEventObjective.TimerBonus);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatThunderfoot);
                    SpawnThunderfoot();
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatStewShamanTugga);
                    SpawnStewShamanTugga();
                    ActivateWipOptionalObjective(PublicEventObjective.FreeCapturedLopp);
                    break;
                case PublicEventPhase.RandomPath:
                    // WIP-guessed from LaughingWS Instances-and-more: the branch
                    // randomly routes Skullcano through the Find Chief cave path
                    // or the chasm path after Thunderfoot. Keep only objective
                    // routing here; branch door/trigger creation uses guessed
                    // placements and stays in the blocked bucket.
                    publicEvent.SetPhase(ShouldUseWipFindChiefPath()
                        ? PublicEventPhase.RandomPathFindChief
                        : PublicEventPhase.RandomPathChasm);
                    break;
                case PublicEventPhase.RandomPathFindChief:
                    // WIP-guessed branch cave-path scaffold. The exact cave door,
                    // trigger placement, Chief Kaskalak entity choreography, and
                    // route weights still need manual retail/client proof.
                    publicEvent.ActivateObjective(PublicEventObjective.FindChiefKaskalak, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.MineGoldInfusedLavaCores);
                    BroadcastFactionPair(CommunicatorMessage.DorianWalker2, CommunicatorMessage.ArtemisZin2);
                    break;
                case PublicEventPhase.RandomPathCave:
                    // WIP-guessed branch cave-path continuation. Objective routing
                    // is safe to expose, but escort movement, heat aura cadence,
                    // and cave door choreography remain blocked pending smoke.
                    publicEvent.ActivateObjective(PublicEventObjective.EscortChiefKaskalak);
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveMoltenCavernHeat);
                    break;
                case PublicEventPhase.RandomPathChasm:
                    publicEvent.ActivateObjective(PublicEventObjective.CrossTheLavaFilledChasm, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.FindAWayAcrossTheLava);
                    // The chasm-phase tracker in the 2015-09-04 gold run lists only the crossing objective, the
                    // "Avoid Laveka's attacks" challenge and Deathless: no Primal Fire Essences row, so that
                    // optional is part of the provisional random pool rather than always on.
                    ActivateWipOptionalObjective(PublicEventObjective.GatherPrimalFireEssences);
                    publicEvent.ActivateObjective(PublicEventObjective.DontGetStruckByLaveka);
                    // WIP-guessed from LaughingWS Instances-and-more: branch sends Dorian/Artemis
                    // when the chasm route starts. Exact faction pairing and timing need smoke proof.
                    BroadcastFactionPair(CommunicatorMessage.DorianWalker3, CommunicatorMessage.ArtemisZin3);
                    break;
                case PublicEventPhase.Bosun:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatBosunOctog);
                    // WIP-guessed from LaughingWS Instances-and-more: branch sends Dorian/Artemis
                    // on the Bosun handoff. Optional prisoner follow-up is gated below.
                    BroadcastFactionPair(CommunicatorMessage.DorianWalker4, CommunicatorMessage.ArtemisZin4);
                    if (ActivateWipOptionalObjective(PublicEventObjective.FreeTheRedmoonPrisoners))
                    {
                        BroadcastFactionPair(CommunicatorMessage.DorianWalker5, CommunicatorMessage.ArtemisZin5);
                        ActivateWipOptionalObjective(PublicEventObjective.RidSkullcanoOfMarauders);
                    }
                    else
                    {
                        publicEvent.ActivateObjective(PublicEventObjective.RidSkullcanoOfMarauders);
                    }
                    break;
                case PublicEventPhase.Platform:
                    publicEvent.ActivateObjective(PublicEventObjective.GatherOnThePlatform, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.GetToRedmoon:
                    publicEvent.ActivateObjective(PublicEventObjective.KillGruharAndTakeStash);
                    publicEvent.ActivateObjective(PublicEventObjective.ReachTheEldanTerraformer);
                    ActivateWipOptionalObjective(PublicEventObjective.GatherShinyGoldObjects);
                    if (ActivateWipOptionalObjective(PublicEventObjective.HackMissileConsoles))
                        BroadcastFactionPair(CommunicatorMessage.DorianWalker9, CommunicatorMessage.ArtemisZin9);
                    break;
                case PublicEventPhase.Redmoon:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatMordechaiRedmoon);
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
                case PublicEventObjective.DefeatThunderfoot:
                    publicEvent.SetPhase(PublicEventPhase.RandomPath);
                    break;
                case PublicEventObjective.FindChiefKaskalak:
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakToChiefKaskalak);
                    break;
                case PublicEventObjective.SpeakToChiefKaskalak:
                    publicEvent.SetPhase(PublicEventPhase.RandomPathCave);
                    break;
                case PublicEventObjective.EscortChiefKaskalak:
                    publicEvent.SetPhase(PublicEventPhase.Bosun);
                    break;
                case PublicEventObjective.CrossTheLavaFilledChasm:
                    publicEvent.SetPhase(PublicEventPhase.Bosun);
                    break;
                case PublicEventObjective.DefeatBosunOctog:
                    publicEvent.SetPhase(PublicEventPhase.Platform);
                    break;
                case PublicEventObjective.GatherOnThePlatform:
                    publicEvent.SetPhase(PublicEventPhase.GetToRedmoon);
                    break;
                case PublicEventObjective.KillGruharAndTakeStash:
                    publicEvent.ActivateObjective(PublicEventObjective.RedmoonTerraformer);
                    break;
                case PublicEventObjective.ReachTheEldanTerraformer:
                    publicEvent.SetPhase(PublicEventPhase.Redmoon);
                    break;
                case PublicEventObjective.DefeatMordechaiRedmoon:
                    // The engine has no script-side way to fail an objective, so the challenge rows are credited
                    // only when their condition held; a failed row simply never completes.
                    if (!playerDied)
                        publicEvent.UpdateObjective(PublicEventObjective.DeathlessInSkullcano, 1);

                    if (elapsedSeconds <= TimerBonusSeconds)
                        publicEvent.UpdateObjective(PublicEventObjective.TimerBonus, 1);

                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void SpawnThunderfoot()
        {
            // Build 16042 reviewed instance entity 1100300049 places
            // Thunderfoot in event 148 phase 0 with the normal objective-credit script.
            INonPlayerEntity thunderfoot = publicEvent.CreateEntity<INonPlayerEntity>();
            thunderfoot.Initialise(CreateThunderfootEntityModel());
            AddToMap(thunderfoot, ThunderfootPosition);
        }

        private static EntityModel CreateThunderfootEntityModel()
        {
            return CreateOpeningBossEntityModel(
                ThunderfootEntityId,
                ThunderfootCreatureId,
                ThunderfootWorldId,
                ThunderfootAreaId,
                ThunderfootPublicEventId,
                ThunderfootPublicEventPhase,
                ThunderfootPosition,
                ThunderfootDisplayInfo,
                ThunderfootFactionId,
                ThunderfootScriptName);
        }

        private void SpawnStewShamanTugga()
        {
            // Build 16042 reviewed instance entity 1100300050 places
            // Stew-Shaman Tugga in event 148 phase 0 with the normal objective-credit script.
            INonPlayerEntity tugga = publicEvent.CreateEntity<INonPlayerEntity>();
            tugga.Initialise(CreateTuggaEntityModel());
            AddToMap(tugga, TuggaPosition);
        }

        private static EntityModel CreateTuggaEntityModel()
        {
            return CreateOpeningBossEntityModel(
                TuggaEntityId,
                TuggaCreatureId,
                TuggaWorldId,
                TuggaAreaId,
                TuggaPublicEventId,
                TuggaPublicEventPhase,
                TuggaPosition,
                TuggaDisplayInfo,
                TuggaFactionId,
                TuggaScriptName);
        }

        private static EntityModel CreateOpeningBossEntityModel(
            uint entityId,
            uint creatureId,
            ushort worldId,
            ushort areaId,
            uint publicEventId,
            uint publicEventPhase,
            Vector3 position,
            uint displayInfo,
            ushort factionId,
            string scriptName)
        {
            return new EntityModel
            {
                Id          = entityId,
                Type        = EntityType.NonPlayer,
                Creature    = creatureId,
                World       = worldId,
                Area        = areaId,
                X           = position.X,
                Y           = position.Y,
                Z           = position.Z,
                DisplayInfo = displayInfo,
                Faction1    = factionId,
                Faction2    = factionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = publicEventId,
                    Phase   = publicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = scriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 35f
                    }
                }
            };
        }

        private void BroadcastFactionPair(CommunicatorMessage exileMessage, CommunicatorMessage dominionMessage)
        {
            foreach (IPlayer player in mapInstance.GetPlayers())
            {
                CommunicatorMessage message = player.Faction1 == Faction.Dominion
                    ? dominionMessage
                    : exileMessage;
                ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
                communicatorMessage?.Send(player.Session);
            }
        }

        private bool ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // Provisional random pool. The one 2015-09-04 gold run read shows a subset of these rows (Lopp,
            // Redmoon Prisoners, Gold Objects and Missile Consoles present; Primal Fire Essences absent), so a
            // random choice is kept. Weights, per-phase counts and trigger placement are NOT known.
            if (!ShouldActivateWipOptionalObjective(objective))
                return false;

            publicEvent.ActivateObjective(objective);
            return true;
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        protected virtual bool ShouldUseWipFindChiefPath()
        {
            return Random.Shared.Next(2) == 1;
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
