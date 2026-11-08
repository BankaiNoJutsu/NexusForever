using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 146: the item under evaluation is compared against a tradeskill tier. The player is not involved and <c>Value</c> is not read.
    /// </summary>
    /// <remarks>
    /// Mirrors the client evaluator <c>FUN_1404a0f90</c> as read by an agent, see Decomp/Analysis/PREREQUISITE_146_NATIVE_READ_20261108.md (not re-derived; the table facts were re-checked). The item gives a
    /// level <c>v = (Flags &amp; 1) ? PowerLevel : (RequiredLevel != 0 ? RequiredLevel : 1)</c>; a Depreciated (0x4000) item takes Flags and RequiredLevel from the substitute Item2 74201. The tradeskill of
    /// the <c>TradeskillTier</c> row named by <c>ObjectId</c> has thresholds <c>thr[Tier - 1] = ItemLevelMin</c> for Tier 1 to 10 (missing tiers 0) and <c>count</c> = the highest tier present. The band
    /// index is the highest <c>i</c> with <c>v &gt;= thr[i - 1]</c> (1 when below the first threshold or when there are no tiers), and the comparison of the slot compares that index with the row's
    /// <c>Tier</c>. A missing item or tier row is false for every comparison, NotEqual included. The meaning of Item2 flag bit 0 is unknown.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.ItemTradeSkillLevel)]
    public class PrerequisiteCheckItemTradeSkillLevel : IPrerequisiteCheck
    {
        private const uint SubstituteItemId = 74201u;
        private const uint DepreciatedFlag = 0x4000u;
        private const uint PowerLevelFlag = 1u;
        private const uint MaxTier = 10u;

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemTradeSkillLevel(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            Item2Entry item = parameters?.Item?.Info?.Entry;
            if (item == null)
                return false;

            TradeskillTierEntry tierRow = gameTableManager.TradeskillTier.GetEntry(objectId);
            if (tierRow == null)
                return false;

            uint flags = (uint)item.Flags;
            uint requiredLevel = item.RequiredLevel;
            if ((flags & DepreciatedFlag) != 0u)
            {
                Item2Entry substitute = gameTableManager.Item.GetEntry(SubstituteItemId);
                if (substitute != null)
                {
                    flags = (uint)substitute.Flags;
                    requiredLevel = substitute.RequiredLevel;
                }
            }

            uint level = (flags & PowerLevelFlag) != 0u
                ? item.PowerLevel
                : requiredLevel != 0u ? requiredLevel : 1u;

            return PrerequisiteCompare.Compare(comparison, GetBandIndex(tierRow.TradeSkillId, level), tierRow.Tier);
        }

        private uint GetBandIndex(uint tradeskillId, uint level)
        {
            var thresholds = new uint[MaxTier];
            uint count = 0u;
            foreach (TradeskillTierEntry tier in gameTableManager.TradeskillTier.Entries)
            {
                if (tier.TradeSkillId != tradeskillId || tier.Tier < 1u || tier.Tier > MaxTier)
                    continue;

                thresholds[tier.Tier - 1u] = tier.ItemLevelMin;
                count = Math.Max(count, tier.Tier);
            }

            for (uint i = count; i >= 1u; i--)
            {
                if (level >= thresholds[i - 1u])
                    return i;
            }

            return 1u;
        }
    }
}
