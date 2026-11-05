using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using NexusForever.Game.Account.Currency;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.Session;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    [Fact]
    public async Task CommunityTokenCreationDefersSuccessUntilThePayerAndCompleteGraphCommit()
    {
        using var f = await Create(community: true, tokens: true);
        f.Register();
        Assert.Empty(f.Packets<ServerGuildResult>());
        using var source = f.Source.Context(); Assert.Empty(source.Guild); Assert.Empty(source.Residence);
        using var auth = f.TokenAuth.Context(); Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount);
    }

    [Fact]
    public async Task CommunityTokenCreationSettlesTheDurableAuthDebitWithoutChargingCharacterCredits()
    {
        using var f = await Create(community: true, tokens: true);
        f.Register(); await f.Flush(); await f.Flush();
        using var source = f.Source.Context(); using var auth = f.TokenAuth.Context();
        Assert.Equal(15ul, Assert.Single(auth.AccountCurrency).Amount);
        var debit = Assert.Single(auth.AccountCurrencyOperation);
        Assert.StartsWith("community-create-debit:", debit.OperationKey);
        Assert.Equal(5ul, debit.Amount); Assert.True(debit.Subtract);
        Assert.Single(source.Guild); Assert.Single(source.GuildMember); Assert.Single(source.Residence);
        Assert.Equal(2, source.ResidencePlot.Count()); Assert.Equal(5000ul, source.CharacterCurrency.Single().Amount);
        Assert.Contains(source.BankReconciliationReceipt, row => row.Payload.Contains("community-creation-tokens"));
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.Equal(15ul, f.Actor.Player.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task CommunityTokenCreationRefundsARejectedCharacterGraphBeforeRetrying()
    {
        using var f = await Create(community: true, tokens: true);
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_token_create BEFORE INSERT ON residence_plot BEGIN SELECT RAISE(ABORT, 'owned token graph failure'); END");
        f.Register(); await f.Flush();
        using (var source = f.Source.Context())
        using (var auth = f.TokenAuth.Context())
        {
            Assert.Empty(source.Guild); Assert.Empty(source.Residence); Assert.Empty(source.ResidencePlot);
            Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount);
            Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
            await source.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_token_create");
        }
        await f.Flush(); await f.Flush();
        using var stored = f.Source.Context(); using var payer = f.TokenAuth.Context();
        Assert.Single(stored.Guild); Assert.Single(stored.Residence); Assert.Equal(2, stored.ResidencePlot.Count());
        Assert.Equal(15ul, Assert.Single(payer.AccountCurrency).Amount);
        var payments = payer.AccountCurrencyOperation.ToArray();
        Assert.Equal(5ul, payments.Where(row => row.Subtract).Aggregate(0ul, (sum, row) => sum + row.Amount)
            - payments.Where(row => !row.Subtract).Aggregate(0ul, (sum, row) => sum + row.Amount));
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommunityTokenCreationReconcilesLostCommitRepliesWithoutAnotherNetPayment(bool authFailure)
    {
        var characterLost = new LostAcknowledgement();
        using var f = await Create(authFailure ? null : characterLost, community: true, tokens: true);
        var payerLost = new TokenLostAcknowledgement();
        if (authFailure) f.TokenAuth.Interceptor = payerLost;
        f.Register(); await TokenReady(f);
        using var source = f.Source.Context(); using var auth = f.TokenAuth.Context();
        Assert.True(authFailure ? payerLost.Observed : characterLost.Observed);
        Assert.Equal(15ul, Assert.Single(auth.AccountCurrency).Amount); Assert.Single(auth.AccountCurrencyOperation);
        Assert.Single(source.Guild); Assert.Single(source.Residence); Assert.Equal(2, source.ResidencePlot.Count());
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityTokenCreationCannotCommitAnUnpaidGraphWhenAuthRejectsItsDebit()
    {
        using var f = await Create(community: true, tokens: true);
        using (var setup = f.TokenAuth.Context()) await setup.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_community_debit BEFORE INSERT ON account_currency_operation BEGIN SELECT RAISE(ABORT, 'owned debit failure'); END");
        f.Register(); await f.Flush();
        using (var auth = f.TokenAuth.Context())
        using (var source = f.Source.Context())
        {
            Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount); Assert.Empty(auth.AccountCurrencyOperation);
            Assert.Empty(source.Guild); Assert.Empty(source.Residence);
            Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
            await auth.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_community_debit");
        }
        await TokenReady(f);
        using var paid = f.TokenAuth.Context(); Assert.Equal(15ul, Assert.Single(paid.AccountCurrency).Amount);
        Assert.Single(paid.AccountCurrencyOperation);
    }

    [Fact]
    public async Task CommunityTokenCreationAdmissionRecognizesTheCompleteGraphWithoutRefundOrRedelivery()
    {
        using var f = await Create(community: true, tokens: true); f.Register(); await TokenReady(f);
        using (var update = f.TokenAuth.Context())
            await update.AccountWorldSession.Where(row => row.AccountId == 12).ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 2ul));
        var admitted = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
        await VendorAccountPurchase.RecoverAtAdmissionAsync(admitted, f.TokenAuth.Database, f.Actor.Player.GetCharacterDatabase(), 1);
        Assert.Equal(15ul, Assert.Single(admitted.AccountCurrency).Amount);
        using var auth = f.TokenAuth.Context(); Assert.Single(auth.AccountCurrencyOperation);
        using var source = f.Source.Context(); Assert.Single(source.Guild); Assert.Single(source.Residence);
        Assert.Equal(2, source.ResidencePlot.Count());
    }

    [Fact]
    public async Task CommunityTokenCreationNewAdmissionCompletesAnUnfinishedRefundFromItsDurableAnchor()
    {
        using var f = await Create(community: true, tokens: true);
        using (var source = f.Source.Context()) await source.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_token_delivery BEFORE INSERT ON residence_plot BEGIN SELECT RAISE(ABORT, 'owned delivery failure'); END");
        using (var auth = f.TokenAuth.Context()) await auth.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_token_refund BEFORE INSERT ON account_currency_operation WHEN NEW.subtract = 0 BEGIN SELECT RAISE(ABORT, 'owned refund failure'); END");
        f.Register();
        await DriveTokens(f, () =>
        {
            using var read = f.TokenAuth.Context(); return read.AccountCurrencyOperation.Any(row => row.Subtract);
        });
        using (var update = f.TokenAuth.Context())
        {
            await update.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_token_refund");
            await update.AccountWorldSession.Where(row => row.AccountId == 12).ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 2ul));
        }
        var admitted = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
        await VendorAccountPurchase.RecoverAtAdmissionAsync(admitted, f.TokenAuth.Database, f.Actor.Player.GetCharacterDatabase(), 1);
        Assert.Equal(20ul, Assert.Single(admitted.AccountCurrency).Amount);
        // Let the retained original compensation task observe the same refund;
        // no replacement session is used to adopt or publish its old success.
        await f.Flush();
        using var payer = f.TokenAuth.Context();
        Assert.Single(payer.AccountCurrencyOperation.Where(row => row.Subtract));
        Assert.Single(payer.AccountCurrencyOperation.Where(row => !row.Subtract));
        using var sourceCheck = f.Source.Context(); Assert.Empty(sourceCheck.Guild); Assert.Empty(sourceCheck.Residence);
        Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityTokenCreationWalletReplyRetryDoesNotReplayAnAcceptedGrantOrPayment()
    {
        using var f = await Create(community: true, tokens: true); bool reject = true; int grants = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAccountCurrencyGrant) grants++;
            if (args[0] is ServerWalletUpdate && reject) throw new IOException("Owned wallet reply rejection.");
            return null;
        });
        f.Register(); await f.Flush(); reject = false; await TokenReady(f);
        Assert.Equal(1, grants);
        using var auth = f.TokenAuth.Context(); Assert.Single(auth.AccountCurrencyOperation);
        Assert.Equal(15ul, Assert.Single(auth.AccountCurrency).Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityTokenCreationKeepsItsAcceptedFeeWhileEarlierEarningPersists()
    {
        using var f = await Create(community: true, tokens: true); bool changed = false;
        f.Actor.OnAuth = () =>
        {
            if (changed) return; changed = true;
            f.Tables.GameFormula.GetEntry(1159).Dataint01 = 19;
            f.Actor.Player.Account.CurrencyManager.CurrencyAddAmount(AccountCurrencyType.ServiceToken, 2);
        };
        f.Register(); await TokenReady(f);
        using var auth = f.TokenAuth.Context(); Assert.Equal(17ul, Assert.Single(auth.AccountCurrency).Amount);
        Assert.Equal(5ul, auth.AccountCurrencyOperation.Single(row => row.OperationKey.StartsWith("community-create-debit:")).Amount);
    }

    [Fact]
    public async Task CommunityTokenCreationRejectsAnOrdinaryCreditReceiptAsAuthAuthority()
    {
        using var f = await Create(community: true, tokens: true);
        var protocol = new VendorAccountPurchase(f.Actor.Player.GetCharacterDatabase().CreateContext, f.TokenAuth.Database.CreateContext, 1);
        Assert.Throws<InvalidOperationException>(() => protocol.CreateCommunity(Guid.NewGuid().ToString("N"), 12, 42, 1, 1,
            "Paid guild", 5, 1, "{\"Kind\":\"community-creation-credits\",\"Receipt\":{}}"));
        using var auth = f.TokenAuth.Context(); Assert.Empty(auth.AccountCurrencyOperation); Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount);
    }

    [Fact]
    public async Task CommunityTokenCreationRejectsARecoveryRouteDifferentFromItsCompleteSealedGraph()
    {
        using var f = await Create(community: true, tokens: true); f.Register(); await TokenReady(f);
        using var auth = f.TokenAuth.Context(); var debit = Assert.Single(auth.AccountCurrencyOperation);
        var intent = JsonSerializer.Deserialize<VendorAccountPurchase.Intent>(debit.SourceJson)!;
        using var source = f.Source.Context(); string payload = source.BankReconciliationReceipt.Single(row => row.Id == intent.Id).Payload;
        using var foreign = new NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture(null); foreign.Seed();
        var protocol = new VendorAccountPurchase(() => foreign.Context(), f.TokenAuth.Database.CreateContext, 1);
        Assert.Throws<InvalidOperationException>(() => protocol.CreateCommunity(intent.Id, 12, 42, intent.Community.Guild,
            intent.Community.Residence, intent.Community.Name, 5, 1, payload));
        Assert.Single(auth.AccountCurrencyOperation); Assert.Equal(15ul, Assert.Single(auth.AccountCurrency).Amount);
    }

    [Fact]
    public async Task CommunityTokenCreationCannotChargeTheStalePayerOfAnotherAccountsCharacter()
    {
        using var f = await Create(community: true, tokens: true);
        using (var change = f.Source.Context()) await change.Character.Where(row => row.Id == 42)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 13u));
        f.Register(); await f.Flush();
        using var auth = f.TokenAuth.Context(); using var source = f.Source.Context();
        Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount); Assert.Empty(auth.AccountCurrencyOperation);
        Assert.Empty(source.Guild); Assert.Empty(source.Residence);
        Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityTokenCreationCannotDebitAfterItsAdmittedEpochIsSuperseded()
    {
        using var f = await Create(community: true, tokens: true);
        using (var change = f.TokenAuth.Context()) await change.AccountWorldSession.Where(row => row.AccountId == 12)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 2ul));
        f.Register(); await f.Flush();
        using var auth = f.TokenAuth.Context(); using var source = f.Source.Context();
        Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount); Assert.Empty(auth.AccountCurrencyOperation);
        Assert.Empty(source.Guild); Assert.Empty(source.Residence);
        Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityTokenCreationCannotPublishItsWalletOrSuccessToAReplacementPlayer()
    {
        using var f = await Create(community: true, tokens: true);
        f.Actor.OnAuth = () => f.Messages.SetProperty(nameof(IWorldSession.Player), null);
        f.Register(); await f.Flush(); await f.Flush();
        using var auth = f.TokenAuth.Context(); using var source = f.Source.Context();
        Assert.Equal(15ul, Assert.Single(auth.AccountCurrency).Amount); Assert.Single(auth.AccountCurrencyOperation);
        Assert.Single(source.Guild); Assert.Single(source.Residence);
        Assert.Empty(f.Packets<ServerAccountCurrencyGrant>());
        Assert.DoesNotContain(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
        Assert.False(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task CommunityTokenCreationPreservesAZeroAlternateFeeWithoutAnAuthDebit()
    {
        using var f = await Create(community: true, tokens: true);
        f.Tables.GameFormula.GetEntry(1159).Dataint01 = 0;
        f.Register(); await TokenReady(f);
        using var auth = f.TokenAuth.Context(); using var source = f.Source.Context();
        Assert.Equal(20ul, Assert.Single(auth.AccountCurrency).Amount); Assert.Empty(auth.AccountCurrencyOperation);
        Assert.Single(source.Guild); Assert.Single(source.Residence); Assert.Equal(5000ul, source.CharacterCurrency.Single().Amount);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    [Fact]
    public async Task CommunityTokenCreationRetriesAFailedWalletObservationWithoutAnotherDebit()
    {
        Fixture f = null; var failure = new TokenObservationFailure();
        using var owned = await Create(new TokenPrimaryCommit(() => f.TokenAuth.Interceptor = failure), community: true, tokens: true);
        f = owned; f.Register(); await TokenReady(f);
        Assert.True(failure.Observed);
        using var auth = f.TokenAuth.Context(); Assert.Equal(15ul, Assert.Single(auth.AccountCurrency).Amount);
        Assert.Single(auth.AccountCurrencyOperation);
        Assert.Single(f.Packets<ServerGuildResult>(), row => row.Result == GuildResult.YouCreated);
    }

    private static Task TokenReady(Fixture f) => DriveTokens(f, () => f.Session.CanProcessIncomingPackets);
    private static async Task DriveTokens(Fixture f, Func<bool> ready)
    {
        var elapsed = Stopwatch.StartNew();
        while (!ready())
        {
            f.Guilds.Update(61); f.Actor.Events.Update(.01); await Task.Delay(5);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(20), "Community token public flow did not reach its expected boundary.");
        }
        Assert.Empty(f.Actor.Errors);
    }

    private sealed class TokenLostAcknowledgement : DbTransactionInterceptor
    {
        internal bool Observed;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (Observed || data.Context is not AuthContext context || !context.ChangeTracker.Entries<AccountCurrencyOperationModel>()
                    .Any(row => row.Entity.OperationKey.StartsWith("community-create-debit:"))) return Task.CompletedTask;
            Observed = true; return Task.FromException(new IOException("Owned token debit acknowledgement loss."));
        }
    }

    private sealed class TokenPrimaryCommit(Action committed) : DbTransactionInterceptor
    {
        private bool observed;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (!observed && data.Context is NexusForever.Database.Character.CharacterContext context
                && context.BankReconciliationReceipt.Local.Any(row => row.Payload.Contains("community-creation-tokens")))
            { observed = true; committed(); }
            return Task.CompletedTask;
        }
    }

    private sealed class TokenObservationFailure : DbCommandInterceptor
    {
        internal bool Observed;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!Observed && command.CommandText.Contains("FROM \"account_currency\"", StringComparison.Ordinal))
            { Observed = true; throw new IOException("Owned Community wallet observation failure."); }
            return new(result);
        }
    }

    private sealed class TokenAuthStore : IDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), $"nf-community-token-{Guid.NewGuid():N}.sqlite");
        private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={path};Pooling=False" };
        internal AuthDatabase Database { get; }
        internal IInterceptor Interceptor;
        internal AccountModel Model { get; }
        internal AuthContext Context() => Interceptor == null ? new AuthContext(Config) : new TokenFaultContext(Config, Interceptor);
        internal TokenAuthStore()
        {
            Database = new TokenAuthDatabase(Context);
            Database.Initialise(Config);
            using var context = Context(); context.Database.EnsureCreated();
            Model = new AccountModel { Id = 12, Email = "community-token-12@example.local", WorldSessionEpoch = 1 };
            Model.AccountCurrency.Add(new AccountCurrencyModel { Id = 12, CurrencyId = (byte)AccountCurrencyType.ServiceToken, Amount = 20 });
            context.Account.Add(Model); context.AccountWorldSession.Add(new AccountWorldSessionModel { AccountId = 12, Epoch = 1 });
            context.SaveChanges();
        }
        public void Dispose() { if (File.Exists(path)) File.Delete(path); }
    }
    private sealed class TokenAuthDatabase(Func<AuthContext> contexts) : AuthDatabase
    {
        public override AuthContext CreateContext() => contexts();
    }
    private sealed class TokenFaultContext(DatabaseConnectionString config, IInterceptor interceptor) : AuthContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { base.OnConfiguring(builder); builder.AddInterceptors(interceptor); }
    }
}
