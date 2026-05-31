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

        private readonly uint objectiveId;

        protected PublicEventObjectiveCreditEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager,
            uint objectiveId)
        {
            _ = spellParametersFactory;
            _ = gameTableManager;
            this.objectiveId = objectiveId;
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
            entity.Map.PublicEventManager.UpdateObjective(objectiveId, 1);
        }
    }
}
