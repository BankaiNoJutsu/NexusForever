using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;
using CharacterFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using SaveActor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;

namespace NexusForever.Game.Tests.Account.Currency;

public sealed partial class AccountCurrencyDurabilityTests
{
    [Theory]
    [InlineData("delivered")]
    [InlineData("abandoned")]
    [InlineData("resumed")]
    [InlineData("usage-at-cap")]
    [InlineData("usage-sql-rejected")]
    public async Task CreddDebitAndUsageShareDeliveryOrAdmissionCompensation(string outcome)
    {
        const uint accountId = 77;
        const ulong characterId = 42;
        byte usageType = (byte)EntitlementType.CREDDUsage;
        bool rejected = outcome is "usage-at-cap" or "usage-sql-rejected";
        uint beforeUsage = outcome == "usage-at-cap" ? 5u : 1u;
        using var auth = new Fixture(providerOptions, AccountCurrencyType.Credd);
        using var character = new CharacterFixture(vendorCharacterOptions);
        character.Seed();
        await SeedVendorPayment(auth, character);

        var authDatabase = new AuthDatabase();
        using (var context = auth.Context())
        {
            authDatabase.Initialise(new DatabaseConnectionString
                { Provider = context.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                    ConnectionString = context.Database.GetDbConnection().ConnectionString });
            context.AccountEntitlement.AddRange(
                new AccountEntitlementModel { Id = accountId, EntitlementId = usageType, Amount = beforeUsage },
                new AccountEntitlementModel { Id = accountId, EntitlementId = 15, Amount = 1 },
                new AccountEntitlementModel { Id = accountId, EntitlementId = 17, Amount = 1 });
            await context.SaveChangesAsync();
            if (outcome == "usage-sql-rejected")
                await context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite() ? """
                    CREATE TRIGGER reject_credd_usage BEFORE UPDATE ON account_entitlement
                    BEGIN SELECT RAISE(ABORT, 'owned CREDD usage write rejection'); END
                    """ : """
                    CREATE TRIGGER reject_credd_usage BEFORE UPDATE ON account_entitlement
                    FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned CREDD usage write rejection'
                    """);
        }
        var characterDatabase = new CharacterDatabase();
        using (var context = character.Context())
            characterDatabase.Initialise(new DatabaseConnectionString
                { Provider = context.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                    ConnectionString = context.Database.GetDbConnection().ConnectionString });

        var protocol = new VendorAccountPurchase(characterDatabase.CreateContext,
            () => auth.Context(outcome == "delivered" ? new CommitFailure(true) : null), realm: 1);
        var intent = protocol.CreateCreddRedemption(accountId, characterId, amount: 2, usageLimit: 5, epoch: 1);
        await Attempt(commit: outcome == "delivered");
        if (outcome == "resumed")
            await Attempt(commit: true);

        AccountModel admitted;
        using (var context = auth.Context())
        {
            var operations = context.AccountCurrencyOperation.ToArray();
            if (rejected)
                Assert.Empty(operations);
            else
                Assert.StartsWith("credd-redeem-debit:", Assert.Single(operations).OperationKey);
            Assert.Equal(rejected ? 100ul : 98ul, context.AccountCurrency.Single(row => row.CurrencyId == (byte)AccountCurrencyType.Credd).Amount);
            Assert.Equal(rejected ? beforeUsage : beforeUsage + 1, context.AccountEntitlement.Single(row => row.EntitlementId == usageType).Amount);
            context.AccountWorldSession.Single().Epoch = 2;
            await context.SaveChangesAsync();
            admitted = context.Account.AsNoTracking().Include(row => row.AccountEntitlement)
                .Include(row => row.AccountCurrency).Single();
            admitted.WorldSessionEpoch = 2;
        }

        await VendorAccountPurchase.RecoverAtAdmissionAsync(admitted, authDatabase, characterDatabase, 1);
        await VendorAccountPurchase.RecoverAtAdmissionAsync(admitted, authDatabase, characterDatabase, 1);
        bool delivered = outcome is "delivered" or "resumed";
        using var settled = auth.Context();
        Assert.Equal(rejected ? 0 : delivered ? 1 : 2, settled.AccountCurrencyOperation.Count());
        Assert.Equal(delivered ? 98ul : 100ul, admitted.AccountCurrency.Single(row => row.CurrencyId == (byte)AccountCurrencyType.Credd).Amount);
        Assert.Equal(delivered ? beforeUsage + 1 : beforeUsage, admitted.AccountEntitlement.Single(row => row.EntitlementId == usageType).Amount);
        Assert.Equal(new uint[] { 1, 1 }, admitted.AccountEntitlement.Where(row => row.EntitlementId is 15 or 17).Select(row => row.Amount));
        using var source = character.Context();
        Assert.Equal(delivered ? 2000ul : 0ul, source.CharacterCurrency.SingleOrDefault()?.Amount ?? 0ul);
        Assert.Equal(rejected ? 0 : 1, source.BankReconciliationReceipt.Count());

