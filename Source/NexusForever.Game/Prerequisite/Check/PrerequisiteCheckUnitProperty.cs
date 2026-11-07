using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 115: native evaluator case <c>0x73</c> reads two entries of the unit`s property array (197 entries, indices 0 to 0xC4, stride 8 at <c>unit+0x5c4</c>) - index <c>ObjectId</c> as the actual and index <c>Value</c> as the expected -
    /// and compares them as floats with the comparison. An index above 0xC4 reads 0; a null unit is false. Retail: 18 slots, both ids from {0, 2, 29, 34, 35, 36, 57}.
    /// Status: native body read from the disassembly (the decompile dropped the float arguments); the array is mapped to <see cref="Property"/> by its 0xC4 bound (the largest <see cref="Property"/> is 196).
    /// See Decomp/Analysis/PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Unit)]
    public class PrerequisiteCheckUnitProperty : IPrerequisiteCheck
    {
        private const uint MaxPropertyIndex = 0xC4;

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            return PrerequisiteCompare.CompareFloat(comparison, Read(subject, objectId), Read(subject, value));
        }

        private static float Read(IUnitEntity unit, uint index)
            => index > MaxPropertyIndex ? 0f : unit.GetPropertyValue((Property)index);
    }
}
