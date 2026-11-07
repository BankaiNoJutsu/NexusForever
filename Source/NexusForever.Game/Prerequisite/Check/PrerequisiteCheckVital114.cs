using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 114: native case <c>0x72</c> reads two vitals of the unit through <c>FUN_140466520</c>, the first with <c>ObjectId</c> and the second with <c>Value</c>, and compares them as floats with the comparison
    /// (the disassembly moves the first result to XMM2, the actual, and the second to XMM3, the expected). A null unit is false. Retail: 10 slots over vital ids 7, 8 and 9 with >= and >.
    /// Status: native body and argument order read from the disassembly. An unreadable vital counts as 0, the native reader`s default. See Decomp/Analysis/PREREQUISITE_VITAL_COMPARE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Vital114)]
    public class PrerequisiteCheckVital114 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            return PrerequisiteCompare.CompareFloat(comparison, Read(subject, objectId), Read(subject, value));
        }

        private static float Read(IUnitEntity unit, uint vital)
            => unit.TryGetVitalValue((Vital)vital, out float amount) ? amount : 0f;
    }
}
