using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite types 97–99: handler table <c>14049f8c0</c>/<c>14049f990</c>/<c>14049f9d0</c> gate NPC/vendor
    /// targets (entity+0x80 types <c>0x14</c>/<c>0x17</c>) then call <c>AccountItemList_WalkItem2IdMatch</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.AccountItemListItem2CountNpc97)]
    [PrerequisiteCheck(PrerequisiteType.AccountItemListItem2CountNpc98)]
    [PrerequisiteCheck(PrerequisiteType.AccountItemListItem2CountNpc99)]
    [PrerequisiteCheck(PrerequisiteType.Unknown223)]
    public class PrerequisiteCheckAccountItemListItem2CountNpc : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckAccountItemListItem2CountNpc(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
                return false;

            uint count = AccountItemOwnedCount.CountAccountRowsByItem2Id(player, objectId, gameTableManager);
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
