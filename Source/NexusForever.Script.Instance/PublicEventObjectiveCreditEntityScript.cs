using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Shared;

namespace NexusForever.Script.Instance
{
    public abstract class PublicEventObjectiveCreditEntityScript : CombatAI
    {
        private readonly uint objectiveId;

        protected PublicEventObjectiveCreditEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager,
            uint objectiveId)
            : base(spellParametersFactory, gameTableManager)
        {
            this.objectiveId = objectiveId;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public override void OnDeath()
        {
            entity.Map.PublicEventManager.UpdateObjective(objectiveId, 1);
        }
    }
}
