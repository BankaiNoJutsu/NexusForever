using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.ProtogamesAcademy
{
    [ScriptFilterOwnerId(667)]
    public class ProtogamesAcademyEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private const uint InvulnotronGatherWorldLocationId = 48842u;
        private const uint InvulnotronGatherObjectiveObjectId = 7932u;
        private static readonly Vector3 InvulnotronGatherPosition = new(-24400.1f, -974.668f, -28977.1f);

        private const uint GromkaGatherWorldLocationId = 48843u;
        private const uint GromkaGatherObjectiveObjectId = 7933u;
        private static readonly Vector3 GromkaGatherPosition = new(-24504.9f, -974.749f, -28857.5f);

        private const uint TeleporterWorldLocationId = 50164u;
        private const uint TeleporterObjectiveObjectId = 7936u;
        private static readonly Vector3 TeleporterPosition = new(-24400.19f, -974.668f, -28977.15f);

        private const uint GatherWorldLocationId = 48846u;
        private const uint GatherObjectiveObjectId = 7934u;
        private static readonly Vector3 GatherPosition = new(-19804.34f, -945.5437f, -29483.94f);

        public ProtogamesAcademyEventScript(
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
                ?? throw new InvalidOperationException("Protogames Academy requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.InitiateProtogamesAcademy);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.InitiateProtogamesAcademy:
                    publicEvent.ActivateObjective(PublicEventObjective.InitiateProtogamesAcademy);
                    break;
                case PublicEventPhase.GatherInvulnotron:
                    publicEvent.ActivateObjective(PublicEventObjective.GatherInvulnotron, mapInstance.PlayerCount);
                    SpawnWipGuessedWorldLocationTrigger(
                        InvulnotronGatherWorldLocationId,
                        InvulnotronGatherObjectiveObjectId,
                        InvulnotronGatherPosition);
                    break;
                case PublicEventPhase.DefeatInvulnotron:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatInvulnotron);
                    break;
                case PublicEventPhase.GatherGromka:
                    publicEvent.ActivateObjective(PublicEventObjective.GatherGromka, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar1);
                    SpawnWipGuessedWorldLocationTrigger(
                        GromkaGatherWorldLocationId,
                        GromkaGatherObjectiveObjectId,
                        GromkaGatherPosition);
                    break;
                case PublicEventPhase.DefeatGromka:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGromka);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar2);
                    break;
                case PublicEventPhase.GatherIrukiBoldbeard:
                    publicEvent.ActivateObjective(PublicEventObjective.GatherIrukiBoldbeard, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar3);
                    SpawnWipGuessedWorldLocationTrigger(
                        InvulnotronGatherWorldLocationId,
                        InvulnotronGatherObjectiveObjectId,
                        InvulnotronGatherPosition);
                    break;
                case PublicEventPhase.DefeatIrukiBoldbeard:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatIrukiBoldbeard);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar4);
                    break;
                case PublicEventPhase.TeleporterToTheNextEvent:
                    publicEvent.ActivateObjective(PublicEventObjective.TeleporterToTheNextEvent1);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar5);
                    SpawnWipGuessedWorldLocationTrigger(
                        TeleporterWorldLocationId,
                        TeleporterObjectiveObjectId,
                        TeleporterPosition);
                    break;
                case PublicEventPhase.DefeatSeekNSlaughter:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSeekNSlaughter);
                    break;
                case PublicEventPhase.Gather:
                    publicEvent.ActivateObjective(PublicEventObjective.Gather, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar6);
                    SpawnWipGuessedWorldLocationTrigger(
                        GatherWorldLocationId,
                        GatherObjectiveObjectId,
                        GatherPosition);
                    break;
                case PublicEventPhase.DefeatIceboxMk2:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatIceboxMk2);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar7);
                    break;
                case PublicEventPhase.Gather2:
                    publicEvent.ActivateObjective(PublicEventObjective.Gather2, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar8);
                    SpawnWipGuessedWorldLocationTrigger(
                        GatherWorldLocationId,
                        GatherObjectiveObjectId,
                        GatherPosition);
                    break;
                case PublicEventPhase.DefeatSuperInvulnotron:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSuperInvulnotron);
                    break;
                case PublicEventPhase.GoToLastEvent:
                    publicEvent.ActivateObjective(PublicEventObjective.GoToTheLastEvent, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar9);
                    break;
                case PublicEventPhase.MeetWithPhineasARotostar:
                    publicEvent.ActivateObjective(PublicEventObjective.MeetWithPhineasARotostar);
                    break;
                case PublicEventPhase.DefeatWrathbone:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWrathbone);
                    break;
            }
        }

        private void SpawnWipGuessedWorldLocationTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch maps these
            // Protogames Academy gather/teleporter trigger ids and positions, but exact
            // trigger rows, cleanup timing, and platform/launcher choreography still need
            // retail smoke proof.
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(worldLocationId, objectId);
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

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.InitiateProtogamesAcademy:
                    publicEvent.SetPhase(PublicEventPhase.GatherInvulnotron);
                    break;
                case PublicEventObjective.GatherInvulnotron:
                    publicEvent.SetPhase(PublicEventPhase.DefeatInvulnotron);
                    break;
                case PublicEventObjective.DefeatInvulnotron:
                    publicEvent.SetPhase(PublicEventPhase.GatherGromka);
                    break;
                case PublicEventObjective.GatherGromka:
                    publicEvent.SetPhase(PublicEventPhase.DefeatGromka);
                    break;
                case PublicEventObjective.DefeatGromka:
                    publicEvent.SetPhase(PublicEventPhase.GatherIrukiBoldbeard);
                    break;
                case PublicEventObjective.GatherIrukiBoldbeard:
                    publicEvent.SetPhase(PublicEventPhase.DefeatIrukiBoldbeard);
                    break;
                case PublicEventObjective.DefeatIrukiBoldbeard:
                    publicEvent.SetPhase(PublicEventPhase.TeleporterToTheNextEvent);
                    break;
                case PublicEventObjective.TeleporterToTheNextEvent1:
                    publicEvent.SetPhase(PublicEventPhase.DefeatSeekNSlaughter);
                    break;
                case PublicEventObjective.DefeatSeekNSlaughter:
                    publicEvent.SetPhase(PublicEventPhase.Gather);
                    break;
                case PublicEventObjective.Gather:
                    publicEvent.SetPhase(PublicEventPhase.DefeatIceboxMk2);
                    break;
                case PublicEventObjective.DefeatIceboxMk2:
                    publicEvent.SetPhase(PublicEventPhase.Gather2);
                    break;
                case PublicEventObjective.Gather2:
                    publicEvent.SetPhase(PublicEventPhase.DefeatSuperInvulnotron);
                    break;
                case PublicEventObjective.DefeatSuperInvulnotron:
                    publicEvent.SetPhase(PublicEventPhase.GoToLastEvent);
                    break;
                case PublicEventObjective.GoToTheLastEvent:
                    publicEvent.SetPhase(PublicEventPhase.MeetWithPhineasARotostar);
                    break;
                case PublicEventObjective.MeetWithPhineasARotostar:
                    publicEvent.SetPhase(PublicEventPhase.DefeatWrathbone);
                    break;
                case PublicEventObjective.DefeatWrathbone:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Phineas
            // communicator ids with Protogames Academy phase handoffs, but exact trigger
            // placement, timing, and entity cleanup remain blocked pending retail smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }
    }
}
