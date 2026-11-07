using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 20: native case <c>0x14</c> (<c>FUN_14049e820</c>) compares the member count of the local player's group (the count at <c>+0x60</c> of the group manager, the manager type 42 reads;
    /// 0 without a group) with <c>Value</c> through the integer helper, count first. <c>ObjectId</c> is not read. Retail: 8 slots, <c>Value</c> 0 to 4.
    /// Status: native body read. See Decomp/Analysis/PREREQUISITE_GROUP_SIZE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.GroupSize)]
    public class PrerequisiteCheckGroupSize : IPrerequisiteCheck
    {
        private readonly IGroupStateManager groupStateManager;

        public PrerequisiteCheckGroupSize(IGroupStateManager groupStateManager)
        {
            this.groupStateManager = groupStateManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint size = 0u;
            if (player.GroupAssociation != 0ul && groupStateManager.TryGetGroup(player.GroupAssociation, out GroupLootState group))
                size = (uint)group.Members.Count;

            return PrerequisiteCompare.Compare(comparison, size, value);
        }
    }
}
