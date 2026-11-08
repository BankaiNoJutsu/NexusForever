using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 226: native <c>FUN_1404a1620</c> (player units only) computes the entitlement count <c>n = FUN_1403d2140(ObjectId)</c> that type 273 uses (character plus account amount,
    /// capped at the Entitlement row's <c>MaxCount</c>); Equal is <c>n != 0</c>, NotEqual is <c>n == 0</c>, every other comparison is false and <c>Value</c> is not read. The read was done by an agent and
    /// not re-derived, see Decomp/Analysis/PREREQUISITE_239_226_244_18_NATIVE_READ_20261108.md (the non-zero reading of Equal is inferred from the compare shape).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Entitlement)]
    public class PrerequisiteCheckEntitlement : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckEntitlement(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, PrerequisiteCheckEntitlementCount.GetCount(player, gameTableManager, objectId) != 0u);
    }
}
