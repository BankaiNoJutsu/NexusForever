using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 217: native test <c>FUN_14049db90</c> compares <c>FUN_1403b48b0(unit)</c>, 1 when the unit's PvP flag bit is set and 0 otherwise, with <c>Value</c>; Equal and
    /// NotEqual only, every other comparison is false. Retail: <c>Value</c> 1 (6 slots) or 0 (3), comparison Equal (8) and NotEqual (1). The native function also reports 1 for a pet or
    /// summon whose owner is flagged; the server models the flag on players only, so any other unit counts as unflagged.
    /// The unit is the supplied target when there is one (the message is "Target is not flagged for PvP.") and the player otherwise.
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PvpFlag)]
    public class PrerequisiteCheckPvpFlag : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(parameters?.Target ?? player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            uint flagged = subject is IPlayer player && player.PvPFlag != PvPFlag.Disabled ? 1u : 0u;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => flagged == value,
                PrerequisiteComparison.NotEqual => flagged != value,
                _                               => false
            };
        }
    }
}
