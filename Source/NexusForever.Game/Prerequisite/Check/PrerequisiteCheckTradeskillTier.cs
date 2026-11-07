using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 19 (<c>FUN_14049f0f0</c>): the evaluator forwards <c>(unit, comparison, objectId)</c>. The body looks up the TradeskillTier row with id <c>ObjectId</c>, finds the
    /// unit's matching tradeskill and compares the unit's tier in it with the row's Tier using the comparison (<c>Value</c> is unused). A unit without that tradeskill compares as tier 0.
    /// Retail rows: 267 of 268 slots have <c>ObjectId</c> in TradeskillTier, comparisons are mostly greater-or-equal, with some equal and less-than.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.TradeSkillProfession)]
    public class PrerequisiteCheckTradeskillTier : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckTradeskillTier(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            TradeskillTierEntry row = gameTableManager.TradeskillTier?.GetEntry(objectId);
            if (row == null)
                return false;

            var tradeskillId = (TradeskillType)row.TradeSkillId;
            uint playerTier = Enum.IsDefined(tradeskillId) && player.HasTradeskill(tradeskillId)
                ? TradeskillPrerequisiteHelper.GetPlayerTradeskillTierRank(player, gameTableManager, tradeskillId)
                : 0u;

            return PrerequisiteCompare.Compare(comparison, playerTier, row.Tier);
        }
    }
}
