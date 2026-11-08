using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 244: native <c>FUN_14049ca70</c> (player units only) looks up the player's current bind point (<c>player+0x1694</c>, filled from the player create packet and 0x016D) in
    /// <c>BindPoint.tbl</c>; Equal is true when it exists and NotEqual when it does not. <c>ObjectId</c> and <c>Value</c> are not read; it is the same test as the Lua <c>HasBindPoint</c>. The type was
    /// named <c>OwnsAccountItem</c> from an earlier reading of the one retail row's ObjectId. Agent-written read, not re-derived, see
    /// Decomp/Analysis/PREREQUISITE_239_226_244_18_NATIVE_READ_20261108.md; the 0x016D packet carrying the bind point id is confirmed by the server's own <c>SetBindPoint</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HasBindPoint)]
    public class PrerequisiteCheckHasBindPoint : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, player.TryGetBindPoint(out _, out _));
    }
}
