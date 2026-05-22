using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;

namespace NexusForever.Game.Guild
{
    public static class RetailCommunityRules
    {
        public static GuildResult? ValidateCreate(IPlayer player)
        {
            if (player.SignatureEnabled)
                return null;

            uint fullSocial = player.Account.EntitlementManager
                .GetEntitlement(EntitlementType.FullSocialParticipation)?.Amount ?? 0u;
            if (fullSocial > 0u)
                return null;

            return GuildResult.PrivilegeRestricted;
        }

        public static bool HasPlayerResidence(IPlayer player) =>
            player.ResidenceManager.Residence != null;
    }
}
