using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;

namespace NexusForever.Script.Template.Event
{
    public class EntitySummonEvent : IEntitySummonEvent
    {
        public IWorldEntity Owner { get; private set; }
        public ICreatureInfo CreatureInfo { get; private set; }

        private Vector3 position;
        private Vector3 rotation;

        /// <summary>
        /// Initialise summon event with supplied owner, creature info, position and rotation.
        /// </summary>
        public void Initialise(IWorldEntity owner, ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation)
        {
            Owner             = owner;
            CreatureInfo      = creatureInfo;
            this.position     = position;
            this.rotation     = rotation;
        }

        /// <summary>
        /// Invoke event.
        /// </summary>
        public void Invoke()
        {
            if (Owner?.Map == null || CreatureInfo == null)
                return;

            Owner.SummonFactory?.Summon(CreatureInfo, position, rotation);
        }
    }
}
