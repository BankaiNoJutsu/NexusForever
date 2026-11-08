using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 239: native <c>FUN_14049ca10</c> (player units only) tests bit 0 of the per-action-set unlock dword for set <c>Value</c> (0 to 3) that the client fills from
    /// <c>ServerActionSet</c> (0x019D, field <c>Unlocked</c>); Equal is the flag and NotEqual its negation, other comparisons are false. The server announces every action set it has
    /// (0 to <see cref="ActionSet.MaxActionSets"/> - 1) as unlocked and keeps no locked state, so the flag is whether the set exists. Agent-written read, not re-derived, see
    /// Decomp/Analysis/PREREQUISITE_239_226_244_18_NATIVE_READ_20261108.md. Once the <c>UnlockActionBar</c> spell effect has a locked state this must read it.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActionBar)]
    public class PrerequisiteCheckActionBar : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, value < ActionSet.MaxActionSets);
    }
}
