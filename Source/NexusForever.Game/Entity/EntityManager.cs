using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Shared;
using NLog;

namespace NexusForever.Game.Entity
{
    public sealed class EntityManager : IEntityManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<ImmutableDictionary<Stat, StatAttribute>> statAttributeStore = new(BuildStatAttributes);

        private readonly IDatabaseManager databaseManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IMapIOManager mapIOManager;

        public EntityManager(
            IDatabaseManager databaseManager = null,
            IGameTableManager gameTableManager = null,
            IMapIOManager mapIOManager = null)
        {
            this.databaseManager  = databaseManager;
            this.gameTableManager = gameTableManager;
            this.mapIOManager     = mapIOManager;
        }

        public void Initialise()
        {
            _ = statAttributeStore.Value;

            CalculateEntityAreaData();
        }

        private static ImmutableDictionary<Stat, StatAttribute> BuildStatAttributes()
        {
            var builder = ImmutableDictionary.CreateBuilder<Stat, StatAttribute>();

            foreach (FieldInfo field in typeof(Stat).GetFields())
            {
                StatAttribute attribute = field.GetCustomAttribute<StatAttribute>();
                if (attribute == null)
                    continue;

                Stat stat = (Stat)field.GetValue(null);
                builder.Add(stat, attribute);
            }

            return builder.ToImmutable();
        }

        [Conditional("DEBUG")]
        private void CalculateEntityAreaData()
        {
            log.Info("Calculating area information for entities...");

            var mapFiles = new Dictionary<ushort, MapFile>();
            var entities = new HashSet<EntityModel>();

            if (databaseManager == null)
                throw new InvalidOperationException("EntityManager requires an IDatabaseManager to calculate entity area data.");
            if (gameTableManager == null)
                throw new InvalidOperationException("EntityManager requires an IGameTableManager to calculate entity area data.");
            if (mapIOManager == null)
                throw new InvalidOperationException("EntityManager requires an IMapIOManager to calculate entity area data.");

            foreach (EntityModel model in databaseManager.GetDatabase<WorldDatabase>().GetEntitiesWithoutArea())
            {
                entities.Add(model);

                if (!mapFiles.TryGetValue(model.World, out MapFile mapFile))
                {
                    WorldEntry entry = gameTableManager.World.GetEntry(model.World);
                    mapFile = mapIOManager.GetBaseMap(entry.AssetPath);
                    mapFiles.Add(model.World, mapFile);
                }

                uint? worldAreaId = mapFile.GetWorldAreaId(new Vector3(model.X, model.Y, model.Z));
                if (!worldAreaId.HasValue)
                    continue;

                model.Area = (ushort)worldAreaId;

                log.Info($"Calculated area {worldAreaId} for entity {model.Id}.");
            }

            databaseManager.GetDatabase<WorldDatabase>().UpdateEntities(entities);

            log.Info($"Calculated area information for {entities.Count} {(entities.Count == 1 ? "entity" : "entities")}.");
        }

        /// <summary>
        /// Return <see cref="StatAttribute"/> for supplied <see cref="Stat"/>.
        /// </summary>
        public StatAttribute GetStatAttribute(Stat stat)
        {
            return GetStatAttributeFor(stat);
        }

        /// <summary>
        /// Return <see cref="StatAttribute"/> for supplied <see cref="Stat"/>.
        /// </summary>
        public static StatAttribute GetStatAttributeFor(Stat stat)
        {
            return statAttributeStore.Value.TryGetValue(stat, out StatAttribute value) ? value : null;
        }
    }
}
