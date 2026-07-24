namespace NexusForever.Game.Entity
{
    internal sealed class VendorPriceModifierCollection
    {
        private sealed record ModifierState(
            float VendorSellMultiplier,
            float VendorBuyMultiplier,
            uint Spell4Id,
            uint Spell4EffectId,
            uint CastingId,
            uint StackGroupId);

        private readonly Dictionary<uint, ModifierState> modifiers = [];

        public float VendorSellMultiplier => CalculateMultiplier(state => state.VendorSellMultiplier);
        public float VendorBuyMultiplier => CalculateMultiplier(state => state.VendorBuyMultiplier);

        public bool TryAdd(
            uint effectId,
            float vendorSellMultiplier,
            float vendorBuyMultiplier,
            uint spell4Id,
            uint spell4EffectId,
            uint castingId,
            uint stackGroupId,
            uint stackCap,
            out string skippedReason)
        {
            skippedReason = null;
            if (!IsValidMultiplier(vendorSellMultiplier) || !IsValidMultiplier(vendorBuyMultiplier))
            {
                skippedReason = "invalid-multiplier";
                return false;
            }

            foreach (KeyValuePair<uint, ModifierState> existing in modifiers.ToArray())
            {
                if (existing.Value.Spell4Id == spell4Id && existing.Value.Spell4EffectId == spell4EffectId)
                    modifiers.Remove(existing.Key);
            }

            if (stackGroupId != 0u && stackCap > 0u)
            {
                uint[] groupEffects = modifiers
                    .Where(entry => entry.Value.StackGroupId == stackGroupId)
                    .OrderBy(entry => entry.Value.CastingId)
                    .Select(entry => entry.Key)
                    .ToArray();

                int removeCount = Math.Max(0, groupEffects.Length - (int)stackCap + 1);
                foreach (uint existingEffectId in groupEffects.Take(removeCount))
                    modifiers.Remove(existingEffectId);
            }

            modifiers[effectId] = new ModifierState(
                vendorSellMultiplier,
                vendorBuyMultiplier,
                spell4Id,
                spell4EffectId,
                castingId,
                stackGroupId);
            return true;
        }

        public bool Remove(uint effectId)
        {
            return modifiers.Remove(effectId);
        }

        private float CalculateMultiplier(Func<ModifierState, float> selector)
        {
            float result = 1f;
            foreach (ModifierState modifier in modifiers.Values)
                result *= selector(modifier);

            return float.IsFinite(result) && result > 0f ? result : 1f;
        }

        private static bool IsValidMultiplier(float multiplier)
        {
            return float.IsFinite(multiplier) && multiplier > 0f;
        }
    }
}
