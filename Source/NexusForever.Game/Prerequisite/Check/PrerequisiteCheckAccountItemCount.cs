using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.AccountItemCount)]
    public class PrerequisiteCheckAccountItemCount : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckAccountItemCount(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
                return false;

            uint count = AccountItemOwnedCount.CountOwnedByItem2Id(player, objectId, gameTableManager);
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
