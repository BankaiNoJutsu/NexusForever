using System.Linq;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public static class AccountDefaultEntitlements
    {
        public const byte EconomyParticipation = 15;
        public const byte FullSocialParticipation = 17;

        private static readonly byte[] BaselineEntitlementIds =
        [
            EconomyParticipation,
            FullSocialParticipation
        ];

        public static bool EnsureBaseline(AccountModel account)
        {
            bool changed = false;

            foreach (byte entitlementId in BaselineEntitlementIds)
            {
                AccountEntitlementModel entitlement = account.AccountEntitlement
                    .SingleOrDefault(e => e.EntitlementId == entitlementId);
                if (entitlement == null)
                {
                    account.AccountEntitlement.Add(new AccountEntitlementModel
                    {
                        EntitlementId = entitlementId,
                        Amount        = 1u
                    });
                    changed = true;
                    continue;
                }

                if (entitlement.Amount == 0u)
                {
                    entitlement.Amount = 1u;
                    changed = true;
                }
            }

            return changed;
        }
    }
}
