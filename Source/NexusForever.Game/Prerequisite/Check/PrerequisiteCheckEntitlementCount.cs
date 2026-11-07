using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 273: native test <c>FUN_1404a1670</c> (player units only) compares <c>FUN_1403d2140(ObjectId)</c> with <c>Value</c> (comparison, count, value). That count is the
    /// character amount plus the account amount of the entitlement, capped at the Entitlement row's <c>MaxCount</c> (0, or no row, means no cap). Retail: <c>ObjectId</c> is an Entitlement id
    /// in all 17 slots, comparison LessThan (12), Equal (4) and GreaterThanOrEqual (1).
    /// See Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.EntitlementCount)]
    public class PrerequisiteCheckEntitlementCount : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckEntitlementCount(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            var type = (EntitlementType)objectId;

            ulong total = (ulong)(player.EntitlementManager?.GetEntitlement(type)?.Amount ?? 0u)
                + (player.Account?.EntitlementManager?.GetEntitlement(type)?.Amount ?? 0u);

            uint maxCount = gameTableManager?.Entitlement?.GetEntry(objectId)?.MaxCount ?? 0u;
            if (maxCount != 0u && total > maxCount)
                total = maxCount;

            uint count = (uint)Math.Min(total, uint.MaxValue);
            return PrerequisiteCompare.Compare(comparison, count, value);
        }
    }
}
