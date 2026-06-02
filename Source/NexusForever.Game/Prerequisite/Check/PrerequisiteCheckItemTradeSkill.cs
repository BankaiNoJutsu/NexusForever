using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemTradeSkill)]
    [PrerequisiteCheck(PrerequisiteType.Unknown245)]
    public class PrerequisiteCheckItemTradeSkill : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemTradeSkill(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
                return false;

            if (gameTableManager.Item.GetEntry(objectId) == null)
                return false;

            if (!TradeskillPrerequisiteHelper.TryResolveTradeskillForItem2(gameTableManager, objectId, out TradeskillType tradeskillId))
                return false;

            uint tierRank = TradeskillPrerequisiteHelper.GetPlayerTradeskillTierRank(player, gameTableManager, tradeskillId);
            return PrerequisiteCompare.Compare(comparison, tierRank, value);
        }
    }
}
