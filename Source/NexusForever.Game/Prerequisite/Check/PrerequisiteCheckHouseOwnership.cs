using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 182: evaluator case <c>0xb6</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x620</c> (<c>1404a1070</c>) with the unit, the
    /// comparison and the <b>second unit</b>. The unit must be a Player or Ghost and the second unit present. The residence is the loaded one with a plot whose HousingPlotInfo world socket id equals
    /// the second unit's socket id (<c>1405b15c0</c>, unit <c>+0x172c</c>); without one the result is false. Otherwise the residence field at <c>+0x20</c> is compared with the unit field at
    /// <c>+0x1a8</c> through the general comparison. ObjectId and Value are never read.
    /// </summary>
    /// <remarks>
    /// Read as "the player owns the residence this housing unit stands on". Residence <c>+0x20</c> is the second half of the residence key type 186 looks up in the neighbour list; that it and unit
    /// <c>+0x1a8</c> are owner and character ids was not read natively. Retail: 2 slots, both Equal, gating 42 housing spells ("Plant Seeds - ...") and housing prop creatures (visibility, activate
    /// spell and animation state), in row 17536 as "owner or neighbour" together with type 186.
    /// The previous implementation tested the player's own residence wherever the player was, so any house owner passed on somebody else's plot, and compared the result with Value (0 on both slots).
    /// Only Equal and NotEqual are evaluated here. Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.HouseOwnership)]
    public class PrerequisiteCheckHouseOwnership : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IResidence residence = ResolveResidence(player, parameters);
            if (residence == null)
                return false;

            return PrerequisiteCompare.CompareFlag(comparison, residence.OwnerId == player.CharacterId);
        }

        /// <summary>
        /// The residence the second unit of the evaluation stands on; null when there is no player, no second unit or no residence for it.
        /// </summary>
        internal static IResidence ResolveResidence(IPlayer player, IPrerequisiteParameters parameters)
        {
            IWorldEntity second = parameters?.Target;
            if (player == null || second == null)
                return null;

            return (second.Map as IResidenceMapInstance)?.GetResidenceForEntity(second);
        }
    }
}
