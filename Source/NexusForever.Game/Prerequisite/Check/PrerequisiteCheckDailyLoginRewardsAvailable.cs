using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.DailyLoginRewardsAvailable)]
    public class PrerequisiteCheckDailyLoginRewardsAvailable : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint rewardsAvailable = player.Account.InventoryManager.GetDailyLoginRewardsAvailable();
            return PrerequisiteCompare.Compare(comparison, rewardsAvailable, value);
        }
    }
}
