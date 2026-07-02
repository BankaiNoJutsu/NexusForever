using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public class EntitySummonFactory : IEntitySummonFactory
    {
        public uint SummonCount => (uint)summonGuids.Count;

        private IWorldEntity owner;

        private readonly HashSet<uint> summonGuids = [];
        private readonly Dictionary<uint, List<uint>> creatureGuids = [];

        #region Dependency Injection

        private readonly IEntityFactory entityFactory;

        public EntitySummonFactory(
            IEntityFactory entityFactory)
        {
            this.entityFactory = entityFactory;
        }

        #endregion

        /// <summary>
        /// Initialises the factory with the owner of the summons.
        /// </summary>
        public void Initialise(IWorldEntity owner)
        {
            if (this.owner != null)
                throw new InvalidOperationException();

            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Summons an entity of <typeparamref name="T"/> at the supplied position and rotation.
        /// </summary>
        public T Summon<T>(ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation) where T : IWorldEntity
        {
            T entity = entityFactory.CreateEntity<T>();
            Summon(creatureInfo, entity, position, rotation);
            return entity;
        }

        /// <summary>
        /// Summons an entity at the supplied position and rotation.
        /// </summary>
        public IWorldEntity Summon(ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation)
        {
            if (creatureInfo == null)
                throw new ArgumentNullException(nameof(creatureInfo));

            IWorldEntity entity = entityFactory.CreateWorldEntity((EntityType)creatureInfo.Entry.CreationTypeEnum);
            Summon(creatureInfo, entity, position, rotation);
            return entity;
        }

        /// <summary>
        /// Summons an entity of <see cref="EntityType"/> at the supplied position and rotation.
        /// </summary>
        public IWorldEntity Summon(ICreatureInfo creatureInfo, EntityType entityType, Vector3 position, Vector3 rotation)
        {
            IWorldEntity entity = entityFactory.CreateWorldEntity(entityType);
            Summon(creatureInfo, entity, position, rotation);
            return entity;
        }

        private void Summon(ICreatureInfo creatureInfo, IWorldEntity entity, Vector3 position, Vector3 rotation)
        {
            if (owner?.Map == null)
                return;

            if (creatureInfo == null)
                throw new ArgumentNullException(nameof(creatureInfo));

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.Initialise(creatureInfo);
            entity.Rotation     = rotation;
            entity.SummonerGuid = owner.Guid;

            owner.Map.EnqueueAdd(entity, new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = owner.Map.Entry,
                    MapLock = (owner.Map as IMapInstance)?.MapLock
                },
                Position = position
            });
        }

        /// <summary>
        /// Start tracking a summon.
        /// </summary>
        public void TrackSummon(IWorldEntity entity)
        {
            if (entity?.SummonerGuid != owner.Guid)
                throw new ArgumentException(null, nameof(entity));

            summonGuids.Add(entity.Guid);

            if (entity.CreatureId == 0u)
                return;

            if (!creatureGuids.TryGetValue(entity.CreatureId, out List<uint> guids))
            {
                guids = [];
                creatureGuids.Add(entity.CreatureId, guids);
            }

            guids.Add(entity.Guid);
        }

        /// <summary>
        /// Try to resolve an active summoned entity by creature id.
        /// </summary>
        public bool TryGetSummonCreature(uint creatureId, out IWorldEntity entity)
        {
            entity = null;
            foreach (IWorldEntity summon in GetSummonCreatures(creatureId))
            {
                entity = summon;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns active summoned entities with supplied creature id.
        /// </summary>
        public IReadOnlyCollection<IWorldEntity> GetSummonCreatures(uint creatureId)
        {
            var summons = new List<IWorldEntity>();
            if (creatureId == 0u || owner?.Map == null)
                return summons;

            if (!creatureGuids.TryGetValue(creatureId, out List<uint> guids))
                return summons;

            foreach (uint guid in guids.ToList())
            {
                IWorldEntity summon = owner.Map.GetEntity<IWorldEntity>(guid);
                if (summon == null || summon.SummonerGuid != owner.Guid)
                {
                    summonGuids.Remove(guid);
                    guids.Remove(guid);
                    continue;
                }

                summons.Add(summon);
            }

            if (guids.Count == 0)
                creatureGuids.Remove(creatureId);

            return summons;
        }

        /// <summary>
        /// Returns the number of active summoned entities with supplied creature id.
        /// </summary>
        public uint GetSummonCreatureCount(uint creatureId)
        {
            return (uint)GetSummonCreatures(creatureId).Count;
        }

        /// <summary>
        /// Stop tracking a summon.
        /// </summary>
        public void UntrackSummon(IWorldEntity entity)
        {
            if (entity?.SummonerGuid != owner.Guid)
                throw new ArgumentException(null, nameof(entity));

            summonGuids.Remove(entity.Guid);

            if (creatureGuids.TryGetValue(entity.CreatureId, out List<uint> guids))
            {
                guids.Remove(entity.Guid);
                if (guids.Count == 0)
                    creatureGuids.Remove(entity.CreatureId);
            }
        }

        /// <summary>
        /// Unsummon a summoned entity with supplied guid.
        /// </summary>
        public void Unsummon(uint guid)
        {
            if (!summonGuids.Contains(guid) || owner?.Map == null)
                return;

            IWorldEntity summon = owner.Map.GetEntity<IWorldEntity>(guid);
            if (summon == null)
                return;

            UntrackSummon(summon);
            summon.RemoveFromMap();
        }

        /// <summary>
        /// Unsummon all summoned entities of creature id.
        /// </summary>
        public void UnsummonCreature<T>(T creatureId) where T : Enum
        {
            UnsummonCreature(creatureId.As<T, uint>());
        }

        /// <summary>
        /// Unsummon all summoned entities of creature id.
        /// </summary>
        public void UnsummonCreature(uint creatureId)
        {
            if (!creatureGuids.TryGetValue(creatureId, out List<uint> guids))
                return;

            foreach (uint guid in guids.ToList())
                Unsummon(guid);
        }

        /// <summary>
        /// Unsummon all summoned entities.
        /// </summary>
        public void Unsummon()
        {
            foreach (uint guid in summonGuids.ToList())
                Unsummon(guid);
        }
    }
}
