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

namespace NexusForever.Script.Instance.Dungeon.ProtogamesAcademy
{
    [ScriptFilterOwnerId(667)]
    public class ProtogamesAcademyEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool invulnotronSpawned;
        private bool gromkaSpawned;
        private bool irukiBoldbeardSpawned;
        private bool seekNSlaughterSpawned;
        private bool iceboxMk2Spawned;
        private bool superInvulnotronSpawned;
        private bool wrathboneSpawned;

        private const ushort ProtogamesAcademyWorldId = 3173;
        private const uint ProtogamesAcademyPublicEventId = 667u;

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

        private const uint LastEventTeleporterWorldLocationId = 48846u;
        private const uint LastEventTeleporterObjectiveObjectId = 7939u;
        private static readonly Vector3 LastEventTeleporterPosition = new(-19804.3f, -945.544f, -29483.9f);

        private const uint PhineasMeetWorldLocationId = 48850u;
        private const uint PhineasMeetObjectiveObjectId = 7940u;
        private static readonly Vector3 PhineasMeetPosition = new(-15760.7f, -904.839f, -29638.5f);

        private static readonly ProtogamesAcademySpawnModel InvulnotronSpawn = new(
            1100300031u,
            67475u,
            4651,
            3u,
            new Vector3(-24330.6f, -974.0855f, -28917.8f),
            Vector3.Zero,
            24783u,
            1322,
            "InvulnotronEntityScript",
            1f,
            10f);

        private static readonly ProtogamesAcademySpawnModel GromkaSpawn = new(
            1100300033u,
            67594u,
            4507,
            5u,
            new Vector3(-24424.6f, -974.0912f, -28788.1f),
            Vector3.Zero,
            28878u,
            1322,
            "GromkaEntityScript",
            1f,
            10f);

        private static readonly ProtogamesAcademySpawnModel IrukiBoldbeardSpawn = new(
            1100300034u,
            67663u,
            4507,
            7u,
            new Vector3(-24360.97f, -972.6974f, -28943.9f),
            Vector3.Zero,
            32741u,
            1322,
            "IrukiBoldbeardEntityScript",
            1f,
            10f);

        private static readonly ProtogamesAcademySpawnModel SeekNSlaughterSpawn = new(
            1100300035u,
            67668u,
            4507,
            11u,
            new Vector3(-19777.14f, -946.8414f, -29457.42f),
            Vector3.Zero,
            21309u,
            233,
            "SeekNSlaughterEntityScript",
            1f,
            10f);

        private static readonly ProtogamesAcademySpawnModel IceboxMk2Spawn = new(
            1100300036u,
            67757u,
            4507,
            13u,
            new Vector3(-19777.14f, -946.8414f, -29457.42f),
            Vector3.Zero,
            21323u,
            233,
            "IceboxMk2EntityScript",
            1f,
            10f);

        private static readonly ProtogamesAcademySpawnModel SuperInvulnotronSpawn = new(
            1100300100u,
            68096u,
            4507,
            15u,
            new Vector3(-19779f, -946f, -29462f),
            Vector3.Zero,
            24783u,
            1322,
            "SuperInvulnotronEntityScript",
            1f,
            10f);

        private static readonly ProtogamesAcademySpawnModel WrathboneSpawn = new(
            1100300101u,
            67944u,
            4508,
            18u,
            new Vector3(-15788f, -904f, -29615f),
            Vector3.Zero,
            26004u,
            1322,
            "WrathboneEntityScript",
            1f,
            10f);

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

