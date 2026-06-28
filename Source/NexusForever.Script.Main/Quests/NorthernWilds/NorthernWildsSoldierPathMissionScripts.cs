using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    public abstract class NorthernWildsSoldierHoldoutEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>, IOwnedScript<ISimpleEntity>, IUpdate
    {
        private sealed class SoldierHoldoutMapInfo : IMapInfo
        {
            public required WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class SoldierHoldoutMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private sealed class CreatureIdRangeSearchCheck : ISearchCheck<IWorldEntity>
        {
            private readonly IReadOnlySet<uint> creatureIds;
            private readonly Vector3 origin;
            private readonly float radiusSquared;
            private readonly IReadOnlySet<uint> ignoredGuids;

            public CreatureIdRangeSearchCheck(
                IReadOnlySet<uint> creatureIds,
                Vector3 origin,
                float radius,
                IReadOnlySet<uint> ignoredGuids)
            {
                this.creatureIds = creatureIds;
                this.origin = origin;
                radiusSquared = radius * radius;
                this.ignoredGuids = ignoredGuids;
            }

            public bool CheckEntity(IWorldEntity entity)
            {
                if (entity == null || !creatureIds.Contains(entity.CreatureId))
                    return false;

                if (ignoredGuids.Contains(entity.Guid))
                    return false;

                return Vector3.DistanceSquared(origin, entity.Position) <= radiusSquared;
            }
        }

        private readonly IEntityFactory entityFactory;
        private readonly Dictionary<IPlayer, ActiveSoldierEvent> activeEvents = [];
        private static readonly Dictionary<IWorldEntity, ActiveSoldierEvent> activeEventSpawns = new(ReferenceEqualityComparer.Instance);
        private IWorldEntity owner;

        protected abstract SoldierEventDefinition Definition { get; }

        protected NorthernWildsSoldierHoldoutEntityScript()
        {
        }

        protected NorthernWildsSoldierHoldoutEntityScript(IEntityFactory entityFactory)
        {
            this.entityFactory = entityFactory;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            OnLoadOwner(owner);
        }

        public void OnLoad(ISimpleEntity owner)
        {
            OnLoadOwner(owner);
        }

        private void OnLoadOwner(IWorldEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator.Path != Path.Soldier)
                return;

            if (activeEvents.ContainsKey(activator))
                return;

            if (activator.PathManager.IsMissionCompleteByObjectId(Definition.PathSoldierEventId))
            {
                RemovePreExistingSpawnedCreatures();
                return;
            }

            if (!activator.PathManager.IsMissionActiveByObjectId(Definition.PathSoldierEventId)
                && !activator.PathManager.TryActivateSoldierMissionByEventId(Definition.PathSoldierEventId))
            {
                return;
            }

            RemovePreExistingSpawnedCreatures();

            ActiveSoldierEvent soldierEvent = new(Definition, activator, this);
            activeEvents.Add(activator, soldierEvent);
            SendStatus(soldierEvent, PlayerPathSoldierEventMode.InitialDelay);
        }

        public void Update(double lastTick)
        {
            if (activeEvents.Count == 0)
                return;

            foreach (ActiveSoldierEvent soldierEvent in activeEvents.Values.ToList())
                UpdateEvent(soldierEvent, lastTick);
        }

        private void UpdateEvent(ActiveSoldierEvent soldierEvent, double lastTick)
        {
            SoldierEventDefinition definition = soldierEvent.Definition;
            soldierEvent.ElapsedMilliseconds += Math.Max(lastTick, 0d) * 1000d;

            if (!soldierEvent.HasActivated
                && soldierEvent.ElapsedMilliseconds >= definition.InitialSpawnMilliseconds)
            {
                soldierEvent.HasActivated = true;
                soldierEvent.NextWaveMilliseconds = definition.InitialSpawnMilliseconds;
                SendStatus(soldierEvent, PlayerPathSoldierEventMode.Active);
            }

            while (soldierEvent.HasActivated
                && soldierEvent.NextWaveIndex < definition.WaveIndices.Count
                && soldierEvent.ElapsedMilliseconds >= soldierEvent.NextWaveMilliseconds)
            {
                uint waveIndex = definition.WaveIndices[soldierEvent.NextWaveIndex];
                bool isBoss = soldierEvent.NextWaveIndex == definition.WaveIndices.Count - 1;
                soldierEvent.Player.Session.EnqueueMessageEncrypted(new ServerPathSoldierHoldOutNextWave
                {
                    PathSoldierEventId = definition.PathSoldierEventId,
                    WaveIndex = waveIndex,
                    IsBoss = isBoss
                });

                SpawnWave(soldierEvent, waveIndex);

                soldierEvent.NextWaveIndex++;
                soldierEvent.NextWaveMilliseconds += definition.MaxTimeBetweenWavesMilliseconds;
                if (definition.MaxTimeBetweenWavesMilliseconds == 0)
                    break;
            }

            if (soldierEvent.ActiveElapsedMilliseconds < definition.MaxEventMilliseconds)
                return;

            if (definition.CompleteOnBossKill)
                CompleteEvent(soldierEvent, PlayerPathSoldierResult.FailTimeOut);
            else
                CompleteEvent(soldierEvent, PlayerPathSoldierResult.Success);
        }

        public void OnRemoveFromMap(IBaseMap map)
        {
            foreach (ActiveSoldierEvent soldierEvent in activeEvents.Values.ToList())
            {
                CleanupEventSpawns(soldierEvent);
            }

            activeEvents.Clear();
        }

        internal static bool TryHandleHoldoutSpawnKilled(IWorldEntity spawn, IUnitEntity killer)
        {
            if (spawn == null)
                return false;

            if (!activeEventSpawns.TryGetValue(spawn, out ActiveSoldierEvent soldierEvent))
                return false;

            bool isBoss = soldierEvent.Definition.CompleteOnBossKill
                && soldierEvent.Definition.BossCreatureId != 0u
                && spawn.CreatureId == soldierEvent.Definition.BossCreatureId;
            if (isBoss && killer is IPlayer player && ReferenceEquals(soldierEvent.Player, player))
            {
                if (soldierEvent.Script.CompleteEvent(soldierEvent, PlayerPathSoldierResult.Success))
                    return true;
            }

            RemoveEventSpawn(soldierEvent, spawn);
            return true;
        }

        private IReadOnlyList<IWorldEntity> SpawnWave(ActiveSoldierEvent soldierEvent, uint waveIndex)
        {
            if (entityFactory == null || owner?.Map == null)
                return [];

            if (soldierEvent.Definition.WaveSpawns == null
                || !soldierEvent.Definition.WaveSpawns.TryGetValue(waveIndex, out IReadOnlyList<SoldierEventSpawnDefinition> spawns))
                return [];

            List<IWorldEntity> spawned = [];
            foreach (SoldierEventSpawnDefinition spawn in spawns)
            {
                INonPlayerEntity entity = entityFactory.CreateEntity<INonPlayerEntity>();
                if (entity == null)
                    continue;

                entity.Initialise(spawn.CreatureId);
                entity.Rotation = spawn.Rotation;
                if (MathF.Abs(spawn.Scale - 1f) > 0.0001f)
                    entity.MovementManager?.SetScale(spawn.Scale);

                entity.CreateFlags |= EntityCreateFlag.Immediate;

                owner.Map.EnqueueAdd(entity, new SoldierHoldoutMapPosition
                {
                    Info = new SoldierHoldoutMapInfo { Entry = owner.Map.Entry },
                    Position = spawn.Position
                });
                soldierEvent.SpawnedEntities.Add(entity);
                activeEventSpawns[entity] = soldierEvent;
                spawned.Add(entity);
            }

            return spawned;
        }

        private void RemovePreExistingSpawnedCreatures()
        {
            SoldierEventDefinition definition = Definition;
            if (definition.PreExistingSpawnCleanupCreatureIds == null
                || definition.PreExistingSpawnCleanupCreatureIds.Count == 0
                || definition.PreExistingSpawnCleanupRadius <= 0f
                || owner?.Map == null)
                return;

            IReadOnlySet<uint> activeSpawnGuids = activeEvents.Values
                .SelectMany(e => e.SpawnedEntities)
                .Where(e => e.InWorld)
                .Select(e => e.Guid)
                .ToHashSet();

            var searchCheck = new CreatureIdRangeSearchCheck(
                definition.PreExistingSpawnCleanupCreatureIds,
                owner.Position,
                definition.PreExistingSpawnCleanupRadius,
                activeSpawnGuids);

            foreach (IWorldEntity entity in owner.Map.Search(owner.Position, definition.PreExistingSpawnCleanupRadius, searchCheck).ToList())
                if (entity.InWorld)
                    entity.RemoveFromMap();
        }

        private static void CleanupEventSpawns(ActiveSoldierEvent soldierEvent)
        {
            foreach (IWorldEntity entity in soldierEvent.SpawnedEntities.ToList())
                RemoveEventSpawn(soldierEvent, entity);

            soldierEvent.SpawnedEntities.Clear();
        }

        private bool CompleteEvent(ActiveSoldierEvent soldierEvent, PlayerPathSoldierResult reason)
        {
            if (!activeEvents.ContainsKey(soldierEvent.Player))
                return false;

            if (reason == PlayerPathSoldierResult.Success
                && !soldierEvent.Player.PathManager.CompleteMissionByObjectId(soldierEvent.Definition.PathSoldierEventId)
                && !soldierEvent.Player.PathManager.IsMissionCompleteByObjectId(soldierEvent.Definition.PathSoldierEventId))
            {
                return false;
            }

            activeEvents.Remove(soldierEvent.Player);
            CleanupEventSpawns(soldierEvent);

            soldierEvent.Player.Session.EnqueueMessageEncrypted(new ServerPathSoldierHoldoutEnd
            {
                PathSoldierEventId = soldierEvent.Definition.PathSoldierEventId,
                Reason = reason
            });
            return true;
        }

        private static void RemoveEventSpawn(ActiveSoldierEvent soldierEvent, IWorldEntity entity)
        {
            activeEventSpawns.Remove(entity);
            soldierEvent.SpawnedEntities.Remove(entity);

            if (entity.InWorld)
                entity.RemoveFromMap();
        }

        private static void SendStatus(ActiveSoldierEvent soldierEvent, PlayerPathSoldierEventMode mode)
        {
            SoldierEventDefinition definition = soldierEvent.Definition;
            soldierEvent.Player.Session.EnqueueMessageEncrypted(new ServerPathSoldierHoldoutStatus
            {
                PathSoldierEventId = definition.PathSoldierEventId,
                Mode = mode,
                DelayTime = mode == PlayerPathSoldierEventMode.InitialDelay
                    ? definition.InitialSpawnMilliseconds
                    : definition.MaxEventMilliseconds,
                WaveIndex = Math.Min(soldierEvent.NextWaveIndex, definition.WaveIndices.Count - 1),
                StartTimeOffset = mode == PlayerPathSoldierEventMode.InitialDelay
                    ? (int)Math.Min(soldierEvent.ElapsedMilliseconds, int.MaxValue)
                    : (int)Math.Min(soldierEvent.ActiveElapsedMilliseconds, int.MaxValue)
            });
        }

        protected sealed record SoldierEventDefinition(
            ushort PathSoldierEventId,
            int InitialSpawnMilliseconds,
            int MaxTimeBetweenWavesMilliseconds,
            int MaxEventMilliseconds,
            IReadOnlyList<uint> WaveIndices,
            IReadOnlyDictionary<uint, IReadOnlyList<SoldierEventSpawnDefinition>> WaveSpawns = null,
            IReadOnlySet<uint> PreExistingSpawnCleanupCreatureIds = null,
            float PreExistingSpawnCleanupRadius = 0f,
            bool CompleteOnBossKill = false,
            uint BossCreatureId = 0u);

        protected sealed record SoldierEventSpawnDefinition(
            uint CreatureId,
            Vector3 Position,
            Vector3 Rotation,
            float Scale = 1f);

        private sealed class ActiveSoldierEvent
        {
            public ActiveSoldierEvent(SoldierEventDefinition definition, IPlayer player, NorthernWildsSoldierHoldoutEntityScript script)
            {
                Definition = definition;
                Player = player;
                Script = script;
                NextWaveMilliseconds = definition.InitialSpawnMilliseconds;
            }

            public SoldierEventDefinition Definition { get; }
            public IPlayer Player { get; }
            public NorthernWildsSoldierHoldoutEntityScript Script { get; }
            public List<IWorldEntity> SpawnedEntities { get; } = [];
            public double ElapsedMilliseconds { get; set; }
            public double ActiveElapsedMilliseconds => Math.Max(ElapsedMilliseconds - Definition.InitialSpawnMilliseconds, 0d);
            public int NextWaveIndex { get; set; }
            public double NextWaveMilliseconds { get; set; }
            public bool HasActivated { get; set; }
        }
    }

    [ScriptFilterCreatureId(11139u)]
    public class NorthernWildsColdburrowSkeechHoldoutEntityScript : NorthernWildsSoldierHoldoutEntityScript
    {
        public NorthernWildsColdburrowSkeechHoldoutEntityScript()
        {
        }

        public NorthernWildsColdburrowSkeechHoldoutEntityScript(IEntityFactory entityFactory)
            : base(entityFactory)
        {
        }

        // PathSoldierEvent 12: DEFEND: Conquer the Coldburrow Skeech.
        // Creature spawning, survivor health, fail states, and participation rules
        // remain blocked; this lifecycle only covers table/video-backed timing.
        protected override SoldierEventDefinition Definition => new(12, 10000, 45000, 180000, [0u, 1u, 2u, 3u]);
    }

    [ScriptFilterCreatureId(11141u)]
    public class NorthernWildsDominionHoldoutEntityScript : NorthernWildsSoldierHoldoutEntityScript
    {
        public NorthernWildsDominionHoldoutEntityScript()
        {
        }

        public NorthernWildsDominionHoldoutEntityScript(IEntityFactory entityFactory)
            : base(entityFactory)
        {
        }

        // PathSoldierEvent 13: HOLDOUT: Conquer the Dominion.
        protected override SoldierEventDefinition Definition => new(13, 3000, 20000, 60000, [0u, 1u, 2u, 3u, 4u]);
    }

    [ScriptFilterCreatureId(12508u)]
    public class NorthernWildsYetiHoldoutEntityScript : NorthernWildsSoldierHoldoutEntityScript
    {
        private const uint FierceYetiIcefangCreatureId = 36331u;
        private const uint FierceYetiIcefangBossCreatureId = 36335u;
        private const float FierceYetiIcefangBossScale = 1.5f;

        private static readonly IReadOnlyDictionary<uint, IReadOnlyList<SoldierEventSpawnDefinition>> YetiWaveSpawns =
            new Dictionary<uint, IReadOnlyList<SoldierEventSpawnDefinition>>
            {
                [0u] =
                [
                    new(FierceYetiIcefangCreatureId, new Vector3(4160f, -743f, -5526f), Vector3.Zero),
                    new(FierceYetiIcefangCreatureId, new Vector3(4149f, -744f, -5534f), Vector3.Zero)
                ],
                [1u] =
                [
                    new(FierceYetiIcefangCreatureId, new Vector3(4159f, -744f, -5544f), Vector3.Zero),
                    new(FierceYetiIcefangCreatureId, new Vector3(4164f, -744f, -5540f), Vector3.Zero)
                ],
                [2u] =
                [
                    new(FierceYetiIcefangCreatureId, new Vector3(4160f, -743f, -5526f), Vector3.Zero),
                    new(FierceYetiIcefangCreatureId, new Vector3(4149f, -744f, -5534f), Vector3.Zero),
                    new(FierceYetiIcefangCreatureId, new Vector3(4159f, -744f, -5544f), Vector3.Zero)
                ],
                [3u] =
                [
                    new(FierceYetiIcefangBossCreatureId, new Vector3(4157f, -744f, -5536f), Vector3.Zero, FierceYetiIcefangBossScale)
                ]
            };

        public NorthernWildsYetiHoldoutEntityScript()
        {
        }

        public NorthernWildsYetiHoldoutEntityScript(IEntityFactory entityFactory)
            : base(entityFactory)
        {
        }

        // PathSoldierEvent 2: HOLDOUT: Conquer the Yeti.
        protected override SoldierEventDefinition Definition => new(
            2,
            7000,
            45000,
            180000,
            [0u, 1u, 2u, 3u],
            YetiWaveSpawns,
            new HashSet<uint> { FierceYetiIcefangCreatureId, FierceYetiIcefangBossCreatureId },
            40f,
            true,
            FierceYetiIcefangBossCreatureId);
    }

    [ScriptFilterCreatureId(36331u, 36335u)]
    public class NorthernWildsYetiHoldoutSpawnEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnKilled(IUnitEntity killer)
        {
            NorthernWildsSoldierHoldoutEntityScript.TryHandleHoldoutSpawnKilled(owner, killer);
        }
    }
}
