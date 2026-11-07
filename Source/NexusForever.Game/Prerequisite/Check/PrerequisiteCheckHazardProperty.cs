using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 111: native <c>FUN_1404a03e0</c> asks the hazard manager whether the hazard <c>ObjectId</c> has a record whose field <c>+0x28</c> (the packet's <c>Enabled</c> flag) is non-zero. Equal returns that,
    /// NotEqual its negation, other comparisons are false. 47 retail slots (Equal 44).
    /// Status: native body read; the field is identified from the packet order. See Decomp/Analysis/PREREQUISITE_HAZARD_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HazardProperty111)]
    public class PrerequisiteCheckHazardProperty111 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, player.IsHazardEnabled(objectId));
    }

    /// <summary>
    /// Prerequisite type 113: native <c>FUN_1404a04f0</c> compares the meter value of the hazard <c>ObjectId</c> (record <c>+8</c>, the packet's <c>MeterValue</c>; -1.0 without a record) with <c>(float)Value</c> through the
    /// float helper, actual first. 36 retail slots, Values 0 to 584.
    /// Status: native body read; server mapping inferred. See Decomp/Analysis/PREREQUISITE_HAZARD_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HazardProperty113)]
    public class PrerequisiteCheckHazardProperty113 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFloat(comparison, player.GetHazardMeter(objectId), unchecked((int)value));
    }
}
