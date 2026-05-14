using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;

namespace NexusForever.Script.Template.Event
{
    public interface IEntitySummonEvent : IScriptEvent
    {
        /// <summary>
        /// Entity that owns this summon event.
        /// </summary>
        IWorldEntity Owner { get; }

        /// <summary>
        /// Creature info used to initialise the summoned entity.
        /// </summary>
        ICreatureInfo CreatureInfo { get; }

        /// <summary>
        /// Initialise summon event with supplied owner, creature info, position and rotation.
        /// </summary>
        void Initialise(IWorldEntity owner, ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation);
    }
}
