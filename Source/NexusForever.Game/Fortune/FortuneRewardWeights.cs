using NexusForever.Game.Static.Fortune;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Fortune
{
    /// <summary>
    /// Current emulator rarity-tier weights for Madame Fay card selection and
    /// <c>ServerFortuneRewards</c> display probabilities. Retail sends normalized floats in
    /// <c>RewardItemProbabilities</c>; the client multiplies by 100 for UI
    /// (<c>FortunesLib.GetFortunesLootList</c> / FUN_140766370). Exact retail per-item weights are
    /// not mapped.
    /// </summary>
    internal static class FortuneRewardWeights
    {
        private const uint NormalWeight = 1000u;
        private const uint RareWeight   = 200u;
        private const uint EpicWeight   = 50u;

        public static uint GetWeight(RewardRarity rarity)
        {
            return rarity switch
            {
                RewardRarity.Epic  => EpicWeight,
                RewardRarity.Rare  => RareWeight,
                _                  => NormalWeight
            };
        }

        public static uint GetWeight(AccountItemEntry entry, RewardRarity rarity)
        {
            if (entry == null)
                return 0u;

            return GetWeight(rarity);
        }

        public static float ToDisplayProbability(uint weight, ulong totalWeight)
        {
            if (weight == 0u || totalWeight == 0ul)
                return 0f;

            return weight / (float)totalWeight;
        }
    }
}
