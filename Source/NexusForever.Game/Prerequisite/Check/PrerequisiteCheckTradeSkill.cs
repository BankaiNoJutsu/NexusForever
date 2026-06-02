using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.TradeSkill)]
    public class PrerequisiteCheckTradeSkill : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckTradeSkill(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            var tradeskillId = (TradeskillType)objectId;
            if (!Enum.IsDefined(tradeskillId))
                return false;

            if (!player.HasTradeskill(tradeskillId))
                return false;

            uint tierRank = TradeskillPrerequisiteHelper.GetPlayerTradeskillTierRank(player, gameTableManager, tradeskillId);
            return PrerequisiteCompare.Compare(comparison, tierRank, value);
        }
    }
}
