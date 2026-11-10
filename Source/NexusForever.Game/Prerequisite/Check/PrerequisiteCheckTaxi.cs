using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 85: handler table <c>Prerequisite_CheckTaxi</c> (<c>14049f0f0</c>) tests taxi movement state at
    /// entity <c>+0x16a0</c>. NF checks platform <see cref="EntityType.Taxi"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Taxi)]
    public class PrerequisiteCheckTaxi : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint onTaxi = MovementPrerequisiteHelper.GetPlatformEntityType(player) == (uint)EntityType.Taxi ? 1u : 0u;
            // Native (evaluator case 0x55, vtable +0xf0) receives the unit, the comparison and ObjectId; Value is never read (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // The local player must be on a type 9 (taxi) entity; a non-zero ObjectId must additionally equal an id held by that entity, which the server does not model, so ObjectId is not applied.
            // Comparing the flag with Value inverted the 14 retail slots whose Value is 0.
            return PrerequisiteCompare.CompareFlag(comparison, onTaxi == 1u);
        }
    }
}
