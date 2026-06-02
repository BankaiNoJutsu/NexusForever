using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 268: live vtable <c>+0x3c8</c> ->
    /// <c>Prerequisite_CheckCREDDPendingOrderState_Table268</c> (<c>14049f090</c>)
    /// compares account CREDD pending-order flag at <c>+0x1708</c> to <c>objectId</c>.
    /// Row <c>38851</c> uses <see cref="PrerequisiteComparison.NotEqual"/> with
    /// <c>objectId0=1</c> for CREDD exchange NPC visibility when no order is pending.
    /// NF maps flag <c>1</c> to any open persisted <c>AccountCREDDOrder</c> row for the account.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CREDDPendingOrderState)]
    public class PrerequisiteCheckCREDDPendingOrderState : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint pendingOrderState = player?.Account?.GetCREDDPendingOrderState() ?? 0u;
            return PrerequisiteCompare.Compare(comparison, pendingOrderState, objectId);
        }
    }
}
