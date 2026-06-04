using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEventEntityFactory : IPublicEventEntityFactory
    {
        private IPublicEvent publicEvent;

        private Dictionary<uint, List<EntityModel>> entityModels;
        private readonly HashSet<IGridEntity> entities = [];

        #region Dependency Injection

        private readonly ILogger<PublicEventEntityFactory> log;

        private readonly IDatabaseManager databaseManager;
        private readonly IEntityFactory entityFactory;
        private readonly ICreatureInfoManager creatureInfoManager;

        public PublicEventEntityFactory(
            ILogger<PublicEventEntityFactory> log,
            IDatabaseManager databaseManager,
            IEntityFactory entityFactory,
            ICreatureInfoManager creatureInfoManager)
        {
            this.log                 = log;
            this.databaseManager     = databaseManager;
            this.entityFactory       = entityFactory;
            this.creatureInfoManager = creatureInfoManager;
        }

        #endregion

        /// <summary>
        /// Initalise the <see cref="PublicEventEntityFactory"/> with the specified <see cref="IPublicEvent"/>.
        /// </summary>
        public void Initialise(IPublicEvent publicEvent)
        {
            if (this.publicEvent != null)
                throw new InvalidOperationException();

            this.publicEvent = publicEvent;

            entityModels = databaseManager.GetDatabase<WorldDatabase>()
                .GetEntitiesPublicEvent(publicEvent.Id)
                .GroupBy(e => e.EntityEvent.Phase)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Spawn entities for the specified <see cref="IPublicEvent"/> phase.
        /// </summary>
        public void SpawnEntities(uint phase)
        {
            if (!entityModels.TryGetValue(phase, out List<EntityModel> models))
                return;

            foreach (EntityModel model in models)
            {
                if (model.Type == EntityType.Player)
                {
                    log.LogWarning("Skipping public-event player entity {EntityId} for event {PublicEventId} phase {Phase}; players are created from character state.", model.Id, publicEvent.Id, phase);
                    continue;
                }

                IWorldEntity entity = entityFactory.CreateWorldEntity(model.Type);
                ICreatureInfo creatureInfo = GetCreatureInfo(model, phase);
                if (creatureInfo != null)
                    entity.Initialise(creatureInfo, model);
                else
                    entity.Initialise(model);
                entity.Rotation = new Vector3(model.Rx, model.Ry, model.Rz);

                entities.Add(entity);

                publicEvent.Map.EnqueueAdd(entity, new MapPosition
                {
                    Info = new MapInfo
                    {
                        Entry = publicEvent.Map.Entry
                    },
                    Position = new Vector3(model.X, model.Y, model.Z)
                });
            }

            log.LogTrace($"Spawned entities for public event {publicEvent.Id} phase {phase}.");
        }

        private ICreatureInfo GetCreatureInfo(EntityModel model, uint phase)
        {
            if (model.Creature == 0u)
                return null;

            ICreatureInfo creatureInfo = creatureInfoManager.GetCreatureInfo(model.Creature);
            if (creatureInfo == null)
                throw new InvalidOperationException($"Missing creature info for creature {model.Creature} while spawning public event {publicEvent.Id} entity {model.Id} in phase {phase}.");

            return creatureInfo;
        }

        /// <summary>
        /// Remove all entities spawned for the <see cref="IPublicEvent"/>.
        /// </summary>
        public void RemoveEntities()
        {
            foreach (IGridEntity entity in entities)
                if (entity.InWorld)
                    publicEvent.Map.EnqueueRemove(entity);

            entities.Clear();

            log.LogTrace($"Removed entities from public event {publicEvent.Id}.");
        }

        /// <summary>
        /// Create a new <see cref="IGridEntity"/> that belongs to the <see cref="IPublicEvent"/>.
        /// </summary>
        public T CreateEntity<T>() where T : IGridEntity
        {
            T entity = entityFactory.CreateEntity<T>();
            entities.Add(entity);
            return entity;
        }
    }
}
