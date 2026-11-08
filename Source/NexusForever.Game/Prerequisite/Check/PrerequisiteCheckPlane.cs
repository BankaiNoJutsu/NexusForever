using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 232: native <c>FUN_14049e610</c> tests <c>(plane word &amp; Value) != 0</c> on the local player's plane word, the second dword of
    /// <c>ServerPlaneVisibilitySpellSlingerVoid</c> (0x0172); Equal returns the flag, NotEqual its negation. The word is zero until a <c>ChangePlane</c> spell effect sets bits.
    /// See Decomp/Analysis/PREREQUISITE_NATIVE_COMPARISONS_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Plane)]
    public class PrerequisiteCheckPlane : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsMask(player.PlanesCanSee, comparison, value);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsMask(subject is IPlayer player ? player.PlanesCanSee : 0u, comparison, value);

        private static bool MeetsMask(uint mask, PrerequisiteComparison comparison, uint value)
            => PrerequisiteCompare.CompareFlag(comparison, (mask & value) != 0u);
    }
}
