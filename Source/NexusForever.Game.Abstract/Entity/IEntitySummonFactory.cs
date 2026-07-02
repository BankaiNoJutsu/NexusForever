using System.Numerics;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IEntitySummonFactory
    {
        uint SummonCount { get; }

        /// <summary>
        /// Initialises the factory with the owner of the summons.
        /// </summary>
        void Initialise(IWorldEntity owner);

        /// <summary>
        /// Summons an entity of <typeparamref name="T"/> at the supplied position and rotation.
        /// </summary>
        T Summon<T>(ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation) where T : IWorldEntity;

        /// <summary>
        /// Summons an entity at the supplied position and rotation.
        /// </summary>
        IWorldEntity Summon(ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation);

        /// <summary>
        /// Summons an entity of <see cref="EntityType"/> at the supplied position and rotation.
        /// </summary>
        IWorldEntity Summon(ICreatureInfo creatureInfo, EntityType entityType, Vector3 position, Vector3 rotation);

        /// <summary>
        /// Start tracking a summon.
        /// </summary>
        void TrackSummon(IWorldEntity entity);

        /// <summary>
        /// Try to resolve an active summoned entity by creature id.
        /// </summary>
        bool TryGetSummonCreature(uint creatureId, out IWorldEntity entity);

        /// <summary>
        /// Returns active summoned entities with supplied creature id.
        /// </summary>
        IReadOnlyCollection<IWorldEntity> GetSummonCreatures(uint creatureId);

        /// <summary>
        /// Returns the number of active summoned entities with supplied creature id.
        /// </summary>
        uint GetSummonCreatureCount(uint creatureId);

        /// <summary>
        /// Stop tracking a summon.
        /// </summary>
        void UntrackSummon(IWorldEntity entity);

        /// <summary>
        /// Unsummon a summoned entity with supplied guid.
        /// </summary>
        void Unsummon(uint guid);

        /// <summary>
        /// Unsummon all summoned entities of creature id.
        /// </summary>
        void UnsummonCreature<T>(T creatureId) where T : Enum;

        /// <summary>
        /// Unsummon all summoned entities of creature id.
        /// </summary>
        void UnsummonCreature(uint creatureId);

        /// <summary>
        /// Unsummon all summoned entities.
        /// </summary>
        void Unsummon();
    }
}
