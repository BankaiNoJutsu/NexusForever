using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 64: native jump-table entry 64 (<c>1404a24ad</c>) reads the unit`s resource slot <c>ObjectId</c> (0 to 10, the dword array at <c>unit+0x21c</c> that <c>FUN_140466520</c> exposes as
    /// <see cref="Vital.Resource0"/> to <see cref="Vital.Resource10"/>) as a float and compares it with <c>Value</c> as a float (slot first). A slot above 10 reads 0; a null unit is false.
    /// It is not a path level: the earlier path-type reading is superseded (retail ObjectId 1 to 4 with Values 0, 1, 25, 50, 60, 100).
    /// Status: native body read from the disassembly. See Decomp/Analysis/PREREQUISITE_RESOURCE_SLOT_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ResourceSlot)]
    public class PrerequisiteCheckResourceSlot : IPrerequisiteCheck
    {
        private static readonly Vital[] Slots =
        [
            Vital.Resource0, Vital.Resource1, Vital.Resource2, Vital.Resource3, Vital.Resource4, Vital.Resource5,
            Vital.Resource6, Vital.Resource7, Vital.Resource8, Vital.Resource9, Vital.Resource10
        ];

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            float actual = 0f;
            if (objectId < Slots.Length && !subject.TryGetVitalValue(Slots[objectId], out actual))
                actual = 0f;

            return PrerequisiteCompare.CompareFloat(comparison, actual, unchecked((int)value));
        }
    }
}
