using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Authentication;
using CharacterFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Account.Currency;

public sealed partial class AccountCurrencyDurabilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealmAdmissionRecoversLocalVendorDebitsBeforePublishingWallets(bool validKey)
    {
        const string key = "00112233445566778899AABBCCDDEEFF";
        using var auth = new Fixture(providerOptions);
        using var character = new CharacterFixture(options: null);
        character.Seed();
        await SeedVendorPayment(auth, character);

        var authDatabase = new AuthDatabase();
        using (var seed = auth.Context())
        {
            authDatabase.Initialise(new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
            seed.Account.Single().SessionKey = key;
            await seed.SaveChangesAsync();
        }
        var characterDatabase = new CharacterDatabase();
        using (var seed = character.Context())
            characterDatabase.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = seed.Database.GetDbConnection().ConnectionString });

        await Pay(1, 15, AccountCurrencyType.ProtostarPromissoryNote, commit: false);
        await Pay(1, 9, AccountCurrencyType.Omnibit, commit: true);
        await Pay(2, 7, AccountCurrencyType.NCoin, commit: false);

        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var databaseProxy);
        databaseProxy.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) =>
            method.GetGenericArguments()[0] == typeof(AuthDatabase) ? authDatabase : characterDatabase);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var handler = new ClientHelloRealmHandler(databases, realm);
        AccountModel firstAdmission = null;
        for (int attempt = 0; attempt < (validKey ? 2 : 1); attempt++)
        {
            var events = new EventQueue();
            Exception error = null;
            events.UnhandledExceptionHandler = exception => error = exception;
            var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
            network.SetProperty(nameof(IWorldSession.Events), events);
            AccountModel initialized = null;
            network.SetMethodHandler(nameof(IWorldSession.Initialise), args => { initialized = Assert.IsType<AccountModel>(args[0]); return null; });
            var request = new ClientHelloRealm();
            typeof(ClientHelloRealm).GetProperty(nameof(ClientHelloRealm.AccountId))!.SetValue(request, 77u);
            typeof(ClientHelloRealm).GetProperty(nameof(ClientHelloRealm.AccountString))!.SetValue(request, "wallet-77@example.local");
            typeof(ClientHelloRealm).GetProperty(nameof(ClientHelloRealm.SessionKey))!.SetValue(request, Convert.FromHexString(validKey ? key : new string('F', 32)));

            handler.HandleMessage(session, request);
            Assert.Null(initialized);
            Assert.False(session.CanProcessIncomingPackets);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (events.PendingEvents) { events.Update(.01); await Task.Delay(5, timeout.Token); }
            if (validKey)
            {
                Assert.Null(error);
                Assert.NotNull(initialized);
                Assert.Equal((ulong)attempt + 1, initialized.WorldSessionEpoch);
                Assert.Equal(new ulong[] { 61, 43, 100 }, initialized.AccountCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
                firstAdmission ??= initialized;
                Assert.Single(network.GetInvocations(nameof(IWorldSession.SetEncryptionKey)));
                Assert.True(session.CanProcessIncomingPackets);
            }
            else
            {
                Assert.IsType<InvalidPacketValueException>(error);
                Assert.Null(initialized);
                Assert.Empty(network.GetInvocations(nameof(IWorldSession.SetEncryptionKey)));
            }
        }
        if (validKey)
        {
            var stale = await Assert.ThrowsAsync<InvalidOperationException>(() => VendorAccountPurchase.RecoverAtAdmissionAsync(firstAdmission, authDatabase, characterDatabase, 1));
            Assert.Contains("admitted account epoch", stale.Message);
        }
        using var storedAuth = auth.Context();
        Assert.Equal(3, storedAuth.AccountCurrencyOperation.Count(row => row.Subtract));
        Assert.Equal(validKey ? 1 : 0, storedAuth.AccountCurrencyOperation.Count(row => !row.Subtract));
        Assert.Equal(validKey ? new ulong[] { 61, 43, 100 } : [61, 43, 85],
            storedAuth.AccountCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
        using var storedCharacter = character.Context();
        Assert.Equal(validKey ? 2 : 1, storedCharacter.BankReconciliationReceipt.Count());
        Assert.Equal(912ul, storedCharacter.Item.Single().Id);

        async Task Pay(ushort purchaseRealm, ulong cost, AccountCurrencyType currency, bool commit)
        {
            var checkout = new VendorAccountPurchase(characterDatabase.CreateContext, authDatabase.CreateContext, purchaseRealm);
            var intent = checkout.Create(77, 42, 7001, 1, new string('A', 64), [new(currency, cost)]);
            await using var source = characterDatabase.CreateContext();
            await using var transaction = await source.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            Assert.Equal(VendorAccountAuthorization.Authorized, await checkout.AuthorizeAsync(source, intent,
                unit => unit.Item.Add(new() { Id = 911ul + purchaseRealm, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory, StackCount = 1 })));
            await source.SaveChangesAsync();
            if (commit) await transaction.CommitAsync();
            else await transaction.RollbackAsync();
        }
    }

    [Theory]
    [InlineData("delivered")]
    [InlineData("abandoned")]
    [InlineData("resumed")]
    [InlineData("auth-ack-lost")]
    [InlineData("refund-ack-lost")]
    [InlineData("cancel-ack-lost")]
    [InlineData("concurrent-recovery")]
    public async Task VendorDebitRecoveryMakesDeliveryAndRefundMutuallyExclusive(string outcome)
    {
        using var auth = new Fixture(providerOptions);
        using var character = new CharacterFixture(options: null);
        character.Seed();
        await SeedVendorPayment(auth, character);
        var checkout = new VendorAccountPurchase(() => character.Context(), () => auth.Context(outcome == "auth-ack-lost" ? new CommitFailure(true) : null), 1);
        var intent = checkout.Create(77, 42, 7001, 2, new string('A', 64), [new(AccountCurrencyType.ProtostarPromissoryNote, 15), new(AccountCurrencyType.Omnibit, 9)]);
        bool delivered = outcome is "delivered" or "auth-ack-lost" or "concurrent-recovery";
        await AuthorizeAndWrite(delivered);
        if (outcome == "abandoned")
        {
            // Reusing the ID with a wholly different wallet must still hit
            // the original Auth identity after the Character rollback.
            var conflicting = intent with { QuoteFingerprint = new string('B', 64), Costs = [new(AccountCurrencyType.NCoin, 1)] };
            await using var source = character.Context();
            await using var transaction = await source.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await Assert.ThrowsAsync<InvalidOperationException>(() => checkout.AuthorizeAsync(source, conflicting, _ => { }));
            Assert.True(source.IsSaveRejected);
            await transaction.RollbackAsync();
        }
        if (outcome == "resumed")
        {
            await AuthorizeAndWrite(true);
            delivered = true;
        }

        AccountCurrencyOperationModel debit;
        using (var read = auth.Context())
        {
            debit = read.AccountCurrencyOperation.AsNoTracking().Single(row => row.CurrencyId == (byte)AccountCurrencyType.Omnibit);
            Assert.Equal(new ulong[] { 61, 50, 85 }, read.AccountCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
        }

        var restarted = new VendorAccountPurchase(() => character.Context(), () => auth.Context(outcome == "refund-ack-lost" ? new CommitFailure(true) : null), 1);
        var expected = delivered ? VendorAccountAuthorization.Delivered : VendorAccountAuthorization.Cancelled;
        if (outcome == "cancel-ack-lost")
        {
            var interruption = new VendorAccountPurchase(() => character.Context(new CommitFailure(true)), () => auth.Context(), 1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => interruption.RecoverAsync(debit));
            using var stillCharged = auth.Context();
            Assert.Equal(2, stillCharged.AccountCurrencyOperation.Count());
        }
        Assert.Equal(expected, await restarted.RecoverAsync(debit));
        Assert.Equal(expected, await restarted.RecoverAsync(debit));
        await using (var retry = character.Context())
        await using (var tx = await retry.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            Assert.Equal(expected, await restarted.AuthorizeAsync(retry, intent, _ => throw new InvalidOperationException("Terminal delivery replayed.")));
            Assert.Empty(retry.ChangeTracker.Entries());
            await tx.CommitAsync();
        }

        using var storedAuth = auth.Context();
        Assert.Equal(delivered ? new ulong[] { 61, 50, 85 } : [70, 50, 100], storedAuth.AccountCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
        Assert.Equal(2, storedAuth.AccountCurrencyOperation.Count(row => row.Subtract));
        Assert.Equal(delivered ? 0 : 2, storedAuth.AccountCurrencyOperation.Count(row => !row.Subtract));
        using var storedCharacter = character.Context();
        Assert.Single(storedCharacter.BankReconciliationReceipt);
        if (delivered) Assert.Equal(2u, storedCharacter.Item.Single().StackCount);
        else Assert.Empty(storedCharacter.Item);

        async Task AuthorizeAndWrite(bool commit)
        {
            await using var source = character.Context();
            await using var tx = await source.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            Assert.Equal(VendorAccountAuthorization.Authorized, await checkout.AuthorizeAsync(source, intent,
                context => context.Item.Add(new() { Id = 912, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory, StackCount = 2 })));
            await source.SaveChangesAsync();
            if (outcome == "concurrent-recovery")
            {
                using var read = auth.Context();
                var paid = read.AccountCurrencyOperation.AsNoTracking().Single(row => row.CurrencyId == (byte)AccountCurrencyType.Omnibit);
                var observing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var competing = new VendorAccountPurchase(() => { observing.TrySetResult(); return character.Context(); }, () => auth.Context(), 1);
                var result = Task.Run(() => competing.RecoverAsync(paid));
                await observing.Task.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.False(result.IsCompleted);
                await tx.CommitAsync();
                Assert.Equal(VendorAccountAuthorization.Delivered, await result.WaitAsync(TimeSpan.FromSeconds(15)));
                return;
            }
            if (commit) await tx.CommitAsync();
            else await tx.RollbackAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedVendorEnrollmentCannotChangeEitherWalletOrDeliverAnItem(bool earlyFlush)
    {
        using var auth = new Fixture(providerOptions);
        using var character = new CharacterFixture(options: null);
        character.Seed();
        await SeedVendorPayment(auth, character);
        var checkout = new VendorAccountPurchase(() => character.Context(), () => auth.Context(), 1);
        var intent = checkout.Create(77, 42, 7001, 1, new string('A', 64),
            [new(AccountCurrencyType.Omnibit, 9), new(AccountCurrencyType.ProtostarPromissoryNote, earlyFlush ? 15ul : 101ul)]);
        await using (var source = character.Context())
        await using (var tx = await source.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            await Assert.ThrowsAnyAsync<Exception>(() => checkout.AuthorizeAsync(source, intent, context =>
            {
                context.Item.Add(new() { Id = 912, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory, StackCount = 1 });
                if (earlyFlush) context.SaveChanges();
            }));
            Assert.True(source.IsSaveRejected);
            await tx.RollbackAsync();
        }

        using var storedAuth = auth.Context();
        Assert.Equal(new ulong[] { 70, 50, 100 }, storedAuth.AccountCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
        Assert.Empty(storedAuth.AccountCurrencyOperation);
        using var storedCharacter = character.Context();
        Assert.Empty(storedCharacter.BankReconciliationReceipt);
        Assert.Empty(storedCharacter.Item);
    }

    private static async Task SeedVendorPayment(Fixture auth, CharacterFixture character)
    {
        using (var seed = character.Context())
        {
            seed.Character.Single().AccountId = 77;
            await seed.SaveChangesAsync();
        }
        using var wallet = auth.Context();
        wallet.AccountCurrency.Add(new() { Id = 77, CurrencyId = (byte)AccountCurrencyType.Omnibit, Amount = 70 });
        wallet.AccountCurrency.Add(new() { Id = 77, CurrencyId = (byte)AccountCurrencyType.NCoin, Amount = 50 });
        await wallet.SaveChangesAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VendorPaymentRequiresDurableStorageOnBothSides(bool ephemeralAuth)
    {
        using var auth = new Fixture(providerOptions);
        using var character = new CharacterFixture(options: null);
        var checkout = new VendorAccountPurchase(
            () => ephemeralAuth ? character.Context() : new CharacterContext(new DbContextOptionsBuilder<CharacterContext>()
                .UseSqlite("Data Source=temporary;Mode=Memory").Options),
            () => ephemeralAuth ? new AuthContext(new DbContextOptionsBuilder<AuthContext>().UseSqlite("Data Source=:memory:").Options) : auth.Context(), 1);

        var error = Assert.Throws<InvalidOperationException>(() => checkout.Create(77, 42, 7001, 1, new string('A', 64),
            [new(AccountCurrencyType.ProtostarPromissoryNote, 15)]));
        Assert.Contains("durable database storage", error.Message);
    }
}
