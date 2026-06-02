using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemTradeSkillKnown)]
    public class PrerequisiteCheckItemTradeSkillKnown : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemTradeSkillKnown(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
                return false;

            if (gameTableManager.Item.GetEntry(objectId) == null)
                return false;

            bool known = TradeskillPrerequisiteHelper.HasKnownItem2Id(player, gameTableManager, objectId);
            return PrerequisiteCompare.Compare(comparison, known ? 1u : 0u, value);
        }
    }
}
