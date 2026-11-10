using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 267: live case <c>0x10b</c> handler <c>14049e9a0</c> compares bit 1
    /// of the current group context flags, matching <see cref="GroupFlags.Raid"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.GroupIsRaid)]
    public class PrerequisiteCheckGroupIsRaid : IPrerequisiteCheck
    {
        private readonly IGroupStateManager groupStateManager;

        public PrerequisiteCheckGroupIsRaid(IGroupStateManager groupStateManager)
        {
            this.groupStateManager = groupStateManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint isRaid = 0u;
            if (player.GroupAssociation != 0ul
                && groupStateManager.TryGetGroup(player.GroupAssociation, out GroupLootState group)
                && group.Flags.HasFlag(GroupFlags.Raid))
            {
                isRaid = 1u;
            }

            // Native (evaluator case 0x10b, vtable +0x320, 14049e9a0) receives only the unit and the comparison and tests bit 1 of the group context flags (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // Value is never read; comparing the flag with Value inverted both retail slots (NotEqual, Value 0).
            return PrerequisiteCompare.CompareFlag(comparison, isRaid == 1u);
        }
    }
}
