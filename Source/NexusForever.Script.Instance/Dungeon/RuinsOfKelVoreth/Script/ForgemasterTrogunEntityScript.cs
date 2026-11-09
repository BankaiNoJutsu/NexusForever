using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// Forgemaster Trogun encounter phase as far as the evidence agrees.
    /// </summary>
    public enum TrogunPhase
    {
        Opening,
        ForgeRetreat
    }

    /// <summary>
    /// WIP-guessed from LaughingWS worlddb SQL: the branch only maps this boss script
    /// to the Ruins of Kel Voreth objective. Combat mechanics remain blocked.
    /// Phase tracking is a disabled placeholder: the 50% forge retreat is reported by four independent
    /// clips (agent-read, Observed, see INSTANCE_KELVORETH_SECONDSOURCE_PACKET_20261109.md), but nothing
    /// acts on it, so <see cref="PhaseTrackingEnabled"/> defaults to false and the phase has no gameplay effect.
    /// </summary>
    [ScriptFilterScriptName("ForgemasterTrogunEntityScript")]
    public class ForgemasterTrogunEntityScript : PublicEventObjectiveCreditEntityScript, IUnitScript
    {
        /// <summary>
        /// Health fraction at which Trogun is reported to run back to the forge.
        /// </summary>
        public const float ForgeRetreatHealthFraction = 0.5f;

        /// <summary>
        /// Disabled by default until the phase is wired to behaviour that has evidence.
        /// </summary>
        public bool PhaseTrackingEnabled { get; set; }

        public TrogunPhase Phase { get; private set; } = TrogunPhase.Opening;

        private readonly EncounterHealthPhases healthPhases = new(ForgeRetreatHealthFraction);

        public ForgemasterTrogunEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatForgemasterTrogun)
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
                Phase = TrogunPhase.ForgeRetreat;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> leaves combat.
        /// </summary>
        public void OnLeaveCombat()
        {
            healthPhases.Reset();
            Phase = TrogunPhase.Opening;
        }
    }
}
