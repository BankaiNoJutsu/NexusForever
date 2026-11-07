using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 55: native jump-table entry 55 (<c>1404a243f</c>) compares the unit`s property array entry <c>ObjectId</c> (197 entries, 0 above index 0xC4) as a float with <c>Value</c> as a float
    /// (property first). A null unit is false. It is not a currency amount: retail ObjectIds are 9 (ResourceMax0), 41 (ShieldCapacityMax) and 100 (MoveSpeedMultiplier).
    /// Status: native body read from the disassembly; the array is mapped to <see cref="Property"/> as for type 115. See Decomp/Analysis/PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.UnitPropertyValue)]
    public class PrerequisiteCheckUnitPropertyValue : IPrerequisiteCheck
    {
        private const uint MaxPropertyIndex = 0xC4;

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            float actual = objectId > MaxPropertyIndex ? 0f : subject.GetPropertyValue((Property)objectId);
            return PrerequisiteCompare.CompareFloat(comparison, actual, unchecked((int)value));
        }
    }
}
