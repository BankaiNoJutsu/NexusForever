using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// Folds the active <see cref="PhaseChange"/> records of a player into the two phase words sent in packet 0x0171.
    /// </summary>
    public static class PhaseChangeRules
    {
        /// <summary>
        /// Phase word of a player no spell has moved: bit 0, the value the server sends at login.
        /// </summary>
        public const uint DefaultMask = 1u;

        /// <summary>
        /// Start from <see cref="DefaultMask"/> in both words and apply each change in order: <c>word = (word | add) &amp; ~clear</c>.
        /// </summary>
        /// <remarks>
        /// In the shipped rows the add and clear bits never overlap, so the order of the two operations is not observable there.
        /// </remarks>
        public static void Fold(IEnumerable<PhaseChange> changes, out uint phasesIPerceive, out uint phasesThatPerceiveMe)
        {
            phasesIPerceive      = DefaultMask;
            phasesThatPerceiveMe = DefaultMask;

            foreach (PhaseChange change in changes)
            {
                phasesIPerceive      = (phasesIPerceive | change.AddIPerceive) & ~change.ClearIPerceive;
                phasesThatPerceiveMe = (phasesThatPerceiveMe | change.AddPerceiveMe) & ~change.ClearPerceiveMe;
            }
        }
    }
}