        async Task Attempt(bool commit)
        {
            await InVendorTransaction(characterDatabase.CreateContext, async (context, transaction) =>
            {
                var authorization = () => protocol.AuthorizeAsync(context, intent, unit =>
                    unit.CharacterCurrency.Add(new CharacterCurrencyModel
                    {
                        Id = characterId, CurrencyId = (byte)CurrencyType.Credits,
                        Amount = intent.Costs.Single().Amount * VendorAccountPurchase.CreddToCreditsRatio
                    }));
                if (rejected)
                {
                    if (outcome == "usage-at-cap")
                        await Assert.ThrowsAsync<InvalidOperationException>(authorization);
                    else
                        await Assert.ThrowsAsync<DbUpdateException>(authorization);
                    Assert.True(context.IsSaveRejected);
                }
                else
                {
                    Assert.Equal(VendorAccountAuthorization.Authorized, await authorization());
                    await context.SaveChangesAsync();
                }
                if (commit)
                    await transaction.CommitAsync();
                else
                    await transaction.RollbackAsync();
            });
        }
    }

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("replacement")]
    public async Task CreddRedemptionHandlerPublishesOnlyAfterSettlement(string outcome)
    {
        bool rejectCharacter = outcome == "rollback";
        bool replace = outcome == "replacement";
        using var auth = new Fixture(providerOptions, AccountCurrencyType.Credd);
        using var character = new CharacterFixture(vendorCharacterOptions);
        character.Seed();
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)auth.Tables;
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(
            new CurrencyTypeEntry { Id = (uint)CurrencyType.Credits, CapAmount = 10000 }));
        tables.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(
            new EntitlementEntry { Id = (uint)EntitlementType.CREDDUsage, MaxCount = 5 }));
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        network.SetProperty(nameof(IWorldSession.Account), auth.Account);
        network.SetProperty(nameof(IWorldSession.Events), events);
        auth.AccountProxy.SetProperty(nameof(IAccount.Session), session);
        auth.AccountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);

        AccountModel account;
        var authDatabase = new AuthDatabase();
        using (var context = auth.Context())
        {
            authDatabase.Initialise(new DatabaseConnectionString
            {
                Provider = context.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = context.Database.GetDbConnection().ConnectionString
            });
            context.AccountEntitlement.Add(new AccountEntitlementModel
                { Id = auth.Account.Id, EntitlementId = (byte)EntitlementType.CREDDUsage, Amount = 1 });
            context.AccountWorldSession.Add(new() { AccountId = auth.Account.Id, Epoch = 1 });
            await context.SaveChangesAsync();
            account = context.Account.AsNoTracking().Include(row => row.AccountCurrency)
                .Include(row => row.AccountEntitlement).Single();
        }
        var wallet = new AccountCurrencyManager(auth.Account, account, auth.Tables);
        var entitlements = new AccountEntitlementManager(auth.Account, account, null, auth.Tables);
        auth.AccountProxy.SetProperty(nameof(IAccount.CurrencyManager), wallet);
        auth.AccountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlements);

        var characterDatabase = new CharacterDatabase();
        using (var context = character.Context())
        {
            characterDatabase.Initialise(new DatabaseConnectionString
                { Provider = context.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                    ConnectionString = context.Database.GetDbConnection().ConnectionString });
            context.Character.Single().AccountId = auth.Account.Id;
            context.CharacterCurrency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = (byte)CurrencyType.Credits, Amount = 500 });
            await context.SaveChangesAsync();
            if (rejectCharacter)
                await context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite() ? """
                    CREATE TRIGGER reject_credd_credits BEFORE UPDATE ON character_currency
                    BEGIN SELECT RAISE(ABORT, 'owned CREDD Character write rejection'); END
                    """ : """
                    CREATE TRIGGER reject_credd_credits BEFORE UPDATE ON character_currency
                    FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned CREDD Character write rejection'
                    """);
        }

        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var databaseProxy);
        databaseProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), characterDatabase);
        var items = RecordingDispatchProxy<IItemManager>.Create(out _);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bank = new RealmBankManager(databases, items, async command =>
        {
            started.TrySetResult();
            await release.Task;
            await command.ExecuteAsync();
        });
        var model = Assert.Single(await characterDatabase.GetCharacters(auth.Account.Id));
        using var actor = new SaveActor(character, actorAccount: auth.Account, actorSession: session,
            actorEvents: events, authDatabase: authDatabase, characterDatabase: characterDatabase, actorModel: model,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, auth.Tables, bank));
        network.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var credits = new CurrencyManager(auth.Tables);
        credits.Initialise(actor.Player, model);
        actor.Set(nameof(Player.CurrencyManager), credits);
        auth.AccountProxy.SetMethodHandler(nameof(IAccount.Save), args =>
        {
            var context = Assert.IsType<AuthContext>(args[0]);
            entitlements.Save(context);
            wallet.Save(context);
            return null;
        });
        await actor.Load();

        int observedResults = 0;
        network.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerCREDDRedeemResult)
                return null;
            observedResults++;
            using var storedAuth = auth.Context();
            using var storedCharacter = character.Context();
            ulong storedWallet = storedAuth.AccountCurrency.Single().Amount;
            uint storedUsage = storedAuth.AccountEntitlement.Single().Amount;
            ulong storedCredits = storedCharacter.CharacterCurrency.Single().Amount;
            Assert.Equal(rejectCharacter ? 100ul : 98ul, storedWallet);
            Assert.Equal(rejectCharacter ? 1u : 2u, storedUsage);
            Assert.Equal(rejectCharacter ? 500ul : 2500ul, storedCredits);
            Assert.Equal(storedWallet, wallet.GetCurrencyAmount(AccountCurrencyType.Credd));
            Assert.Equal(storedUsage, entitlements.GetEntitlement(EntitlementType.CREDDUsage).Amount);
            Assert.Equal(storedCredits, Assert.Single(credits).Amount);
            return null;
        });

        var request = new ClientCREDDRedeem();
        typeof(ClientCREDDRedeem).GetProperty(nameof(ClientCREDDRedeem.CreddAmount))!.SetValue(request, 2ul);
        new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance).HandleMessage(session, request);
        IEnumerable<object> Packets() => network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);

        try
        {
            Assert.Empty(Packets().OfType<ServerCREDDRedeemResult>());
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (!started.Task.IsCompleted)
            {
                events.Update(.01);
                await Task.Delay(5, timeout.Token);
            }
            Assert.Equal(500ul, Assert.Single(credits).Amount);
            Assert.Empty(Packets().OfType<ServerCREDDRedeemResult>());
            using (var beforeAuth = auth.Context())
            {
                Assert.Equal(100ul, beforeAuth.AccountCurrency.Single().Amount);
                Assert.Equal(1u, beforeAuth.AccountEntitlement.Single().Amount);
            }
            using (var beforeCharacter = character.Context())
                Assert.Equal(500ul, beforeCharacter.CharacterCurrency.Single().Amount);
            if (replace)
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        }
        finally { release.TrySetResult(); }

        await actor.Drain();
        using var settledAuth = auth.Context();
        using var settledCharacter = character.Context();
        Assert.Equal(rejectCharacter ? 100ul : 98ul, settledAuth.AccountCurrency.Single().Amount);
        Assert.Equal(rejectCharacter ? 1u : 2u, settledAuth.AccountEntitlement.Single().Amount);
        Assert.Equal(rejectCharacter ? 2 : 1, settledAuth.AccountCurrencyOperation.Count());
        Assert.Equal(rejectCharacter ? 500ul : 2500ul, settledCharacter.CharacterCurrency.Single().Amount);
        Assert.Equal(replace ? 0 : 1, observedResults);
        if (replace)
        {
            Assert.Empty(Packets().OfType<ServerCREDDRedeemResult>());
            Assert.Empty(Packets().OfType<ServerAccountOperationResult>());
            return;
        }
        Assert.Equal(rejectCharacter ? 100ul : 98ul, wallet.GetCurrencyAmount(AccountCurrencyType.Credd));
        Assert.Equal(rejectCharacter ? 1u : 2u, entitlements.GetEntitlement(EntitlementType.CREDDUsage).Amount);
        Assert.Equal(rejectCharacter ? 500ul : 2500ul, Assert.Single(credits).Amount);
        Assert.Equal(rejectCharacter ? CREDDRedeemResultType.GenericFail : CREDDRedeemResultType.Ok,
            (CREDDRedeemResultType)Assert.Single(Packets().OfType<ServerCREDDRedeemResult>()).Value);
        var result = Assert.Single(Packets().OfType<ServerAccountOperationResult>());
        Assert.Equal(AccountOperation.CREDDRedeem, result.Operation);
        Assert.Equal(rejectCharacter ? AccountOperationResult.GenericFail : AccountOperationResult.Ok, result.Result);
        Assert.Empty(network.GetInvocations(nameof(IWorldSession.ForceDisconnect)));
    }
}
