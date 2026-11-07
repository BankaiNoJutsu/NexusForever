using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 42: native case <c>0x2a</c> (<c>FUN_14049e850</c>) compares the local player's entry in the group manager's member array (0 when absent) with the leader entry (0 when the group
    /// has no members): comparison 1 returns "equal", 2 "different", anything else false, and <c>Value</c> is not read. A player without a group therefore counts as the leader.
    /// Status: native body read. See Decomp/Analysis/PREREQUISITE_GROUP_LEADER_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsGroupLeader)]
    public class PrerequisiteCheckIsGroupLeader : IPrerequisiteCheck
    {
        private readonly IGroupStateManager groupStateManager;

        public PrerequisiteCheckIsGroupLeader(IGroupStateManager groupStateManager)
        {
            this.groupStateManager = groupStateManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            bool isLeader = player.GroupAssociation == 0ul
                || (groupStateManager.TryGetGroup(player.GroupAssociation, out GroupLootState group)
                    && group.Leader.Equals(player.Identity));

            return PrerequisiteCompare.CompareFlag(comparison, isLeader);
        }
    }
}
