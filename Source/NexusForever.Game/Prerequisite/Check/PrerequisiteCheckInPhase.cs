using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 24: native <c>FUN_14049e640</c> (shared with type 25) tests bit <c>Value &amp; 31</c> of the local player's <c>PhasesIPerceive</c> word
    /// (the dword the server sends in <c>ServerPhaseVisibilityWorldLocation</c>, 0x0171); Equal returns the bit, NotEqual its negation, other comparisons false.
    /// The entity, the target and ObjectId are not read. See Decomp/Analysis/PREREQUISITE_24_25_219_NATIVE_READ_20261108.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InPhase)]
    public class PrerequisiteCheckInPhase : IPrerequisiteCheck
    {
        /// <summary>
        /// Mask of a unit that is not a player: no native meaning, the login default of one is assumed.
        /// </summary>
        internal const uint DefaultPhaseMask = 1u;

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsMask(player.PhasesIPerceive, comparison, value);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsMask(subject is IPlayer player ? player.PhasesIPerceive : DefaultPhaseMask, comparison, value);

        internal static bool MeetsMask(uint mask, PrerequisiteComparison comparison, uint value)
            => PrerequisiteCompare.CompareFlag(comparison, ((mask >> (int)(value & 31u)) & 1u) != 0u);
    }
}
