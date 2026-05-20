using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterOwnerId(3460)]
    public class TutorialMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
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
                return entity.CreatureId == creatureId;
            }
        }

        private sealed class TutorialMapInfo : IMapInfo
        {
            public required GameTable.Model.WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class TutorialMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private const ushort ExileMovementQuestId = 10513;
        private const ushort DominionMovementQuestId = 10521;
        private const ushort ExileHoverboardQuestId = 10527;
        private const ushort DominionHoverboardQuestId = 10532;
        private const uint ExileCombatFinalWorldLocationId = 51740u;
        private const uint DominionCombatStartWorldLocationId = 52898u;
        private const uint DominionCombatMineEasyWorldLocationId = 52899u;
        private const uint DominionCombatMineMediumWorldLocationId = 52900u;
        private const uint DominionCombatMineHardWorldLocationId = 52901u;
        private const uint DominionCombatTurretWorldLocationId00 = 52902u;
        private const uint DominionCombatTurretWorldLocationId01 = 52903u;
        private const uint DominionCombatEliteWorldLocationId = 53015u;

        private static readonly ushort[] starterTutorialQuestIds = [ExileMovementQuestId, DominionMovementQuestId, ExileHoverboardQuestId, DominionHoverboardQuestId];
        private static readonly ushort[] followUpTutorialQuestIds = [10518, 10519, 10520, 10522, 10523, 10524, 10525, 10526, 10528, 10530, 10540, 10541];
        private static readonly uint[] tutorialWorldLocationIds = [51735u, 51736u, 51737u, 51703u, 51734u];
        private static readonly MatchAllPlayerSearchCheck matchAllPlayers = new();

        private IBaseMap owner;
        private bool tutorialTriggersSpawned;
        private bool exileCombatLaneSpawned;
        private bool dominionCombatLaneSpawned;
        private readonly HashSet<uint> initialisedPlayerGuids = [];

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;
        private readonly IEntityFactory entityFactory;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<TutorialMapScript> log;

        public TutorialMapScript(
            ILogger<TutorialMapScript> log,
            ICinematicFactory cinematicFactory,
            IEntityFactory entityFactory,
            IGlobalQuestManager globalQuestManager,
            IGameTableManager gameTableManager)
        {
            this.log               = log;
            this.cinematicFactory   = cinematicFactory;
            this.entityFactory      = entityFactory;
            this.globalQuestManager = globalQuestManager;
            this.gameTableManager   = gameTableManager;
        }

        #endregion

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            EnsureTutorialTriggers();
            EnsureExileCombatSimulationEntities();
            EnsureDominionCombatSimulationEntities();
        }

        public void Update(double lastTick)
        {
            if (owner == null)
                return;

            foreach (IPlayer player in owner.Search(Vector3.Zero, null, matchAllPlayers))
            {
                if (!initialisedPlayerGuids.Add(player.Guid))
                    continue;

                log.LogDebug("Tutorial map scan recovered existing character {CharacterId} (guid {PlayerGuid}) on map {MapId}: position ({X}, {Y}, {Z}), starter states [{StarterQuestStates}], follow-up states [{FollowUpQuestStates}].",
                    player.CharacterId,
                    player.Guid,
                    owner.Entry.Id,
                    player.Position.X,
                    player.Position.Y,
                    player.Position.Z,
                    FormatQuestStates(player, starterTutorialQuestIds),
                    FormatQuestStates(player, followUpTutorialQuestIds));

                EnsureTutorialQuests(player);
            }
        }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            initialisedPlayerGuids.Add(player.Guid);
            bool hadTutorialState = HasAnyQuestState(player, starterTutorialQuestIds)
                || HasAnyQuestState(player, followUpTutorialQuestIds);

            log.LogDebug("Tutorial map player add for character {CharacterId} (guid {PlayerGuid}) on map {MapId}: faction {Faction}, position ({X}, {Y}, {Z}), starter states [{StarterQuestStates}], follow-up states [{FollowUpQuestStates}].",
                player.CharacterId,
                player.Guid,
                owner?.Entry?.Id,
                player.Faction1,
                player.Position.X,
                player.Position.Y,
                player.Position.Z,
                FormatQuestStates(player, starterTutorialQuestIds),
                FormatQuestStates(player, followUpTutorialQuestIds));

            EnsureTutorialQuests(player);

            if (!hadTutorialState)
            {
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INoviceTutorialOnEnter>());
                log.LogDebug("Queued Rider's Reef intro cinematic for fresh tutorial entry on character {CharacterId} (guid {PlayerGuid}).",
                    player.CharacterId,
                    player.Guid);
            }
        }

        public void OnRemoveFromMap(IGridEntity entity)
        {
            initialisedPlayerGuids.Remove(entity.Guid);
        }

        private void EnsureTutorialTriggers()
        {
            if (tutorialTriggersSpawned || owner == null)
                return;

            foreach (uint worldLocationId in tutorialWorldLocationIds)
            {
                IWorldLocationVolumeGridTriggerEntity trigger = entityFactory.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
                trigger.Initialise(worldLocationId, 0u);

                owner.EnqueueAdd(trigger, new TutorialMapPosition
                {
                    Info = new TutorialMapInfo
                    {
                        Entry = owner.Entry
                    },
                    Position = new Vector3(trigger.Entry.Position0, trigger.Entry.Position1, trigger.Entry.Position2)
                });
            }

            tutorialTriggersSpawned = true;
            log.LogDebug("Spawned Rider's Reef tutorial triggers on map {MapId}: world locations {WorldLocationIds}.",
                owner.Entry.Id, string.Join(", ", tutorialWorldLocationIds));
        }

        private void EnsureExileCombatSimulationEntities()
        {
            if (exileCombatLaneSpawned || owner == null)
                return;

            WorldLocation2Entry final = gameTableManager.WorldLocation2.GetEntry(ExileCombatFinalWorldLocationId);

            if (final == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef Exile final combat wave on map {MapId}: world location {WorldLocationId} is missing.",
                    owner.Entry.Id,
                    ExileCombatFinalWorldLocationId);
                return;
            }

            if (owner.Search(ToVector3(final), 30f, new CreatureSearchCheck(73492u)).Any()
                || owner.Search(ToVector3(final), 30f, new CreatureSearchCheck(73567u)).Any())
            {
                log.LogDebug("Skipping Rider's Reef Exile final combat wave fallback on map {MapId}: imported final-wave hostiles are already active near world location {WorldLocationId}.",
                    owner.Entry.Id,
                    ExileCombatFinalWorldLocationId);
                exileCombatLaneSpawned = true;
                return;
            }

            Vector3 finalPosition = ToVector3(final);
            SpawnTutorialEntity<INonPlayerEntity>(73492u, finalPosition + new Vector3(-9f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(73567u, finalPosition + new Vector3(8f, 0f, 5f));
            SpawnTutorialEntity<INonPlayerEntity>(73492u, finalPosition + new Vector3(14f, 0f, -9f));

            exileCombatLaneSpawned = true;
            log.LogDebug("Spawned Rider's Reef Exile final combat wave on map {MapId}: legionnaires=3.",
                owner.Entry.Id);
        }

        private void EnsureDominionCombatSimulationEntities()
        {
            if (dominionCombatLaneSpawned || owner == null)
                return;

            WorldLocation2Entry start = gameTableManager.WorldLocation2.GetEntry(DominionCombatStartWorldLocationId);
            WorldLocation2Entry easyMine = gameTableManager.WorldLocation2.GetEntry(DominionCombatMineEasyWorldLocationId);
            WorldLocation2Entry mediumMine = gameTableManager.WorldLocation2.GetEntry(DominionCombatMineMediumWorldLocationId);
            WorldLocation2Entry hardMine = gameTableManager.WorldLocation2.GetEntry(DominionCombatMineHardWorldLocationId);
            WorldLocation2Entry turret00 = gameTableManager.WorldLocation2.GetEntry(DominionCombatTurretWorldLocationId00);
            WorldLocation2Entry turret01 = gameTableManager.WorldLocation2.GetEntry(DominionCombatTurretWorldLocationId01);
            WorldLocation2Entry elite = gameTableManager.WorldLocation2.GetEntry(DominionCombatEliteWorldLocationId);

            if (start == null || easyMine == null || mediumMine == null || hardMine == null || turret00 == null || turret01 == null || elite == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef Dominion combat lane on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            SpawnTutorialEntity<INonPlayerEntity>(73465u, ToVector3(start) + new Vector3(-8f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(73465u, ToVector3(start) + new Vector3(12f, 0f, 4f));
            SpawnTutorialEntity<INonPlayerEntity>(73465u, ToVector3(easyMine) + new Vector3(-10f, 0f, -10f));
            SpawnTutorialEntity<INonPlayerEntity>(73465u, ToVector3(mediumMine) + new Vector3(12f, 0f, -14f));
            SpawnTutorialEntity<INonPlayerEntity>(73465u, ToVector3(hardMine) + new Vector3(-8f, 0f, -16f));

            SpawnTutorialEntity<ISimpleCollidableEntity>(73463u, ToVector3(easyMine));
            SpawnTutorialEntity<ISimpleCollidableEntity>(73667u, ToVector3(mediumMine));
            SpawnTutorialEntity<ISimpleCollidableEntity>(73668u, ToVector3(hardMine));

            SpawnTutorialEntity<INonPlayerEntity>(74862u, ToVector3(turret00));
            SpawnTutorialEntity<INonPlayerEntity>(74862u, ToVector3(turret01));

            Vector3 elitePosition = ToVector3(elite);
            SpawnTutorialEntity<INonPlayerEntity>(73473u, elitePosition + new Vector3(-9f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(73566u, elitePosition + new Vector3(8f, 0f, 5f));
            SpawnTutorialEntity<INonPlayerEntity>(73473u, elitePosition + new Vector3(14f, 0f, -9f));

            dominionCombatLaneSpawned = true;
            log.LogDebug("Spawned Rider's Reef Dominion combat lane on map {MapId}: dagun=5, mines=3, turrets=2, finalHostiles=3.",
                owner.Entry.Id);
        }

        private void SpawnTutorialEntity<T>(uint creatureId, Vector3 position, Vector3 rotation = default) where T : class, IWorldEntity
        {
            T entity = entityFactory.CreateEntity<T>();
            if (entity == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef tutorial entity {CreatureId} on map {MapId}: entity factory returned null for {EntityType}.",
                    creatureId,
                    owner?.Entry?.Id,
                    typeof(T).Name);
                return;
            }

            entity.Initialise(creatureId);
            entity.Rotation = rotation;

            owner.EnqueueAdd(entity, new TutorialMapPosition
            {
                Info = new TutorialMapInfo
                {
                    Entry = owner.Entry
                },
                Position = position
            });
        }

        private static Vector3 ToVector3(WorldLocation2Entry worldLocation)
        {
            return new Vector3(worldLocation.Position0, worldLocation.Position1, worldLocation.Position2);
        }

        private void EnsureTutorialQuests(IPlayer player)
        {
            if (HasAnyQuestState(player, followUpTutorialQuestIds))
            {
                log.LogDebug("Skipping Rider's Reef tutorial ensure for character {CharacterId} (guid {PlayerGuid}): follow-up quest state already present [{FollowUpQuestStates}].",
                    player.CharacterId,
                    player.Guid,
                    FormatQuestStates(player, followUpTutorialQuestIds));
                return;
            }

            (ushort movementQuestId, ushort hoverboardQuestId) = player.Faction1 switch
            {
                Faction.Exile    => (ExileMovementQuestId, ExileHoverboardQuestId),
                Faction.Dominion => (DominionMovementQuestId, DominionHoverboardQuestId),
                _                => ((ushort)0, (ushort)0)
            };

            if (movementQuestId == 0)
                return;

            GrantQuestIfMissing(player, movementQuestId);
            GrantQuestIfMissing(player, hoverboardQuestId);
            SyncTutorialAreaObjectives(player);
        }

        private void GrantQuestIfMissing(IPlayer player, ushort questId)
        {
            if (player.QuestManager.GetQuestState(questId) != null)
                return;

            IQuestInfo questInfo = globalQuestManager.GetQuestInfo(questId);
            if (questInfo != null)
            {
                player.QuestManager.QuestAdd(questInfo);
                log.LogDebug("Granted Rider's Reef tutorial quest {QuestId} to character {CharacterId} (guid {PlayerGuid}) on map {MapId}.",
                    questId, player.CharacterId, player.Guid, owner?.Entry?.Id);
            }
        }

        private void SyncTutorialAreaObjectives(IPlayer player)
        {
            int furthestReachedIndex = GetFurthestReachedTutorialWorldLocationIndex(player);
            if (furthestReachedIndex < 0)
            {
                log.LogDebug("Tutorial area sync found no Rider's Reef world-location overlap for character {CharacterId} (guid {PlayerGuid}): position ({X}, {Y}, {Z}), hit radius {HitRadius}, starter states [{StarterQuestStates}].",
                    player.CharacterId,
                    player.Guid,
                    player.Position.X,
                    player.Position.Y,
                    player.Position.Z,
                    player.HitRadius,
                    FormatQuestStates(player, starterTutorialQuestIds));
                return;
            }

            foreach (IQuest quest in player.QuestManager.GetActiveQuests().Where(q => starterTutorialQuestIds.Contains(q.Id)))
            {
                for (int index = 0; index <= furthestReachedIndex; index++)
                {
                    uint worldLocationId = tutorialWorldLocationIds[index];
                    foreach (IQuestObjective objective in GetObjectivesToUpdate(quest, worldLocationId))
                    {
                        log.LogDebug("Tutorial area sync advanced character {CharacterId} (guid {PlayerGuid}): quest {QuestId} objective {ObjectiveId} at world location {WorldLocationId}, furthest index {FurthestIndex}, position ({X}, {Y}, {Z}).",
                            player.CharacterId, player.Guid, quest.Id, objective.ObjectiveInfo.Id, worldLocationId, furthestReachedIndex,
                            player.Position.X, player.Position.Y, player.Position.Z);
                        quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
                    }
                }
            }
        }

        private int GetFurthestReachedTutorialWorldLocationIndex(IPlayer player)
        {
            int furthestIndex = -1;
            float horizontalPadding = player.HitRadius * 0.5f;

            for (int index = 0; index < tutorialWorldLocationIds.Length; index++)
            {
                WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(tutorialWorldLocationIds[index]);
                if (worldLocation != null && IsInsideWorldLocation(player.Position, worldLocation, horizontalPadding))
                    furthestIndex = index;
            }

            return furthestIndex;
        }

        private static bool MatchesWorldLocation(IQuestObjective objective, uint worldLocationId)
        {
            if (objective.ObjectiveInfo.Type != QuestObjectiveType.EnterArea)
                return false;

            QuestObjectiveEntry objectiveEntry = objective.ObjectiveInfo.Entry;
            return objectiveEntry.WorldLocationsIdIndicator00 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator01 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator02 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator03 == worldLocationId;
        }

        private static IEnumerable<IQuestObjective> GetObjectivesToUpdate(IQuest quest, uint worldLocationId)
        {
            List<IQuestObjective> matchingObjectives = quest
                .Where(o => MatchesWorldLocation(o, worldLocationId))
                .Where(o => !o.IsComplete())
                .ToList();

            if (matchingObjectives.Count == 0)
                return [];

            IEnumerable<IQuestObjective> objectivesToUpdate = matchingObjectives.Any(o => !o.ObjectiveInfo.IsOptional())
                ? matchingObjectives.Where(o => !o.ObjectiveInfo.IsOptional())
                : matchingObjectives;

            return objectivesToUpdate.OrderByDescending(o => o.Index).ToList();
        }

        private static bool IsInsideWorldLocation(Vector3 position, WorldLocation2Entry worldLocation, float horizontalPadding = 0f)
        {
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(position.X, position.Z),
                new Vector2(worldLocation.Position0, worldLocation.Position2));

            float horizontalRange = worldLocation.Radius + horizontalPadding;
            if (horizontalDistanceSquared > horizontalRange * horizontalRange)
                return false;

            return worldLocation.MaxVerticalDistance <= 0f
                || MathF.Abs(position.Y - worldLocation.Position1) <= worldLocation.MaxVerticalDistance;
        }

        private static bool HasAnyQuestState(IPlayer player, IEnumerable<ushort> questIds)
        {
            return questIds.Any(questId => player.QuestManager.GetQuestState(questId) != null);
        }

        private static string FormatQuestStates(IPlayer player, IEnumerable<ushort> questIds)
        {
            return string.Join(", ", questIds.Select(questId => $"{questId}:{player.QuestManager.GetQuestState(questId)?.ToString() ?? "None"}"));
        }
    }
}
