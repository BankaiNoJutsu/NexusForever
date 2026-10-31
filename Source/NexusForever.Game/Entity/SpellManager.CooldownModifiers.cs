using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public partial class SpellManager
    {
        private List<SpellCooldownModifier> cooldownModifiers = [];

        public IReadOnlyCollection<SpellCooldownModifier> GetSpellCooldownModifiers()
            => cooldownModifiers?.ToArray() ?? [];

        public bool RemoveSpellCooldownModifier(uint effectId)
            => cooldownModifiers?.RemoveAll(m => m.EffectId == effectId) > 0;

        public bool ApplySpellCooldownModifier(SpellCooldownModifier modifier, bool held)
        {
            if (modifier == null || modifier.Key == 0 || modifier.Scope > 3 || modifier.Mode > 3
                || !float.IsFinite(modifier.Magnitude)
                || modifier.Mode != 2 && modifier.Magnitude < 0)
                return false;
            cooldownModifiers ??= [];
            if (cooldownModifiers.Any(m => m.EffectId == modifier.EffectId))
                return false;

            foreach (var pair in activeCooldowns.ToArray())
            {
                var active = pair.Value;
                // The native re-arm walk matches the originating spell wrapper;
                // global nodes carry no wrapper and are not re-armed here.
                var entry = gameTableManager?.Spell4?.GetEntry(active.Spell4Id);
                if (active.Type == GlobalCooldownType || active.TimeRemaining <= 0 || !MatchesCooldown(modifier, entry))
                    continue;
                active.TimeRemaining = ApplyCooldownOperation(modifier, active.TimeRemaining, active.TimeRemaining);
                if (active.TimeRemaining <= 0)
                    activeCooldowns.Remove(pair.Key);
                // The effect reaches the client through SpellGo and re-arms its
                // timer there; a second cooldown packet would apply it twice.
            }
            if (held)
                cooldownModifiers.Add(modifier);
            return true;
        }

        private bool MatchesCooldown(SpellCooldownModifier modifier, Spell4Entry entry)
        {
            if (entry == null)
                return false;
            return modifier.Scope switch
            {
                0 => entry.Spell4BaseIdBaseSpell == modifier.Key,
                1 => entry.Id == modifier.Key,
                2 => EnumerateSpell4GroupList(entry.Spell4GroupListId).Contains(modifier.Key),
                3 => EnumerateSpellCooldownNodeIds(entry).Contains(modifier.Key),
                _ => false
            };
        }

        private double FoldCooldownModifiers(Spell4Entry entry, double cooldown)
        {
            double value = cooldown;
            foreach (var modifier in cooldownModifiers ?? [])
                if (MatchesCooldown(modifier, entry))
                    value = ApplyCooldownOperation(modifier, cooldown, value);
            return value;
        }

        private double FoldGlobalCooldownModifiers(uint cooldownId, double cooldown)
        {
            double value = cooldown;
            foreach (var modifier in cooldownModifiers ?? [])
                if (modifier.Scope == 3 && modifier.Key == cooldownId)
                    value = ApplyCooldownOperation(modifier, cooldown, value);
            return value;
        }

        private static double ApplyCooldownOperation(SpellCooldownModifier modifier, double input, double accumulator)
        {
            // Client arithmetic truncates each step to whole milliseconds. Mode0
            // uses the original input, even after earlier records changed the accumulator.
            double inputMs = Math.Clamp(Math.Truncate(input * 1000d), 0d, int.MaxValue);
            double currentMs = Math.Clamp(Math.Truncate(accumulator * 1000d), 0d, int.MaxValue);
            double result = modifier.Mode switch
            {
                0 => Math.Min(Math.Truncate((float)inputMs * modifier.Magnitude), inputMs),
                1 => Math.Truncate((float)currentMs * modifier.Magnitude),
                2 => currentMs + Math.Truncate(modifier.Magnitude),
                3 => Math.Truncate(modifier.Magnitude),
                _ => currentMs
            };
            return Math.Clamp(result, 0d, int.MaxValue) / 1000d;
        }

        private double ScaleSpellCooldown(Spell4Entry entry, double cooldown)
        {
            if (entry == null || !new[] { entry.Spell4TagId00, entry.Spell4TagId01, entry.Spell4TagId02,
                    entry.Spell4TagId03, entry.Spell4TagId04 }.Any(id => id is 1 or 2 or 7))
                return cooldown;
            float multiplier = player.GetPropertyValue(Property.CooldownReductionModifier);
            if (!float.IsFinite(multiplier) || multiplier < 0)
                return cooldown;
            return Math.Clamp(Math.Truncate((float)(cooldown * 1000d) * multiplier), 0d, int.MaxValue) / 1000d;
        }
    }
}
