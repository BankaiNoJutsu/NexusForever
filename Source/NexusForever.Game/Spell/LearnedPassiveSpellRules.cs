using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    public static class LearnedPassiveSpellRules
    {
        // Classifications belong to one table load; a missing/replaced table
        // must not poison another runtime's learned-passive index.
        private static readonly ConditionalWeakTable<GameTable<Spell4EffectsEntry>, HashSet<uint>> cache = new();

        public static bool IsLearnedPassive(uint spell4Id, IGameTableManager tables)
        {
            Spell4Entry spell = tables?.Spell4?.GetEntry(spell4Id);
            GameTable<Spell4EffectsEntry> effects = tables?.Spell4Effects;
            if (spell == null || spell.SpellDuration != 0u || effects == null)
                return false;
            return cache.GetValue(effects, BuildIndex).Contains(spell4Id);
        }

        private static HashSet<uint> BuildIndex(GameTable<Spell4EffectsEntry> table)
        {
            HashSet<uint> taught = table.Entries.Where(e => e.EffectType == SpellEffectType.AddSpell)
                .Select(e => e.DataBits00).ToHashSet();
            return table.Entries.Where(e => taught.Contains(e.SpellId))
                .GroupBy(e => e.SpellId)
                .Where(g => g.All(e => e.EffectType == SpellEffectType.UnitPropertyModifier
                    && e.DurationTime == 0u && e.DelayTime == 0u && e.TickTime == 0u))
                .Select(g => g.Key).ToHashSet();
        }
    }
}
