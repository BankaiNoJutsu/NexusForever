using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Shared;

namespace NexusForever.Script.Instance
{
    public abstract class PublicEventObjectiveCreditEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        protected ICreatureEntity entity;

        private readonly uint[] objectiveIds;
        private bool credited;

        protected PublicEventObjectiveCreditEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager,
            params uint[] objectiveIds)
        {
            _ = spellParametersFactory;
            _ = gameTableManager;
            if (objectiveIds.Length == 0)
                throw new ArgumentException("At least one public event objective id is required.", nameof(objectiveIds));

            this.objectiveIds = objectiveIds;
        }

        public virtual void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public virtual void OnDeath()
        {
            if (credited)
                return;

            credited = true;
            foreach (uint objectiveId in objectiveIds)
                entity.Map.PublicEventManager.UpdateObjective(objectiveId, 1);
        }
    }
}
