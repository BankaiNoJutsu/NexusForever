using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 266: client case <c>0x10a</c> dispatches vtable <c>+0xc8</c> to <c>14049cae0</c>,
    /// which compares entity type <c>+0x80</c> against native ids <c>0x18</c>/<c>0x19</c> (<see cref="EntityType.Pet"/>/<see cref="EntityType.EsperPet"/>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PetOrEsperPetEntity)]
    public class PrerequisiteCheckPetOrEsperPetEntity : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            // Native (evaluator case 0x10a, vtable +0xc8) receives only the unit and the comparison; the body at 14049cae0 tests entity type 0x18 or 0x19 (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // Value is never read; comparing the flag with Value inverted all 4 retail slots (Value 0). The slot pointer (140b67808) was read with DumpPointerSlots.java.
            bool isPetEntity = entity.Type is EntityType.Pet or EntityType.EsperPet;
            return PrerequisiteCompare.CompareFlag(comparison, isPetEntity);
        }
    }
}
