using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 186: evaluator case <c>0xba</c> calls manager vtable <c>+0x628</c> (<c>Prerequisite_CheckHousingNeighborResidence_Table186</c>, <c>1404a10e0</c>) with the unit, the comparison
    /// and the <b>second unit</b>. The unit must be a Player or Ghost and the second unit present; the residence is resolved from the second unit's world socket exactly as for type 182
    /// (<see cref="PrerequisiteCheckHouseOwnership"/>), and without one the result is false. The flag is "the residence key (fields <c>+8</c> and <c>+0x20</c>) has a non-empty entry in the local
    /// player's neighbour list", the list <c>ServerHousingNeighborUpdate</c> (<c>0x0519</c>) maintains; Equal returns it, NotEqual its negation. ObjectId and Value are never read.
    /// </summary>
    /// <remarks>
    /// Read as "the player is a neighbour of the residence this housing unit stands on". Retail: 1 slot, Equal, in row 17536 as "owner or neighbour" with type 182.
    /// The previous implementation asked whether the player's own residence had any neighbour with a permission level above 0, wherever the player was.
    /// Status: Implemented, inferred. See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.HousingNeighborResidence)]
    public class PrerequisiteCheckHousingNeighborResidence : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IResidence residence = PrerequisiteCheckHouseOwnership.ResolveResidence(player, parameters);
            if (residence == null)
                return false;

            return PrerequisiteCompare.CompareFlag(comparison, residence.HasNeighbor(player.CharacterId));
        }
    }
}
