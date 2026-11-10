using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    public static class AugmentationSpellRules
    {
        private static readonly ConditionalWeakTable<GameTable<Spell4EffectsEntry>, Dictionary<uint, uint[]>> cache = new();

        // Timed, delayed, mixed and filtered proc AMPs remain outside this owned
        // lifecycle. Periodic AMPs whose rows all tick on the caster are owned as
        // a running spell that is ended when the AMP is retired.
        public static bool IsSupported(uint spell4Id, IGameTableManager tables)
        {
            Spell4Entry spell = tables?.Spell4?.GetEntry(spell4Id);
            var effects = tables?.Spell4Effects;
            return spell != null && spell.CastTime == 0 && spell.SpellDuration == 0 && HasSupportedStackGroup(spell, tables)
                && spell.ChannelMaxTime == 0 && spell.ChannelInitialDelay == 0 && spell.ChannelPulseTime == 0
                && effects != null && cache.GetValue(effects, BuildIndex).TryGetValue(spell4Id, out uint[] triggers)
                && (triggers.Length == 0 || spell.Spell4StackGroupId == 2)
                && triggers.All(id => tables.Spell4.GetEntry(id) != null);
        }

        private static bool HasSupportedStackGroup(Spell4Entry spell, IGameTableManager tables)
        {
            if (spell.Spell4StackGroupId == 0)
                return true;

            // Build16042's supported permanent AMPs use this exact uncapped
            // group. Do not infer other arbitration modes.
            Spell4StackGroupEntry group = tables.Spell4StackGroup?.GetEntry(spell.Spell4StackGroupId);
            return spell.Spell4StackGroupId == 2 && group is { Id: 2, StackCap: 0, StackTypeEnum: 4 };
        }

        private static Dictionary<uint, uint[]> BuildIndex(GameTable<Spell4EffectsEntry> table)
        {
            return table.Entries.GroupBy(e => e.SpellId)
                .Where(g => ((g.All(e => e.EffectType == SpellEffectType.UnitPropertyModifier)
                            || g.All(IsSupportedCooldownEffect)
                            || g.All(IsSupportedProcEffect))
                        && g.All(e => e.DelayTime == 0 && e.TickTime == 0)
                        || g.All(IsSupportedPeriodicEffect))
                    && g.All(e => e.DurationTime == 0
                    && (e.TargetFlags & ~(uint)(SpellEffectTargetFlags.Caster | SpellEffectTargetFlags.Target)) == 0
                    && e.PrerequisiteIdCasterPersistence == 0 && e.PrerequisiteIdTargetPersistence == 0
                    && e.PrerequisiteIdTargetSuspend == 0))
                .ToDictionary(g => g.Key, g => g.Where(e => e.EffectType == SpellEffectType.Proc)
                    .Select(e => e.DataBits01).Distinct().ToArray());
        }

        /// <summary>
        /// A periodic AMP: every row ticks on the caster for as long as the AMP is slotted (effect flag 0x2, no duration of its own) and leaves no state behind.
        /// </summary>
        /// <remarks>
        /// Retail: Stalker "Stealth Regen" (Spell4 59394, Proxy tick 1000 ms, "While in Stealth you regenerate 5 health per second"), Engineer Health Regeneration I to III
        /// (57392 to 57394, VitalModifier tick 2500 ms). The spell runs until the AMP is retired; see Spell.GetEffectPulseDuration.
        /// </remarks>
        public static bool IsPeriodic(uint spell4Id, IGameTableManager tables)
        {
            return IsSupported(spell4Id, tables)
                && tables.Spell4Effects.Entries.Any(e => e.SpellId == spell4Id && e.TickTime != 0);
        }

        private static bool IsSupportedPeriodicEffect(Spell4EffectsEntry effect)
        {
            return effect.EffectType is SpellEffectType.Proxy or SpellEffectType.VitalModifier
                && effect.Flags == 2 && effect.TargetFlags == (uint)SpellEffectTargetFlags.Caster
                && effect.TickTime != 0 && (effect.DelayTime == 0 || effect.DelayTime == effect.TickTime);
        }

        private static bool IsSupportedProcEffect(Spell4EffectsEntry effect)
        {
            float chance = BitConverter.UInt32BitsToSingle(effect.DataBits02);
            return effect.EffectType == SpellEffectType.Proc && effect.Flags == 2
                && effect.TargetFlags == (uint)SpellEffectTargetFlags.Caster
                && float.IsFinite(chance) && chance > 0 && chance <= 1
                && ProcDispatchEvidenceBoundary.Describe(effect.DataBits00, effect.DataBits03).IsConservativelyDispatchSupported
                // Match the established unfiltered proc fixture. Other count,
                // spell/condition filters and tails have no interpreted owner.
                && effect.DataBits05 == 1 && effect.DataBits06 == 0 && effect.DataBits07 == 0
                && effect.DataBits08 == 0 && effect.DataBits09 == 0;
        }

        private static bool IsSupportedCooldownEffect(Spell4EffectsEntry effect)
        {
            float magnitude = BitConverter.UInt32BitsToSingle(effect.DataBits03);
            return effect.EffectType == SpellEffectType.ModifySpellCooldown
                && (effect.Flags & 2u) != 0 && effect.TargetFlags == (uint)SpellEffectTargetFlags.Caster
                && effect.DataBits00 <= 3 && effect.DataBits01 != 0 && effect.DataBits02 <= 3
                && float.IsFinite(magnitude) && (effect.DataBits02 == 2 || magnitude >= 0);
        }
    }
}
