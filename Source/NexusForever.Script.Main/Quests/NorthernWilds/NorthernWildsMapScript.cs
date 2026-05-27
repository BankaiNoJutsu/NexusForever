using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Map script for Northern Wilds (world 426).
    /// Dynamically spawns quest entities when map loads.
    /// </summary>
    [ScriptFilterOwnerId(426)]
    public class NorthernWildsMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private sealed class NorthernWildsMapInfo : IMapInfo
        {
            public required GameTable.Model.WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class NorthernWildsMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private sealed class CreatureSearchCheck : ISearchCheck<IWorldEntity>
        {
            private readonly uint creatureId;

            public CreatureSearchCheck(uint creatureId)
            {
                this.creatureId = creatureId;
            }

            public bool CheckEntity(IWorldEntity entity)
            {
                return entity.CreatureId == creatureId;
            }
        }

        private const uint Q3486LoftiteCrystalId = 11205u;
        private const uint Q3486LoftiteCrystalWL = 7807u;

        private const uint Q3667ControlPanelId = 11194u;
        private const uint Q3667ControlPanelWL = 7778u;

        private const uint Q3480SettlerNpcId = 11070u;
        private const uint Q3480SettlerNpcAreaWL = 12155u;

        private const uint Q3487CannonId = 11251u;
        private const uint Q3487CannonWL = 9196u;

        private const uint Q3487UltrabotId = 12526u;
        private const uint Q3487UltrabotWL = 9200u;

        private const ushort Q3480ReportingForDutyQuest = 3480;

        private const ushort Q3486EmpoweredTowerQuest = 3486;
        private const uint Q3486ArrivedAtTowerZoneId = 729u;
        private const uint Q3486ArrivedAtTowerObjective = 4987u;
        private const uint Q3486ArrivedAtTowerStoryPanel = 1575u;

        private const uint DominionUltrabotPublicEventId = 154u;
        private const uint CampIcefuryZoneId = 602u;

        private static readonly IReadOnlyDictionary<Path, (ushort EpisodeId, IReadOnlyDictionary<ushort, uint> Missions)> PathMissions = new Dictionary<Path, (ushort, IReadOnlyDictionary<ushort, uint>)>
        {
            [Path.Soldier] = (8, new Dictionary<ushort, uint>
            {
                [33] = 25,
                [34] = 25,
                [156] = 25
            }),
            [Path.Settler] = (82, new Dictionary<ushort, uint>
            {
                [650] = 25,
                [651] = 25,
                [652] = 25
            }),
            [Path.Scientist] = (28, new Dictionary<ushort, uint>
            {
                [42] = 25,
                [160] = 25,
                [648] = 25
            }),
            [Path.Explorer] = (9, new Dictionary<ushort, uint>
            {
                [35] = 25,
                [36] = 25,
                [158] = 25,
                [1254] = 25
            })
        };

        private readonly IEntityFactory entityFactory;
        private readonly IGameTableManager gameTableManager;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IStoryBuilder storyBuilder;
        private readonly ILogger<NorthernWildsMapScript> log;

        private IBaseMap owner;
        private bool entitiesSpawned;
        private IPublicEvent dominionUltrabotPublicEvent;
        private readonly HashSet<ulong> dominionUltrabotParticipants = [];

        public NorthernWildsMapScript(
            ILogger<NorthernWildsMapScript> log,
            IEntityFactory entityFactory,
            IGameTableManager gameTableManager,
            ICinematicFactory cinematicFactory,
            IStoryBuilder storyBuilder)
        {
            this.log = log;
            this.entityFactory = entityFactory;
            this.gameTableManager = gameTableManager;
            this.cinematicFactory = cinematicFactory;
            this.storyBuilder = storyBuilder;
        }

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            EnsureQuestEntities();
            EnsureDominionUltrabotEvent();
        }

        public void Update(double lastTick) { }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            TryQueueIntroCinematic(player);
            ActivatePathMissions(player);
            if (player.Zone?.Id == CampIcefuryZoneId)
                TryJoinDominionUltrabotEvent(player);
        }

        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IPlayer player)
                TryLeaveDominionUltrabotEvent(player);
        }

        public void OnEnterZone(IWorldEntity entity, uint zone)
        {
            if (entity is not IPlayer player)
                return;

            TryCreditEmpoweredTowerArrival(player, zone);

            ActivatePathMissions(player);
            if (zone == CampIcefuryZoneId)
                TryJoinDominionUltrabotEvent(player);
            else
                TryLeaveDominionUltrabotEvent(player);
        }

        private void TryCreditEmpoweredTowerArrival(IPlayer player, uint zone)
        {
            if (zone != Q3486ArrivedAtTowerZoneId)
                return;

            if (player.QuestManager.GetQuestState(Q3486EmpoweredTowerQuest) != QuestState.Accepted)
                return;

            storyBuilder.SendServerStoryPanelShow(player, Q3486ArrivedAtTowerStoryPanel);
            player.QuestManager.ObjectiveUpdate(Q3486ArrivedAtTowerObjective, 1u);
        }

        private void TryQueueIntroCinematic(IPlayer player)
        {
            if (player.QuestManager.GetQuestState(Q3480ReportingForDutyQuest) != null)
                return;

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INorthernWildsOnCreate>());
        }

        private void EnsureQuestEntities()
        {
            if (entitiesSpawned || owner == null)
                return;

            SpawnEntityIfMissing(Q3486LoftiteCrystalId, Q3486LoftiteCrystalWL, "Q3486 Loftite Crystal");
            SpawnEntityIfMissing(Q3667ControlPanelId, Q3667ControlPanelWL, "Q3667 Control Panel");
            SpawnEntityIfMissing(Q3480SettlerNpcId, Q3480SettlerNpcAreaWL, "Q3480 Settler NPC");
            SpawnEntityIfMissing(Q3487CannonId, Q3487CannonWL, "Q3487 Dominion Cannon");
            SpawnEntityIfMissing(Q3487UltrabotId, Q3487UltrabotWL, "Q3487 Ultrabot");

            entitiesSpawned = true;
            log.LogDebug("Northern Wilds quest entities initialised on map {MapId}.", owner.Entry.Id);
        }

        private IPublicEvent EnsureDominionUltrabotEvent()
        {
            if (owner == null)
                return null;

            IPublicEvent existingEvent = owner.PublicEventManager.GetEvent(DominionUltrabotPublicEventId);
            if (existingEvent != null)
            {
                dominionUltrabotPublicEvent = existingEvent;
                return existingEvent.IsFinalised ? null : existingEvent;
            }

            dominionUltrabotPublicEvent = owner.PublicEventManager.CreateEvent(DominionUltrabotPublicEventId);
            if (dominionUltrabotPublicEvent == null)
                log.LogWarning("Unable to create Northern Wilds public event {PublicEventId} on map {MapId}.", DominionUltrabotPublicEventId, owner.Entry.Id);

            return dominionUltrabotPublicEvent;
        }

        private static void ActivatePathMissions(IPlayer player)
        {
            if (!PathMissions.TryGetValue(player.Path, out (ushort EpisodeId, IReadOnlyDictionary<ushort, uint> Missions) pathEpisode))
                return;

            player.PathManager.ActivateMissions(pathEpisode.EpisodeId, pathEpisode.Missions);
        }

        private void TryJoinDominionUltrabotEvent(IPlayer player)
        {
            if (!dominionUltrabotParticipants.Add(player.CharacterId))
                return;

            IPublicEvent publicEvent = EnsureDominionUltrabotEvent();
            if (publicEvent == null || publicEvent.IsFinalised)
            {
                dominionUltrabotParticipants.Remove(player.CharacterId);
                return;
            }

            publicEvent.JoinEvent(player, PublicEventTeam.PublicTeam);
        }

        private void TryLeaveDominionUltrabotEvent(IPlayer player)
        {
            if (!dominionUltrabotParticipants.Remove(player.CharacterId))
                return;

            IPublicEvent publicEvent = dominionUltrabotPublicEvent;
            if (publicEvent == null || publicEvent.IsFinalised)
                return;

            publicEvent.LeaveEvent(player, PublicEventRemoveReason.LeftArea);
        }

        public void OnPublicEventFinish(IPublicEvent publicEvent, IPublicEventTeam publicEventTeam)
        {
            if (publicEvent.Id != DominionUltrabotPublicEventId)
                return;

            dominionUltrabotParticipants.Clear();
            dominionUltrabotPublicEvent = null;
        }

        private void SpawnEntityIfMissing(uint creatureId, uint worldLocationId, string label)
        {
            WorldLocation2Entry wl = gameTableManager.WorldLocation2.GetEntry(worldLocationId);
            if (wl == null)
            {
                log.LogWarning("Unable to spawn {Label} on map {MapId}: world location {WorldLocationId} missing.", label, owner.Entry.Id, worldLocationId);
                return;
            }

            Vector3 position = new(wl.Position0, wl.Position1, wl.Position2);
            if (owner.Search(position, 20f, new CreatureSearchCheck(creatureId)).Any())
            {
                log.LogDebug("Skipping {Label} fallback on map {MapId}: creature {CreatureId} is already active near WL {WorldLocationId}.",
                    label, owner.Entry.Id, creatureId, worldLocationId);
                return;
            }

            INonPlayerEntity entity = entityFactory.CreateEntity<INonPlayerEntity>();
            entity.Initialise(creatureId);
            entity.Rotation = Vector3.Zero;

            owner.EnqueueAdd(entity, new NorthernWildsMapPosition
            {
                Info = new NorthernWildsMapInfo { Entry = owner.Entry },
                Position = position
            });

            log.LogDebug("Spawned {Label} (creature {CreatureId}) on map {MapId} at WL {WorldLocationId}.", label, creatureId, owner.Entry.Id, worldLocationId);
        }
    }
}
