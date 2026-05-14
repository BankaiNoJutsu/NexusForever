using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Entity.Creature
{
    public class CreatureInfoManager : ICreatureInfoManager
    {
        private ImmutableDictionary<uint, ICreatureInfo> creatureInfo = ImmutableDictionary<uint, ICreatureInfo>.Empty;

        #region Dependency Injection

        private readonly ILogger<CreatureInfoManager> log;

        private readonly IGameTableManager gameTableManager;
        private readonly IFactory<ICreatureInfo> creatureInfoFactory;
        private readonly IDatabaseManager databaseManager;

        public CreatureInfoManager(
            ILogger<CreatureInfoManager> log,
            IGameTableManager gameTableManager,
            IFactory<ICreatureInfo> creatureInfoFactory,
            IDatabaseManager databaseManager)
        {
            this.log = log;

            this.gameTableManager    = gameTableManager;
            this.creatureInfoFactory = creatureInfoFactory;
            this.databaseManager     = databaseManager;
        }

        #endregion

        public void Initialise()
        {
            log.LogInformation("Initialising creature information templates...");

            WorldDatabase database = databaseManager.GetDatabase<WorldDatabase>();
            Dictionary<uint, List<CreatureInfoPropertyModel>> creatureInfoProperties = database.GetCreatureInfoProperties()
                .GroupBy(p => p.CreatureId)
                .ToDictionary(p => p.Key, p => p.ToList());

            Dictionary<uint, List<CreatureInfoStatModel>> creatureInfoStats = database.GetCreatureInfoStats()
                .GroupBy(p => p.CreatureId)
                .ToDictionary(p => p.Key, p => p.ToList());

            var builder = ImmutableDictionary.CreateBuilder<uint, ICreatureInfo>();
            foreach (Creature2Entry entry in gameTableManager.Creature2.Entries)
            {
                ICreatureInfo info = creatureInfoFactory.Resolve();
                info.Initialise(entry);
                info.InitialiseOverrides(
                    creatureInfoProperties.TryGetValue(entry.Id, out List<CreatureInfoPropertyModel> properties) ? properties : Enumerable.Empty<CreatureInfoPropertyModel>(),
                    creatureInfoStats.TryGetValue(entry.Id, out List<CreatureInfoStatModel> stats) ? stats : Enumerable.Empty<CreatureInfoStatModel>());

                builder.Add(entry.Id, info);
            }

            creatureInfo = builder.ToImmutable();

            log.LogInformation("Initialised {Count} creature information templates.", creatureInfo.Count);
        }

        /// <summary>
        /// Get the <see cref="ICreatureInfo"/> by creature id.
        /// </summary>
        public ICreatureInfo GetCreatureInfo<T>(T creatureId) where T : Enum
        {
            return GetCreatureInfo(creatureId.As<T, uint>());
        }

        /// <summary>
        /// Get the <see cref="ICreatureInfo"/> by creature id.
        /// </summary>
        public ICreatureInfo GetCreatureInfo(uint creatureId)
        {
            return creatureInfo.TryGetValue(creatureId, out ICreatureInfo info) ? info : null;
        }
    }
}
