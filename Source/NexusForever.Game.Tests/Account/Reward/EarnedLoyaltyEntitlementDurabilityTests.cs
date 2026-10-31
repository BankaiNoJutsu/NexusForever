using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Account.Reward;

public sealed class EarnedLoyaltyEntitlementDurabilityTests : IDisposable
{
    private const uint AccountId = 1;
    private const uint EarnedAccountItemId = 166;
    private const EntitlementType EarnedEntitlement = EntitlementType.WakeHereCooldownReduction;
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusEarnedEntitlement_{Guid.NewGuid():N}");
    private readonly DatabaseConnectionString connection;
    private readonly IGameTableManager tables;

    public EarnedLoyaltyEntitlementDurabilityTests()
    {
        Directory.CreateDirectory(directory);
        connection = new() { Provider = NexusForever.Database.DatabaseProvider.Sqlite, ConnectionString = $"Data Source={Path.Combine(directory, "auth.sqlite")}" };
        tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
        proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Rewards()));
        proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.AccountItems()));
        proxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 8 }));
        // Exact prepared build16042 Entitlement.tbl rows15/17/41. SHA256:
        // 7d84102f4da712194318e0e76d3761c59048d3e98a2bd3fe1af1b72c0f721abc.
        proxy.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(
            new EntitlementEntry { Id = 15, MaxCount = 1, Flags = 3 },
            new EntitlementEntry { Id = 17, MaxCount = 1, Flags = 3 },
            new EntitlementEntry { Id = 43, MaxCount = 10, Flags = 0, LocalizedTextIdName = 721799, LocalizedTextIdDescription = 721800 },
            new EntitlementEntry { Id = 41, MaxCount = 1, Flags = 0, LocalizedTextIdName = 721795, LocalizedTextIdDescription = 721796 }));
        using var context = Context();
        context.Database.EnsureCreated();
        var account = new AccountModel { Id = AccountId, Email = "earned-entitlement@example.test", S = "", V = "", GameToken = "", SessionKey = "" };
        AccountDefaultEntitlements.EnsureBaseline(account);
        context.Account.Add(account);
        context.SaveChanges();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void EarnedEntitlementRedemptionSurvivesAuthRollbackAndRetryWithoutLosingSourceOrDefaults(bool existingZeroEntitlement, bool failFirstFlush)
    {
        if (existingZeroEntitlement)
        {
            using var seed = Context();
            seed.AccountEntitlement.Add(new AccountEntitlementModel { Id = AccountId, EntitlementId = (byte)EarnedEntitlement, Amount = 0 });
            seed.SaveChanges();
        }
        Client client = ClientFor();
        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 2400);
        Save(client);
        string pendingGroup;
        using (var earned = Context())
        {
            AccountPendingItemModel reward = Assert.Single(earned.AccountPendingItem, row => row.AccountItemId == EarnedAccountItemId);
            pendingGroup = reward.GroupName;
            Assert.Equal("loyalty:1:9:2", pendingGroup);
            Assert.True(earned.AccountLoyaltySlot.Single(row => row.TrackId == 9 && row.Position == 2).Claimed);
            Assert.Equal(0u, earned.AccountEntitlement.Where(row => row.EntitlementId == (byte)EarnedEntitlement).Select(row => row.Amount).SingleOrDefault());
            AssertDefaults(earned);
        }
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.ClaimPendingItemGroup(client.Player, pendingGroup));
        Save(client);
        var source = Assert.Single(client.Inventory);
        Assert.Equal(EarnedAccountItemId, source.AccountItemId);
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.TakeItem(null, source.Id));
        Assert.Equal(1u, client.Entitlements.GetEntitlement(EarnedEntitlement).Amount);
        Assert.Empty(client.Inventory);

        if (failFirstFlush)
        {
            using (var setup = Context())
                setup.Database.ExecuteSqlRaw("CREATE TRIGGER reject_redemption BEFORE UPDATE ON account_loyalty BEGIN SELECT RAISE(ABORT, 'owned entitlement redemption failure'); END");
            Assert.Throws<DbUpdateException>(() => Save(client));
            using var rolledBack = Context();
            Assert.Equal(source.Id, Assert.Single(rolledBack.AccountInventory).InventoryId);
            Assert.Equal(0u, rolledBack.AccountEntitlement.Where(row => row.EntitlementId == (byte)EarnedEntitlement).Select(row => row.Amount).SingleOrDefault());
            AssertDefaults(rolledBack);
            rolledBack.Database.ExecuteSqlRaw("DROP TRIGGER reject_redemption");
        }

        Save(client);
        using (var committed = Context())
        {
            Assert.Empty(committed.AccountInventory);
            Assert.DoesNotContain(committed.AccountPendingItem, row => row.AccountItemId == EarnedAccountItemId);
            Assert.True(committed.AccountLoyaltySlot.Single(row => row.TrackId == 9 && row.Position == 2).Claimed);
            AssertDefaults(committed);
        }
        Client relog = ClientFor();
        Assert.Empty(relog.Inventory);
        Assert.Equal(1u, relog.Entitlements.GetEntitlement(EarnedEntitlement)?.Amount ?? 0u);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EarnedBenefitAndSourceDeletionFollowActualOuterTransaction(bool commit)
    {
        Client client = RedeemEarnedBenefit();
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            Assert.Equal(1u, context.AccountEntitlement.AsNoTracking().Single(row => row.EntitlementId == (byte)EarnedEntitlement).Amount);
            Assert.Empty(context.AccountInventory);
            if (commit) transaction.Commit(); else transaction.Rollback();
        }
        using (var durable = Context())
        {
            Assert.Equal(commit ? 0 : 1, durable.AccountInventory.Count());
            Assert.Equal(commit ? 1u : 0u, durable.AccountEntitlement.Where(row => row.EntitlementId == (byte)EarnedEntitlement).Select(row => row.Amount).SingleOrDefault());
        }
        Save(client);
        Assert.Equal(1u, ClientFor().Entitlements.GetEntitlement(EarnedEntitlement).Amount);
        using var verify = Context(); Assert.Empty(verify.AccountInventory); AssertDefaults(verify);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SavepointUncertaintyRetainsIdempotentBenefitAndSourceIntent(bool afterWrites)
    {
        Client client = RedeemEarnedBenefit();
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            if (!afterWrites) transaction.CreateSavepoint("benefit");
            client.Account.Save(context); context.SaveChanges();
            if (afterWrites) transaction.CreateSavepoint("benefit");
            transaction.RollbackToSavepoint("benefit"); transaction.Commit();
        }
        Save(client); Save(client);
        using var verify = Context();
        Assert.Equal(1u, verify.AccountEntitlement.Single(row => row.EntitlementId == (byte)EarnedEntitlement).Amount);
        Assert.Empty(verify.AccountInventory); AssertDefaults(verify);
    }

    [Fact]
    public void DisposedUnflushedContextReleasesCreateIntentForFreshUnit()
    {
        Client client = ClientFor(); client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
        using (var discarded = Context()) client.Account.Save(discarded);
        Save(client);
        using var verify = Context(); Assert.Equal(1u, verify.AccountEntitlement.Single(row => row.EntitlementId == 43).Amount); AssertDefaults(verify);
    }

    [Fact]
    public void TransactionOnlyDisposalCannotBeAcknowledgedByLaterEmptyFlush()
    {
        Client client = RedeemEarnedBenefit();
        using (var context = Context())
        {
            var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            transaction.Dispose(); context.SaveChanges();
        }
        Save(client);
        using var verify = Context(); Assert.Empty(verify.AccountInventory);
        Assert.Equal(1u, verify.AccountEntitlement.Single(row => row.EntitlementId == (byte)EarnedEntitlement).Amount); AssertDefaults(verify);
    }

    [Fact]
    public void SameContextRefreshBeforeSqlIncludesLatestAmount()
    {
        Client client = ClientFor(); client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
        using (var context = Context())
        {
            client.Account.Save(context);
            client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
            client.Account.Save(context); context.SaveChanges();
        }
        using var verify = Context(); Assert.Equal(2u, verify.AccountEntitlement.Single(row => row.EntitlementId == 43).Amount); AssertDefaults(verify);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OlderTerminalCompletionDoesNotDiscardLaterAmountMutation(bool commit)
    {
        Client client = ClientFor(); client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
            if (commit) transaction.Commit(); else transaction.Rollback();
        }
        Save(client);
        using var verify = Context(); Assert.Equal(2u, verify.AccountEntitlement.Single(row => row.EntitlementId == 43).Amount); AssertDefaults(verify);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PostSqlReentryPoisonsUnitEvenWhenAmountReturnsToSameNumericValue(bool sameNumericValue)
    {
        Client client = ClientFor(); client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            if (sameNumericValue) client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, -1);
            client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
            Assert.Throws<InvalidOperationException>(() => client.Account.Save(context));
            Assert.Throws<InvalidOperationException>(() => transaction.Commit());
            transaction.Rollback();
            Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        }
        Save(client);
        using var verify = Context();
        Assert.Equal(sameNumericValue ? 1u : 2u, verify.AccountEntitlement.Single(row => row.EntitlementId == 43).Amount); AssertDefaults(verify);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectedOverlapCannotAcknowledgeUnrelatedChildOrCommitPartialCurrencyWork(bool alreadyFlushed)
    {
        Client client = ClientFor(); client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
        using var owner = Context(); client.Account.Save(owner);
        client.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyExtraAuctions, 1);
        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
        bool unrelatedDirty = true;
        client.Rbac.SetMethodHandler(nameof(IAccount.Save), _ => { unrelatedDirty = false; return null; });
        using (var rejected = Context())
        {
            using var transaction = rejected.Database.BeginTransaction();
            client.Currency.Save(rejected);
            if (alreadyFlushed) rejected.SaveChanges();
            Assert.Throws<InvalidOperationException>(() => client.Account.Save(rejected));
            Assert.True(unrelatedDirty); // Real Account.Save must reserve before legacy children clear dirty intent.
            Assert.Throws<InvalidOperationException>(() => transaction.Commit());
            transaction.Rollback();
            owner.SaveChanges(); // Original owner finishes; rejection must remain permanent.
            Assert.Throws<InvalidOperationException>(() => rejected.SaveChanges());
        }
        Assert.True(unrelatedDirty);
        Save(client);
        Assert.False(unrelatedDirty);
        using var verify = Context();
        Assert.Equal(2u, verify.AccountEntitlement.Single(row => row.EntitlementId == 43).Amount);
        Assert.Equal(800ul, verify.AccountCurrency.Single(row => row.CurrencyId == 8).Amount);
        Assert.Single(verify.AccountCosmicOperation); AssertDefaults(verify);
    }

    private Client RedeemEarnedBenefit()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 2400); Save(client);
        string group;
        using (var context = Context()) group = context.AccountPendingItem.Single(row => row.AccountItemId == EarnedAccountItemId).GroupName;
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.ClaimPendingItemGroup(client.Player, group)); Save(client);
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.TakeItem(null, Assert.Single(client.Inventory).Id));
        return client;
    }

    private static void AssertDefaults(AuthContext context)
    {
        Assert.Equal(1u, context.AccountEntitlement.Single(row => row.EntitlementId == AccountDefaultEntitlements.EconomyParticipation).Amount);
        Assert.Equal(1u, context.AccountEntitlement.Single(row => row.EntitlementId == AccountDefaultEntitlements.FullSocialParticipation).Amount);
    }

    private Client ClientFor()
    {
        using var context = Context();
        AccountModel model = context.Account.AsNoTracking().Include(row => row.AccountCurrency).Include(row => row.AccountPendingItem)
            .Include(row => row.AccountInventory).Include(row => row.AccountEntitlement).Include(row => row.Loyalty).Include(row => row.LoyaltySlots).Single();
        var account = new NexusForever.Game.Account.Account();
        Set(account, nameof(IAccount.Id), AccountId);
        Set(account, nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        var rbac = RecordingDispatchProxy<IAccountRBACManager>.Create(out var rbacProxy);
        Set(account, nameof(IAccount.RbacManager), rbac);
        Set(account, nameof(IAccount.GenericUnlockManager), RecordingDispatchProxy<IGenericUnlockManager>.Create(out _));
        Set(account, nameof(IAccount.CostumeManager), RecordingDispatchProxy<IAccountCostumeManager>.Create(out _));
        Set(account, nameof(IAccount.KeybindingManager), RecordingDispatchProxy<IAccountKeybindingManager>.Create(out _));
        Set(account, nameof(IAccount.RewardRotationGrantManager), new AccountRewardRotationGrantManager(account, model));
        var currency = new AccountCurrencyManager(account, model, tables); Set(account, nameof(IAccount.CurrencyManager), currency);
        var entitlements = new AccountEntitlementManager(account, model, null, tables); Set(account, nameof(IAccount.EntitlementManager), entitlements);
        var inventory = new AccountInventoryManager(account, model, null, gameTableManager: tables); Set(account, nameof(IAccount.InventoryManager), inventory);
        var database = new AuthDatabase(); database.Initialise(connection);
        var loyalty = new AccountLoyaltyManager(account, tables, database); Set(account, nameof(IAccount.LoyaltyManager), loyalty);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = 100 });
        return new Client(account, player, currency, entitlements, inventory, rbacProxy);
    }

    private static void Set(object instance, string property, object value) => instance.GetType()
        .GetField($"<{property}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, value);

    private AuthContext Context() => new(connection);
    private void Save(Client client) { using var context = Context(); client.Account.Save(context); context.SaveChanges(); }
    private sealed record Client(IAccount Account, IPlayer Player, AccountCurrencyManager Currency, AccountEntitlementManager Entitlements, AccountInventoryManager Inventory,
        RecordingDispatchProxy<IAccountRBACManager> Rbac);

    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(connection.ConnectionString);
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
