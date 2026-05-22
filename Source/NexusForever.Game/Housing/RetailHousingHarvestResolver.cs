using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Housing
{
    /// <summary>
    /// Maps housing plug <see cref="HousingContributionInfoEntry"/> tiers to harvest item yields (build 16042).
    /// </summary>
    public static class RetailHousingHarvestResolver
    {
        public readonly struct HarvestYield
        {
            public uint Item2Id { get; init; }
            public uint Quantity { get; init; }
            public uint ContributionInfoId { get; init; }
            public byte TierIndex { get; init; }
        }

        public static bool TryResolveYield(
            HousingPlugItemEntry plugEntry,
            IGameTableManager gameTableManager,
            uint contributionPointTotal,
            out HarvestYield yield)
        {
            yield = default;
            if (plugEntry == null || gameTableManager == null)
                return false;

            HarvestYield? best = null;
            foreach (uint contributionId in GetContributionInfoIds(plugEntry))
            {
                if (contributionId == 0u)
                    continue;

                HousingContributionInfoEntry contribution = gameTableManager.HousingContributionInfo.GetEntry(contributionId);
                if (contribution == null)
                    continue;

                for (int tier = 4; tier >= 0; tier--)
                {
                    if (!TryGetTier(contribution, (byte)tier, out uint item2Id, out uint quantity, out uint requirement))
                        continue;

                    if (item2Id == 0u || contributionPointTotal < requirement)
                        continue;

                    if (best == null || tier > best.Value.TierIndex)
                    {
                        best = new HarvestYield
                        {
                            Item2Id            = item2Id,
                            Quantity           = Math.Max(1u, quantity),
                            ContributionInfoId = contribution.Id,
                            TierIndex          = (byte)tier
                        };
                    }

                    break;
                }
            }

            if (best == null)
                return false;

            yield = best.Value;
            return true;
        }

        static IEnumerable<uint> GetContributionInfoIds(HousingPlugItemEntry plugEntry)
        {
            yield return plugEntry.HousingContributionInfoId00;
            yield return plugEntry.HousingContributionInfoId01;
            yield return plugEntry.HousingContributionInfoId02;
            yield return plugEntry.HousingContributionInfoId03;
            yield return plugEntry.HousingContributionInfoId04;
        }

        static bool TryGetTier(
            HousingContributionInfoEntry contribution,
            byte tier,
            out uint item2Id,
            out uint quantity,
            out uint requirement)
        {
            requirement = contribution.ContributionPointRequirement;

            switch (tier)
            {
                case 0:
                    item2Id  = contribution.Item2IdTier00;
                    quantity = contribution.ContributionPointValueTier00;
                    return item2Id > 0u;
                case 1:
                    item2Id  = contribution.Item2IdTier01;
                    quantity = contribution.ContributionPointValueTier01;
                    return item2Id > 0u;
                case 2:
                    item2Id  = contribution.Item2IdTier02;
                    quantity = contribution.ContributionPointValueTier02;
                    return item2Id > 0u;
                case 3:
                    item2Id  = contribution.Item2IdTier03;
                    quantity = contribution.ContributionPointValueTier03;
                    return item2Id > 0u;
                case 4:
                    item2Id  = contribution.Item2IdTier04;
                    quantity = contribution.ContributionPointValueTier04;
                    return item2Id > 0u;
                default:
                    item2Id  = 0;
                    quantity = 0;
                    return false;
            }
        }
    }
}
