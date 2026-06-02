using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 182: handler table <c>1404a5150</c> housing ownership check via housing manager.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HouseOwnership)]
    public class PrerequisiteCheckHouseOwnership : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IResidence residence = player.ResidenceManager.Residence;
            uint owns = 0u;
            if (residence != null && residence.OwnerId == player.CharacterId)
            {
                owns = objectId == 0u || (uint)residence.PropertyInfoId == objectId ? 1u : 0u;
            }

            return PrerequisiteCompare.Compare(comparison, owns, value);
        }
    }
}
