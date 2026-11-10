using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 4: handler table <c>14049d3f0</c> compares entity faction id (player
    /// <c>Faction1</c> proxy). Distinct from type 128 <see cref="PrerequisiteType.Faction128"/>
    /// (<c>Prerequisite_CheckFaction</c> <c>14049c720</c>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Faction)]
    public class PrerequisiteCheckFaction : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Native (evaluator case 0x4, vtable +0x58, 14049c620): a null unit is false, Equal and NotEqual compare with Value, any other comparison is false (read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            if (subject == null)
                return false;

            return PrerequisiteCompare.CompareFlag(comparison, (uint)subject.Faction1 == value);
        }
    }
}
