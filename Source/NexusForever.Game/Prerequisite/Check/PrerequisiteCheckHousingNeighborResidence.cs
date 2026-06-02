using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 186: live case <c>0xba</c> vtable <c>+0x628</c> ->
    /// <c>Prerequisite_CheckHousingNeighborResidence_Table186</c> (<c>1404a10e0</c>).
    /// Native code gates NPC targets, resolves the active housing residence identity, and tests membership
    /// in the cached neighbor map fed by <c>ServerHousingNeighborUpdate</c> (<c>0x0519</c>).
    /// NF proxies "has at least one non-pending neighbor permission" on the player's residence.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HousingNeighborResidence)]
    public class PrerequisiteCheckHousingNeighborResidence : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
                return false;

            IResidence residence = player.ResidenceManager?.Residence;
            bool hasNeighbor = residence != null
                && residence.GetNeighbors().Any(neighbor => neighbor.PermissionLevel > 0);

            return PrerequisiteCompare.Compare(comparison, hasNeighbor ? 1u : 0u, 1u);
        }
    }
}
