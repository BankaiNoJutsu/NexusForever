using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite types 100–101: handler table <c>14049fa80</c>/<c>14049fac0</c> call
    /// <c>AccountItemList_WalkItem2IdMatch</c> (<c>140497d9c</c>) without the NPC target gate used by 97–99.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.AccountItemListItem2Count100)]
    [PrerequisiteCheck(PrerequisiteType.AccountItemListItem2Count101)]
    public class PrerequisiteCheckAccountItemListItem2Count : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckAccountItemListItem2Count(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint count = AccountItemOwnedCount.CountAccountRowsByItem2Id(player, objectId, gameTableManager);
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
