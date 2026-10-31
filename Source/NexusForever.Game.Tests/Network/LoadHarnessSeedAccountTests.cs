using NexusForever.Cryptography;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Game.Tests.Network;

public sealed class LoadHarnessSeedAccountTests
{
    [Fact]
    public void NewSeedAccountHasExactlyCanonicalDefaultsWithoutUnearnedRewards()
    {
        AccountModel account = SeedCommand.CreateNewAccountModel("loadtest9601@example.local", "loadtest");

        Assert.Equal(new[] { (Id: (byte)15, Amount: 1u), (Id: (byte)17, Amount: 1u) },
            account.AccountEntitlement.OrderBy(row => row.EntitlementId).Select(row => (row.EntitlementId, row.Amount)));
        Assert.DoesNotContain(account.AccountEntitlement, row => row.EntitlementId is 7 or 44);
        Assert.Empty(account.AccountInventory);
        Assert.Empty(account.AccountPendingItem);
        Assert.Empty(account.AccountCurrency);
        Assert.Empty(account.AccountGenericUnlock);
        Assert.Empty(account.AccountRewardRotationGrant);
        Assert.Null(account.Loyalty);
        Assert.Empty(account.LoyaltySlots);
        // Run still assigns its existing role and character profile after creating this Auth model.
        Assert.Empty(account.AccountRole);
        Assert.Equal(0u, account.Id);
        Assert.False(AccountDefaultEntitlements.EnsureBaseline(account));
    }

    [Theory]
    [InlineData("loadtest9602@example.local", "loadtest")]
    [InlineData("LOADTEST9603@EXAMPLE.LOCAL", "CaseSensitivePassword")]
    [InlineData("loadtest9604@example.local", "páss:with space")]
    public void NewSeedAccountRetainsTheRealSrpCredentialsAndUtcCreationTime(string email, string password)
    {
        DateTime before = DateTime.UtcNow;
        AccountModel account = SeedCommand.CreateNewAccountModel(email, password);
        DateTime after = DateTime.UtcNow;

        Assert.Equal(email, account.Email);
        Assert.Equal(DateTimeKind.Utc, account.CreateTime.Kind);
        Assert.InRange(account.CreateTime, before, after);
        byte[] salt = Convert.FromHexString(account.S);
        Assert.Equal(16, salt.Length);
        Assert.Equal(Convert.ToHexString(salt), account.S);
        byte[] verifier = Srp6Provider.GenerateVerifier(salt, email.ToLower(), password);
        Assert.Equal(Convert.ToHexString(verifier), account.V);
        Assert.InRange(verifier.Length, 1, 129);
    }

    [Fact]
    public void EachNewSeedModelOwnsItsBaselineRowsIndependently()
    {
        AccountModel first = SeedCommand.CreateNewAccountModel("loadtest9605@example.local", "loadtest");
        AccountModel second = SeedCommand.CreateNewAccountModel("loadtest9606@example.local", "loadtest");

        first.AccountEntitlement.Single(row => row.EntitlementId == 15).Amount = 0;
        first.AccountEntitlement.Add(new AccountEntitlementModel { EntitlementId = 44, Amount = 1 });

        Assert.Equal(new[] { (Id: (byte)15, Amount: 1u), (Id: (byte)17, Amount: 1u) },
            second.AccountEntitlement.OrderBy(row => row.EntitlementId).Select(row => (row.EntitlementId, row.Amount)));
        Assert.NotSame(first.AccountEntitlement, second.AccountEntitlement);
        Assert.NotSame(first.AccountEntitlement.Single(row => row.EntitlementId == 17),
            second.AccountEntitlement.Single(row => row.EntitlementId == 17));
    }
}
