using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template;
using NexusForever.Shared;

namespace NexusForever.Script.Instance
{
    /// <summary>
    /// Base for provisional boss behaviour: casts the spells a <see cref="ProvisionalEncounterPlan"/> says are due.
    /// Disabled by default and inert until <see cref="Enabled"/> is set. The delays, intervals and phase
    /// thresholds in a plan are estimates from observed clips and tables, not retail values; this behaviour
    /// is never retail parity. The script does not credit objectives, so it can sit beside a credit script.
    /// </summary>
    public abstract class ProvisionalEncounterEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        /// <summary>
        /// Process-wide default for new scripts; false unless an operator opts in.
        /// </summary>
        public static bool EnabledByDefault { get; set; }

        public bool Enabled
        {
            get => runner.Enabled;
            set => runner.Enabled = value;
        }

        protected ICreatureEntity entity;

        private readonly ProvisionalEncounterRunner runner;
        private readonly IFactory<ISpellParameters> spellParametersFactory;

        protected ProvisionalEncounterEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            ProvisionalEncounterPlan plan)
        {
            this.spellParametersFactory = spellParametersFactory;
            runner = new ProvisionalEncounterRunner(plan) { Enabled = EnabledByDefault };
        }

        public virtual void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (entity == null)
                return;

            foreach (ProvisionalCast cast in runner.Update(lastTick, entity.Health, entity.MaxHealth, entity.InCombat))
            {
                ISpellParameters parameters = spellParametersFactory.Resolve();
                if (cast.TargetCurrentTarget && entity.TargetGuid.HasValue)
                    parameters.PrimaryTargetId = entity.TargetGuid.Value;

                entity.CastSpell(cast.SpellId, parameters);
            }
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> leaves combat.
        /// </summary>
        public void OnLeaveCombat()
        {
            runner.Reset();
        }
    }
}
