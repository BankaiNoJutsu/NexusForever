using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 44: native test <c>FUN_14049ee40</c>. <c>Value</c> is a TargetGroup id (214 of 214 retail slots; <c>ObjectId</c> is 0) and the unit is tested with the
    /// target-group evaluator <c>FUN_1403b4a20</c>, where 0 means member. Equal meets for a member, NotEqual for a non-member, other comparisons never meet, and a zero
    /// group id never meets. Group type 1 lists creature ids, 2 is its inverse, 10 requires every listed sub-group and 11 any one of them. Types 3 to 9, 12 and 13 test
    /// unit fields (<c>unit+0xd8/0xdc/0x118/0xd0/0x140</c>) that the server does not model, so a group that depends on one of them meets neither comparison.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InTargetGroup)]
    public class PrerequisiteCheckInTargetGroup : IPrerequisiteCheck
    {
        private const uint CreatureListType = 1u;
        private const uint CreatureListExcludedType = 2u;
        private const uint AllOfGroupsType = 10u;
        private const uint AnyOfGroupsType = 11u;

        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckInTargetGroup(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters.Target ?? player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null || value == 0u)
                return false;

            bool? member = IsMember(subject, value, []);
            if (member == null)
                return false;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => member.Value,
                PrerequisiteComparison.NotEqual => !member.Value,
                _                               => false
            };
        }

        /// <summary>
        /// Returns whether the unit is a member of the group, or null when the group depends on a unit field that is not modelled.
        /// An unknown group is not a member.
        /// </summary>
        private bool? IsMember(IUnitEntity unit, uint groupId, HashSet<uint> visiting)
        {
            TargetGroupEntry group = gameTableManager.TargetGroup?.GetEntry(groupId);
            if (group == null)
                return gameTableManager.TargetGroup == null ? null : false;

            switch (group.Type)
            {
                case CreatureListType:
                    return ContainsCreature(group, unit.CreatureId);
                case CreatureListExcludedType:
                    return !ContainsCreature(group, unit.CreatureId);
                case AllOfGroupsType:
                case AnyOfGroupsType:
                    return IsMemberOfSubGroups(unit, group, group.Type == AnyOfGroupsType, visiting);
                default:
                    return null;
            }
        }

        private bool? IsMemberOfSubGroups(IUnitEntity unit, TargetGroupEntry group, bool any, HashSet<uint> visiting)
        {
            if (!visiting.Add(group.Id))
                return false;

            bool unmodelled = false;
            foreach (uint subGroupId in group.DataEntries)
            {
                // the native loop skips empty slots and the group's own id
                if (subGroupId == 0u || subGroupId == group.Id)
                    continue;

                bool? member = IsMember(unit, subGroupId, visiting);
                if (member == null)
                    unmodelled = true;
                else if (member.Value == any)
                {
                    visiting.Remove(group.Id);
                    return any;
                }
            }

            visiting.Remove(group.Id);
            return unmodelled ? null : !any;
        }

        private static bool ContainsCreature(TargetGroupEntry group, uint creatureId)
            => creatureId != 0u && group.DataEntries.Contains(creatureId);
    }
}
