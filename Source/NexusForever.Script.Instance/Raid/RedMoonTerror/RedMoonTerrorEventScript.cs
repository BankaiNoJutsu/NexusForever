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

namespace NexusForever.Script.Instance.Raid.RedMoonTerror
{
    [ScriptFilterOwnerId(705)]
    public class RedMoonTerrorEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool lavekaSpawned;

        private const ushort RedMoonTerrorWorldId = 3032;
        private const string LavekaScriptName = "LavekaTheDarkHeartedEntityScript";

        private static readonly RedMoonTerrorSpawnModel LavekaSpawn = new(
            1100300056u,
            65997u,
            5996,
            new Vector3(-723.7178f, 186.8427f, -265.1872f),
            new Vector3(MathF.PI, 0f, 0f),
            38426u,
            1351,
            LavekaScriptName,
            1f,
            50f);

        public RedMoonTerrorEventScript(
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
                ?? throw new InvalidOperationException("Red Moon Terror requires a map instance.");

            lavekaSpawned = false;
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
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheBrig);
                    break;
                case PublicEventPhase.ChiefWardenLockjaw:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatChiefWardenLockjaw);
                    break;
                case PublicEventPhase.InvestigateTheShredder:
                    publicEvent.ActivateObjective(PublicEventObjective.InvestigateTheShredder);
                    break;
                case PublicEventPhase.DefeatSwabbieSkiLi:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSwabbieSkiLi);
                    break;
                case PublicEventPhase.WasteRelocationShafts:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterTheWasteRelocationShafts);
                    break;
                case PublicEventPhase.Robomination:
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.IshamelTheBloodied552);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheRobomination);
                    break;
                case PublicEventPhase.EngineeringCompartment:
                    publicEvent.ActivateObjective(PublicEventObjective.FindAWayToTheEngineeringCompartment);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.IshamelTheBloodied224);
                    break;
                case PublicEventPhase.EngiMinibosses:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatAssistantTechnicianSkooty);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatChiefEngineScrubberThrag);
                    break;
                case PublicEventPhase.TheEngineers:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheEngineers);
                    break;
                case PublicEventPhase.EnterCrewQuarters:
                    publicEvent.ActivateObjective(PublicEventObjective.MakeYourWayToTheCrewQuarters);
                    break;
                case PublicEventPhase.MordechaiRedmoon:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatMordechaiRedmoon);
                    break;
                case PublicEventPhase.DestroyAntiBoardingTurrets:
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyTheAntiBoardingTurret);
                    break;
                case PublicEventPhase.StarEater:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatStarEaterTheVoracious);
                    break;
                case PublicEventPhase.BreakBackIntoTheRedmoonTerror:
                    publicEvent.ActivateObjective(PublicEventObjective.BreakBackIntoTheRedmoonTerror);
                    break;
                case PublicEventPhase.MarauderOfficers:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatMarauderOfficers);
                    break;
                case PublicEventPhase.FindTheNavigationCore:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheNavigationCore);
                    break;
                case PublicEventPhase.Starmap:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheStarmapSimulation);
                    break;
                case PublicEventPhase.Medbay:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatBonedoctorMuburu);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatHeadshrinkerWgasa);
                    break;
                case PublicEventPhase.Morgue:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTiny);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatUntombedHorror);
                    break;
                case PublicEventPhase.Laveka:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatLavekaTheDarkHearted);
                    SpawnLaveka();
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
                case PublicEventObjective.SurviveTheBrig:
                    publicEvent.SetPhase(PublicEventPhase.ChiefWardenLockjaw);
                    break;
                case PublicEventObjective.DefeatChiefWardenLockjaw:
                    publicEvent.SetPhase(PublicEventPhase.InvestigateTheShredder);
                    break;
                case PublicEventObjective.InvestigateTheShredder:
                    publicEvent.SetPhase(PublicEventPhase.DefeatSwabbieSkiLi);
                    break;
                case PublicEventObjective.DefeatSwabbieSkiLi:
                    publicEvent.SetPhase(PublicEventPhase.WasteRelocationShafts);
                    break;
                case PublicEventObjective.EnterTheWasteRelocationShafts:
                    publicEvent.SetPhase(PublicEventPhase.Robomination);
                    break;
                case PublicEventObjective.DefeatTheRobomination:
                    publicEvent.SetPhase(PublicEventPhase.EngineeringCompartment);
                    break;
                case PublicEventObjective.FindAWayToTheEngineeringCompartment:
                    publicEvent.SetPhase(PublicEventPhase.EngiMinibosses);
                    break;
                case PublicEventObjective.DefeatAssistantTechnicianSkooty:
                case PublicEventObjective.DefeatChiefEngineScrubberThrag:
                    publicEvent.SetPhase(PublicEventPhase.TheEngineers);
                    break;
                case PublicEventObjective.DefeatTheEngineers:
                    publicEvent.SetPhase(PublicEventPhase.EnterCrewQuarters);
                    break;
                case PublicEventObjective.MakeYourWayToTheCrewQuarters:
                    publicEvent.SetPhase(PublicEventPhase.MordechaiRedmoon);
                    break;
                case PublicEventObjective.DefeatMordechaiRedmoon:
                    publicEvent.SetPhase(PublicEventPhase.DestroyAntiBoardingTurrets);
                    break;
                case PublicEventObjective.DestroyTheAntiBoardingTurret:
                    publicEvent.SetPhase(PublicEventPhase.StarEater);
                    break;
                case PublicEventObjective.DefeatStarEaterTheVoracious:
                    publicEvent.SetPhase(PublicEventPhase.BreakBackIntoTheRedmoonTerror);
                    break;
                case PublicEventObjective.BreakBackIntoTheRedmoonTerror:
                    publicEvent.SetPhase(PublicEventPhase.MarauderOfficers);
                    break;
                case PublicEventObjective.DefeatMarauderOfficers:
                    publicEvent.SetPhase(PublicEventPhase.FindTheNavigationCore);
                    break;
                case PublicEventObjective.FindTheNavigationCore:
                    publicEvent.SetPhase(PublicEventPhase.Starmap);
                    break;
                case PublicEventObjective.DefeatTheStarmapSimulation:
                    publicEvent.SetPhase(PublicEventPhase.Medbay);
                    break;
                case PublicEventObjective.DefeatBonedoctorMuburu:
                case PublicEventObjective.DefeatHeadshrinkerWgasa:
                    publicEvent.SetPhase(PublicEventPhase.Morgue);
                    break;
                case PublicEventObjective.DefeatTiny:
                case PublicEventObjective.DefeatUntombedHorror:
                    publicEvent.SetPhase(PublicEventPhase.Laveka);
                    break;
                case PublicEventObjective.DefeatLavekaTheDarkHearted:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void SpawnLaveka()
        {
            if (lavekaSpawned)
                return;

            // Build 16042 reviewed LaughingWS rows place Laveka as a static raid
            // boss row without entity_event phase binding. Exact awakening,
            // apparition challenge, and encounter choreography remain blocked.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(LavekaSpawn));
            AddToMap(entity, LavekaSpawn.Position);
            lavekaSpawned = true;
        }

        private static EntityModel CreateEntityModel(RedMoonTerrorSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = RedMoonTerrorWorldId,
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
                        Value = spawn.Level
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
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Ish'amel
            // raid callouts with Red Moon Terror phase handoffs, but exact timing,
            // encounter choreography, and door/elevator movement remain blocked pending proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private sealed record RedMoonTerrorSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            Vector3 Position,
            Vector3 Rotation,
            uint DisplayInfo,
            ushort FactionId,
            string ScriptName,
            float Health,
            float Level);
    }
}
