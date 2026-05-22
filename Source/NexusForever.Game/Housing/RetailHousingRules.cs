using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Retail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Housing
{
    public static class RetailHousingRules
    {
        /// <summary>
        /// Returns a blocking <see cref="HousingResult"/> when the player cannot create or access housing yet.
        /// </summary>
        public static HousingResult? ValidateResidenceAccess(IPlayer player)
        {
            if (player.Level >= RetailCertainRules.HousingUnlockLevel)
                return null;

            if (player.ResidenceManager.Residence != null)
                return null;

            return HousingResult.Failed;
        }
    }
}
