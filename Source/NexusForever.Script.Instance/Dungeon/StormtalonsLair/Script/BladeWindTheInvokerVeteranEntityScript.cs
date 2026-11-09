using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Encounter phase of Blade-Wind the Invoker as far as the evidence agrees.
    /// Phase 3 starts when all four disciples are finished, not by health, so it is not tracked here.
    /// </summary>
    public enum BladeWindPhase
    {
        One,
        Two
    }

    /// <summary>
    /// Build 16042 maps public-event objective 312 to TargetGroup 2585,
    /// whose Creature2 members are Blade-Wind the Invoker 17160 and 33405.
    /// Phase tracking is a disabled placeholder: the 60% phase 2 threshold comes from one 2015
    /// guide and a 2014 clip (Observed, two clips read by the lead, see INSTANCE_THREE_BOSS_VERIFICATION_20261109.md),
    /// so <see cref="PhaseTrackingEnabled"/> defaults to false and the phase has no gameplay effect.
    /// </summary>
    [ScriptFilterCreatureId(17160u, 33405u)]
    public class BladeWindTheInvokerVeteranEntityScript : PublicEventObjectiveCreditEntityScript, IUnitScript
    {
        /// <summary>
        /// Single-source health fraction at which phase 2 is reported to begin.
        /// </summary>
        public const float PhaseTwoHealthFraction = 0.6f;

        /// <summary>
        /// Disabled by default until a second source agrees with the threshold.
        /// </summary>
        public bool PhaseTrackingEnabled { get; set; }

        public BladeWindPhase Phase { get; private set; } = BladeWindPhase.One;

        private readonly EncounterHealthPhases healthPhases = new(PhaseTwoHealthFraction);

        public BladeWindTheInvokerVeteranEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatBladeWindTheInvoker)
        {
        }

        /// <summary>
        /// Invoked when health is changed by source <see cref="IUnitEntity"/>.
        /// </summary>
        public void OnHealthChange(IUnitEntity source, uint amount, DamageType? type)
        {
            if (!PhaseTrackingEnabled || entity == null)
                return;

            if (healthPhases.Update(entity.Health, entity.MaxHealth).Count > 0)
                Phase = BladeWindPhase.Two;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> leaves combat.
        /// </summary>
        public void OnLeaveCombat()
        {
            healthPhases.Reset();
            Phase = BladeWindPhase.One;
        }
    }
}
