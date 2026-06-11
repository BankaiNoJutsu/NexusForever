using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Configuration.Model;
using NexusForever.Shared.Configuration;
using NLog;

namespace NexusForever.Game.Map
{
    public sealed class EntityCacheManager : IEntityCacheManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<ushort, IEntityCache> entityCaches = new();
        private readonly IDatabaseManager databaseManager;
        private readonly ISharedConfiguration sharedConfiguration;

        public EntityCacheManager(
            IDatabaseManager databaseManager = null,
            ISharedConfiguration sharedConfiguration = null)
        {
            this.databaseManager     = databaseManager;
            this.sharedConfiguration = sharedConfiguration;
        }

        public void Initialise()
        {
            log.Info("Caching map spawns...");

            MapConfig mapConfig = GetMapConfig();
            List<ushort> precachedMapSpawns = mapConfig.PrecacheMapSpawns ?? mapConfig.PrecacheBaseMaps;
            if (precachedMapSpawns == null)
                return;

            foreach (ushort worldId in precachedMapSpawns)
                GetEntityCache(worldId);
        }

        /// <summary>
        /// Returns an existing <see cref="IEntityCache"/> for the supplied world, if it doesn't exist a new one will be created from the database.
        /// </summary>
        public IEntityCache GetEntityCache(ushort worldId)
        {
            if (entityCaches.TryGetValue(worldId, out IEntityCache entityCache))
                return entityCache;

            return LoadEntityCache(worldId);
        }

        private IEntityCache LoadEntityCache(ushort worldId)
        {
            if (databaseManager == null)
                throw new InvalidOperationException("EntityCacheManager requires an IDatabaseManager to load map spawns.");

            var entityCache = new EntityCache(databaseManager.GetDatabase<WorldDatabase>().GetEntities(worldId));
            entityCaches.Add(worldId, entityCache);

            log.Trace($"Initialised {entityCache.EntityCount} spawns on {entityCache.GridCount} grids for world {worldId}.");
            return entityCache;
        }

        private MapConfig GetMapConfig()
        {
            if (sharedConfiguration == null)
                throw new InvalidOperationException("EntityCacheManager requires an ISharedConfiguration to initialise map spawn caches.");

            return sharedConfiguration.Get<MapConfig>();
        }
    }
}
