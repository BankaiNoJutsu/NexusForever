using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.DailyLoginDaysTotal)]
    [PrerequisiteCheck(PrerequisiteType.Unknown286)]
    public class PrerequisiteCheckDailyLoginDaysTotal : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint loginDaysTotal = player.Account.InventoryManager.GetDailyLoginDaysTotal();
            return PrerequisiteCompare.Compare(comparison, loginDaysTotal, value);
        }
    }
}
