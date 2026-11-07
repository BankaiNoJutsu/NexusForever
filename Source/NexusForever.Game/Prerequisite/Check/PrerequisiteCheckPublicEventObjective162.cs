using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 162: native <c>FUN_1404a0a70</c> finds the public event objective <c>ObjectId</c> and, when it exists and is active, takes count (virtual <c>+0x98</c>) over its required count
    /// (virtual <c>+0xd0</c>) times 100 (another mode, virtual <c>+0xf0</c>, uses a float progress directly; not modelled); a missing objective is 0. The percentage is compared with <c>Value</c> through the
    /// float helper (actual first). Retail: 8 slots, Values 100, 50, 30, 60, comparisons >= and ==.
    /// Status: native body read; server mapping inferred. See Decomp/Analysis/PREREQUISITE_PUBLIC_EVENT_OBJECTIVE_PERCENT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEventObjective2)]
    public class PrerequisiteCheckPublicEventObjective162 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFloat(comparison, PublicEventPrerequisiteHelper.GetObjectivePercent(player, objectId), value);
    }
}
