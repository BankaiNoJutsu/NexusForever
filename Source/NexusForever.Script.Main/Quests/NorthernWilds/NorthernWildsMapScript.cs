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
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Map script for Northern Wilds (world 426).
    /// </summary>
    [ScriptFilterOwnerId(426)]
    public class NorthernWildsMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private sealed class NorthernWildsMapInfo : IMapInfo
        {
            public required WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class NorthernWildsMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private sealed class MatchAllPlayerSearchCheck : ISearchCheck<IPlayer>
        {
            public bool CheckEntity(IPlayer entity)
            {
                return true;
            }
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
                return entity?.CreatureId == creatureId;
            }
        }

        private sealed class LoftiteCrystalCollectionSearchCheck : ISearchCheck<IWorldEntity>
        {
            private readonly IPlayer player;

            public LoftiteCrystalCollectionSearchCheck(IPlayer player)
            {
                this.player = player;
            }

            public bool CheckEntity(IWorldEntity entity)
            {
                if (entity is not ICreatureEntity crystal)
                    return false;

                if (crystal.CreatureId != Q3486LoftiteCrystalId || crystal.Health == 0u)
                    return false;

                Vector3 delta = crystal.Position - player.Position;
                if (MathF.Abs(delta.Y) > MathF.Max(Q3486LoftiteCrystalCollectionVerticalRange, crystal.HitRadius))
                    return false;

                float horizontalDistanceSquared = delta.X * delta.X + delta.Z * delta.Z;
                float horizontalRange = MathF.Max(
                    Q3486LoftiteCrystalCollector.CollectionRange,
                    player.HitRadius * 0.5f + crystal.HitRadius * 0.5f + Q3486LoftiteCrystalCollectionHorizontalPadding);

                return horizontalDistanceSquared <= horizontalRange * horizontalRange;
            }
        }

        private const uint Q3486LoftiteCrystalId = 11205u;
        private const float Q3486LoftiteCrystalCollectionSearchRange = 20f;
        private const float Q3486LoftiteCrystalCollectionHorizontalPadding = 1.5f;
        private const float Q3486LoftiteCrystalCollectionVerticalRange = 25f;

        private const ushort Q3480ReportingForDutyQuest = 3480;

        private const ushort Q4526SpatialAnomalyQuest = 4526;
        private const uint Q4526InvestigateWreckageObjective = 6164u;
        private const uint Q4526WreckageWL = 9706u;

        private const ushort Q3486EmpoweredTowerQuest = 3486;
        private const uint Q3486ArrivedAtTowerZoneId = 729u;
        private const uint Q3486ArrivedAtTowerObjective = 4987u;
        private const uint Q3486ArrivedAtTowerStoryPanel = 1575u;

        private const uint DominionUltrabotPublicEventId = 154u;
        private const uint CampIcefuryZoneId = 602u;

        private const uint AvalancheVisualCreatureId = NorthernWildsAvalancheEntityScript.AvalancheVisualCreatureId;
        private const uint AvalancheCasterCreatureId = NorthernWildsAvalancheEntityScript.AvalancheCasterCreatureId;

        private const uint GranokDropPodCreatureId = 13747u;
        private const uint GranokDropPodDecalCreatureId = 14126u;
        private const byte GranokDropPodDecalQuestChecklistIdx = 255;
        private const float DropPodVisibilityDuplicateSearchRange = 7f;

        private static readonly MatchAllPlayerSearchCheck matchAllPlayers = new();
        private static readonly CreatureSearchCheck granokDropPodSearchCheck = new(GranokDropPodCreatureId);
        private static readonly CreatureSearchCheck granokDropPodDecalSearchCheck = new(GranokDropPodDecalCreatureId);

        // Runtime-owned source positions for repeating Northern Wilds avalanche hazards.
        // Imported Creature2 15615 visual rows and legacy 67662 caster rows are
        // removed so static runout placements cannot replace the moving hazards.
        private static readonly IReadOnlyList<Vector3> AvalancheFallbackPositions =
            NorthernWildsAvalancheEntityScript.FallbackSourcePositions;

        // The imported Northern Wilds data includes two Granok drop-pod door/collider rows near
        // 4110, -5215 without matching visible pod body/decal rows. These match the existing pod
        // companion-row offset used elsewhere in the zone.
        private static readonly DropPodVisibilityFallback[] DropPodVisibilityFallbacks =
        [
            new(
                new Vector3(4119.69f, -681.132f, -5193.9f),
                new Vector3(0.4843985f, 0f, 0f)),
            new(
                new Vector3(4115.84f, -684.355f, -5215.019f),
                new Vector3(0.06834697f, 0f, 0f))
        ];

        private readonly IGameTableManager gameTableManager;
        private readonly IEntityFactory entityFactory;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IStoryBuilder storyBuilder;
        private readonly ILogger<NorthernWildsMapScript> log;

        private IBaseMap owner;
        private IPublicEvent dominionUltrabotPublicEvent;
        private readonly HashSet<ulong> dominionUltrabotParticipants = [];
        private readonly HashSet<uint> q3486CollectedCrystalGuids = [];
        private bool avalancheFallbacksSpawned;
        private bool dropPodVisibilityFallbacksSpawned;

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
            EnsureDominionUltrabotEvent();
            EnsureAvalancheFallbacks();
            EnsureDropPodVisibilityFallbacks();
        }

        public void Update(double lastTick)
        {
            TryCollectEmpoweredTowerCrystals();
            TryCreditSpatialAnomalyWreckageInvestigation();
        }

        public void OnAddToMap(IGridEntity entity)
        {
            if (ShouldRemoveImportedAvalancheRow(entity))
            {
                IWorldEntity worldEntity = (IWorldEntity)entity;
                log.LogInformation("Removing imported static Northern Wilds avalanche row on map {MapId}: creature={CreatureId}, entity={EntityId}, guid={Guid}, position={Position}.",
                    owner?.Entry?.Id,
                    worldEntity.CreatureId,
                    worldEntity.EntityId,
                    worldEntity.Guid,
                    worldEntity.Position);
                entity.RemoveFromMap();
                return;
            }

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

            if (entity is IWorldEntity worldEntity && worldEntity.CreatureId == Q3486LoftiteCrystalId)
                q3486CollectedCrystalGuids.Remove(worldEntity.Guid);
        }

        public void OnEnterZone(IWorldEntity entity, uint zone)
        {
            if (entity is not IPlayer player)
                return;

            TryCreditEmpoweredTowerArrival(player, zone);

            ActivatePathMissions(player, zone);
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

        private static bool ShouldRemoveImportedAvalancheRow(IGridEntity entity)
        {
            return entity is IWorldEntity worldEntity
                && IsImportedStaticAvalancheCreature(worldEntity.CreatureId)
                && !NorthernWildsAvalancheEntityScript.IsManagedAvalancheFallback(worldEntity);
        }

        private static bool IsImportedStaticAvalancheCreature(uint creatureId)
        {
            return creatureId == AvalancheVisualCreatureId
                || creatureId == AvalancheCasterCreatureId;
        }

        private void TryCollectEmpoweredTowerCrystals()
        {
            if (owner == null)
                return;

            foreach (IPlayer player in owner.Search(Vector3.Zero, null, matchAllPlayers).ToList())
                TryCollectEmpoweredTowerCrystal(player);
        }

        private void TryCollectEmpoweredTowerCrystal(IPlayer player)
        {
            if (player.Zone?.Id != Q3486ArrivedAtTowerZoneId)
                return;

            if (player.QuestManager.GetQuestState(Q3486EmpoweredTowerQuest) != QuestState.Accepted)
                return;

            foreach (ICreatureEntity crystal in owner
                .Search(player.Position, Q3486LoftiteCrystalCollectionSearchRange, new LoftiteCrystalCollectionSearchCheck(player))
                .OfType<ICreatureEntity>()
                .OrderBy(crystal => HorizontalDistanceSquared(player.Position, crystal.Position))
                .ToList())
            {
                if (!q3486CollectedCrystalGuids.Add(crystal.Guid))
                    continue;

                if (Q3486LoftiteCrystalCollector.TryCollect(player, crystal))
                    return;

                q3486CollectedCrystalGuids.Remove(crystal.Guid);
            }
        }

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.X - b.X;
            float z = a.Z - b.Z;
            return x * x + z * z;
        }

        private void TryQueueIntroCinematic(IPlayer player)
        {
            if (player.QuestManager.GetQuestState(Q3480ReportingForDutyQuest) != null)
                return;

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INorthernWildsOnCreate>());
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
            player.PathManager?.TryActivateCurrentZoneEpisode();
        }

        private static void ActivatePathMissions(IPlayer player, uint worldZoneId)
        {
            player.PathManager?.TryActivateCurrentZoneEpisode(worldZoneId);
        }

        private void EnsureAvalancheFallbacks()
        {
            if (avalancheFallbacksSpawned || owner == null)
                return;

            int spawned = 0;
            foreach (Vector3 position in AvalancheFallbackPositions)
            {
                INonPlayerEntity avalanche = entityFactory.CreateEntity<INonPlayerEntity>();
                if (avalanche == null)
                {
                    log.LogWarning("Unable to spawn Northern Wilds avalanche fallback on map {MapId}: entity factory returned null.", owner.Entry.Id);
                    continue;
                }

                avalanche.Initialise(AvalancheVisualCreatureId);
                avalanche.CreateFlags |= EntityCreateFlag.Immediate;
                owner.EnqueueAdd(avalanche, new NorthernWildsMapPosition
                {
                    Info = new NorthernWildsMapInfo { Entry = owner.Entry },
                    Position = position
                });
                spawned++;
            }

            avalancheFallbacksSpawned = true;
            log.LogInformation("Northern Wilds avalanche fallbacks initialised on map {MapId}: creature={CreatureId}, spawned={Spawned}.",
                owner.Entry.Id,
                AvalancheVisualCreatureId,
                spawned);
        }

        private void EnsureDropPodVisibilityFallbacks()
        {
            if (dropPodVisibilityFallbacksSpawned || owner == null)
                return;

            int podsSpawned = 0;
            int decalsSpawned = 0;
            int podsSkipped = 0;
            int decalsSkipped = 0;
            foreach (DropPodVisibilityFallback fallback in DropPodVisibilityFallbacks)
            {
                if (owner.Search(fallback.Position, DropPodVisibilityDuplicateSearchRange, granokDropPodSearchCheck).Any())
                {
                    podsSkipped++;
                }
                else if (SpawnDropPodVisibilityEntity(GranokDropPodCreatureId, fallback, null))
                {
                    podsSpawned++;
                }

                if (owner.Search(fallback.Position, DropPodVisibilityDuplicateSearchRange, granokDropPodDecalSearchCheck).Any())
                {
                    decalsSkipped++;
                }
                else if (SpawnDropPodVisibilityEntity(GranokDropPodDecalCreatureId, fallback, GranokDropPodDecalQuestChecklistIdx))
                {
                    decalsSpawned++;
                }
            }

            dropPodVisibilityFallbacksSpawned = true;
            log.LogDebug("Northern Wilds drop-pod visibility fallbacks initialised on map {MapId}: podsSpawned={PodsSpawned}, decalsSpawned={DecalsSpawned}, podsSkippedExisting={PodsSkipped}, decalsSkippedExisting={DecalsSkipped}.",
                owner.Entry.Id,
                podsSpawned,
                decalsSpawned,
                podsSkipped,
                decalsSkipped);
        }

        private bool SpawnDropPodVisibilityEntity(uint creatureId, DropPodVisibilityFallback fallback, byte? questChecklistIndex)
        {
            INonPlayerEntity entity = entityFactory.CreateEntity<INonPlayerEntity>();
            if (entity == null)
            {
                log.LogWarning("Unable to spawn Northern Wilds drop-pod visibility fallback {CreatureId} on map {MapId}: entity factory returned null.",
                    creatureId,
                    owner.Entry.Id);
                return false;
            }

            entity.Initialise(creatureId);
            entity.Rotation = fallback.Rotation;
            if (questChecklistIndex.HasValue)
                entity.SetQuestChecklistIndex(questChecklistIndex.Value);

            entity.CreateFlags |= EntityCreateFlag.Immediate;
            owner.EnqueueAdd(entity, new NorthernWildsMapPosition
            {
                Info = new NorthernWildsMapInfo { Entry = owner.Entry },
                Position = fallback.Position
            });
            return true;
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

        private void TryCreditSpatialAnomalyWreckageInvestigation()
        {
            if (owner == null)
                return;

            WorldLocation2Entry wreckageLocation = gameTableManager.WorldLocation2.GetEntry(Q4526WreckageWL);
            if (wreckageLocation == null)
                return;

            foreach (IPlayer player in owner.Search(Vector3.Zero, null, matchAllPlayers).ToList())
            {
                if (player.QuestManager.GetQuestState(Q4526SpatialAnomalyQuest) != QuestState.Accepted)
                    continue;

                if (!IsInsideWorldLocation(player.Position, player.HitRadius, wreckageLocation))
                    continue;

                player.QuestManager.ObjectiveUpdate(Q4526InvestigateWreckageObjective, 1u);
            }
        }

        private static bool IsInsideWorldLocation(Vector3 position, float hitRadius, WorldLocation2Entry worldLocation)
        {
            Vector3 center = new(worldLocation.Position0, worldLocation.Position1, worldLocation.Position2);
            float radius = MathF.Max(worldLocation.Radius, 1f) + hitRadius * 0.5f;
            if (MathF.Abs(position.Y - center.Y) > MathF.Max(radius, hitRadius))
                return false;

            return HorizontalDistanceSquared(position, center) <= radius * radius;
        }

        private readonly record struct DropPodVisibilityFallback(Vector3 Position, Vector3 Rotation);

    }
}