            invulnotronSpawned = false;
            gromkaSpawned = false;
            irukiBoldbeardSpawned = false;
            seekNSlaughterSpawned = false;
            iceboxMk2Spawned = false;
            superInvulnotronSpawned = false;
            wrathboneSpawned = false;
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
                    SpawnInvulnotron();
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
                    SpawnGromka();
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
                    SpawnIrukiBoldbeard();
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
                    SpawnSeekNSlaughter();
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
                    SpawnIceboxMk2();
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
                    SpawnSuperInvulnotron();
                    break;
                case PublicEventPhase.GoToLastEvent:
                    publicEvent.ActivateObjective(PublicEventObjective.GoToTheLastEvent, mapInstance.PlayerCount);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.PhineasARotostar9);
                    SpawnWipGuessedWorldLocationTrigger(
                        LastEventTeleporterWorldLocationId,
                        LastEventTeleporterObjectiveObjectId,
                        LastEventTeleporterPosition);
                    break;
                case PublicEventPhase.MeetWithPhineasARotostar:
                    publicEvent.ActivateObjective(PublicEventObjective.MeetWithPhineasARotostar);
                    SpawnWipGuessedWorldLocationTrigger(
                        PhineasMeetWorldLocationId,
                        PhineasMeetObjectiveObjectId,
                        PhineasMeetPosition);
                    break;
                case PublicEventPhase.DefeatWrathbone:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatWrathbone);
                    SpawnWrathbone();
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

        private void SpawnInvulnotron()
        {
            if (invulnotronSpawned)
                return;

            // Build 16042 reviewed LaughingWS rows place Invulnotron in event 667
            // phase 3 with an objective-credit script. Exact combat choreography
            // remains blocked, but the kill target is now spawnable and creditable.
            SpawnReviewedEntity(InvulnotronSpawn);
            invulnotronSpawned = true;
        }

        private void SpawnGromka()
        {
            if (gromkaSpawned)
                return;

            // Build 16042 reviewed LaughingWS rows place Gromka in event 667 phase 5
            // with an objective-credit script. Exact combat choreography remains
            // blocked, but the kill target is now spawnable and creditable.
            SpawnReviewedEntity(GromkaSpawn);
            gromkaSpawned = true;
        }

        private void SpawnIrukiBoldbeard()
        {
            if (irukiBoldbeardSpawned)
                return;

            // Build 16042 reviewed LaughingWS rows place Iruki Boldbeard in event
            // 667 phase 7 with an objective-credit script. Exact combat choreography
            // remains blocked, but the kill target is now spawnable and creditable.
            SpawnReviewedEntity(IrukiBoldbeardSpawn);
            irukiBoldbeardSpawned = true;
        }

        private void SpawnSeekNSlaughter()
        {
            if (seekNSlaughterSpawned)
                return;

            // Build 16042 reviewed LaughingWS rows place Seek-N-Slaughter in event
            // 667 phase 11 with an objective-credit script. Exact combat choreography
            // remains blocked, but the kill target is now spawnable and creditable.
            SpawnReviewedEntity(SeekNSlaughterSpawn);
            seekNSlaughterSpawned = true;
        }

        private void SpawnIceboxMk2()
        {
            if (iceboxMk2Spawned)
                return;

            // Build 16042 reviewed LaughingWS rows place Icebox Mk. 2 in event
            // 667 phase 13 with an objective-credit script. Exact combat choreography
            // remains blocked, but the kill target is now spawnable and creditable.
            SpawnReviewedEntity(IceboxMk2Spawn);
            iceboxMk2Spawned = true;
        }

        private void SpawnSuperInvulnotron()
        {
            if (superInvulnotronSpawned)
                return;

            // Build 16042 maps objective 4499 to TargetGroup 12361 and Creature2
            // 68096. DataMapping/Jabbithole rows place Super-Invulnotron in
            // Protogames Academy world 3173 zone 4507, but exact encounter
            // choreography and entity_event timing remain blocked.
            SpawnReviewedEntity(SuperInvulnotronSpawn);
            superInvulnotronSpawned = true;
        }

        private void SpawnWrathbone()
        {
            if (wrathboneSpawned)
                return;

            // Build 16042 objective 4346 and the Protogames Academy quest hooks
            // name Wrathbone Creature2 67944. DataMapping/Jabbithole rows place
            // it in world 3173 zone 4508; mechanics and reward side effects still
            // need retail smoke proof.
            SpawnReviewedEntity(WrathboneSpawn);
            wrathboneSpawned = true;
        }

        private void SpawnReviewedEntity(ProtogamesAcademySpawnModel spawn)
        {
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(spawn));
            AddToMap(entity, spawn.Position);
        }

        private static EntityModel CreateEntityModel(ProtogamesAcademySpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = ProtogamesAcademyWorldId,
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
                    EventId = ProtogamesAcademyPublicEventId,
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

        private sealed record ProtogamesAcademySpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            uint EventPhase,
            Vector3 Position,
            Vector3 Rotation,
            uint DisplayInfo,
            ushort FactionId,
            string ScriptName,
            float Health,
            float Level);
    }
}
