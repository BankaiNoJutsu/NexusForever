using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Game.Tests.Account.Entitlement;

public class AccountDefaultEntitlementsTests
{
    [Fact]
    public void EnsureBaseline_WithNoRowsAddsNonTrialEntitlements()
    {
        var account = new AccountModel();

        bool changed = AccountDefaultEntitlements.EnsureBaseline(account);

        Assert.True(changed);
        Assert.Collection(account.AccountEntitlement.OrderBy(e => e.EntitlementId),
            entitlement =>
            {
                Assert.Equal(AccountDefaultEntitlements.EconomyParticipation, entitlement.EntitlementId);
                Assert.Equal(1u, entitlement.Amount);
            },
            entitlement =>
            {
                Assert.Equal(AccountDefaultEntitlements.FullSocialParticipation, entitlement.EntitlementId);
                Assert.Equal(1u, entitlement.Amount);
            });
    }

    [Fact]
    public void EnsureBaseline_WithZeroRowsRaisesExistingEntitlements()
    {
        var account = new AccountModel
        {
            AccountEntitlement =
            [
                new AccountEntitlementModel
                {
                    EntitlementId = AccountDefaultEntitlements.EconomyParticipation,
                    Amount        = 0u
                },
                new AccountEntitlementModel
                {
                    EntitlementId = AccountDefaultEntitlements.FullSocialParticipation,
                    Amount        = 0u
                }
            ]
        };

        bool changed = AccountDefaultEntitlements.EnsureBaseline(account);

        Assert.True(changed);
        Assert.All(account.AccountEntitlement, entitlement => Assert.Equal(1u, entitlement.Amount));
    }

    [Fact]
    public void EnsureBaseline_WithPositiveRowsPreservesExistingAmounts()
    {
        var account = new AccountModel
        {
            AccountEntitlement =
            [
                new AccountEntitlementModel
                {
                    EntitlementId = AccountDefaultEntitlements.EconomyParticipation,
                    Amount        = 2u
                },
                new AccountEntitlementModel
                {
                    EntitlementId = AccountDefaultEntitlements.FullSocialParticipation,
                    Amount        = 3u
                }
            ]
        };

        bool changed = AccountDefaultEntitlements.EnsureBaseline(account);

        Assert.False(changed);
        Assert.Equal(2u, account.AccountEntitlement.Single(e => e.EntitlementId == AccountDefaultEntitlements.EconomyParticipation).Amount);
        Assert.Equal(3u, account.AccountEntitlement.Single(e => e.EntitlementId == AccountDefaultEntitlements.FullSocialParticipation).Amount);
    }
}
