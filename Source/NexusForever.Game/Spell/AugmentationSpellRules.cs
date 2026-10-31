using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    public static class AugmentationSpellRules
    {
        private static readonly ConditionalWeakTable<GameTable<Spell4EffectsEntry>, HashSet<uint>> cache = new();

        // Delayed, channeled, timed, periodic, proc and mixed-effect AMP spells
        // need a broader effect-lifetime owner before they can be reconciled safely.
        public static bool IsSupported(uint spell4Id, IGameTableManager tables)
        {
            Spell4Entry spell = tables?.Spell4?.GetEntry(spell4Id);
            var effects = tables?.Spell4Effects;
            return spell != null && spell.CastTime == 0 && spell.SpellDuration == 0 && HasSupportedStackGroup(spell, tables)
                && spell.ChannelMaxTime == 0 && spell.ChannelInitialDelay == 0 && spell.ChannelPulseTime == 0
                && effects != null && cache.GetValue(effects, BuildIndex).Contains(spell4Id);
        }

        private static bool HasSupportedStackGroup(Spell4Entry spell, IGameTableManager tables)
        {
            if (spell.Spell4StackGroupId == 0)
                return true;

            // Build16042's permanent property AMPs and held cooldown AMP use
            // this exact uncapped group. Do not infer other arbitration modes.
            Spell4StackGroupEntry group = tables.Spell4StackGroup?.GetEntry(spell.Spell4StackGroupId);
            return spell.Spell4StackGroupId == 2 && group is { Id: 2, StackCap: 0, StackTypeEnum: 4 };
        }

        private static HashSet<uint> BuildIndex(GameTable<Spell4EffectsEntry> table)
        {
            return table.Entries.GroupBy(e => e.SpellId)
                .Where(g => (g.All(e => e.EffectType == SpellEffectType.UnitPropertyModifier)
                        || g.All(IsSupportedCooldownEffect))
                    && g.All(e => e.DurationTime == 0 && e.DelayTime == 0 && e.TickTime == 0
                    && (e.TargetFlags & ~(uint)(SpellEffectTargetFlags.Caster | SpellEffectTargetFlags.Target)) == 0
                    && e.PrerequisiteIdCasterPersistence == 0 && e.PrerequisiteIdTargetPersistence == 0
                    && e.PrerequisiteIdTargetSuspend == 0))
                .Select(g => g.Key).ToHashSet();
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
