using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
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
        using var auth = new Fixture(null, AccountCurrencyType.Credd);
        using var character = new CharacterFixture(null);
        character.Seed();
        await SeedVendorPayment(auth, character);

        var authDatabase = new AuthDatabase();
        using (var context = auth.Context())
        {
            authDatabase.Initialise(new DatabaseConnectionString
                { Provider = DatabaseProvider.Sqlite, ConnectionString = context.Database.GetDbConnection().ConnectionString });
            context.AccountEntitlement.AddRange(
                new AccountEntitlementModel { Id = accountId, EntitlementId = usageType, Amount = beforeUsage },
                new AccountEntitlementModel { Id = accountId, EntitlementId = 15, Amount = 1 },
                new AccountEntitlementModel { Id = accountId, EntitlementId = 17, Amount = 1 });
            await context.SaveChangesAsync();
            if (outcome == "usage-sql-rejected")
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER reject_credd_usage BEFORE UPDATE ON account_entitlement
                    BEGIN SELECT RAISE(ABORT, 'owned CREDD usage write rejection'); END
                    """);
        }
        var characterDatabase = new CharacterDatabase();
        using (var context = character.Context())
            characterDatabase.Initialise(new DatabaseConnectionString
                { Provider = DatabaseProvider.Sqlite, ConnectionString = context.Database.GetDbConnection().ConnectionString });

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
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreddRedemptionWorldSaveStoresWalletAndUsageOnTheAuthSide(bool rejectCharacter)
    {
        using var auth = new Fixture(null, AccountCurrencyType.Credd);
        using var character = new CharacterFixture(null);
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

        AccountModel account;
        var authDatabase = new AuthDatabase();
        using (var context = auth.Context())
        {
            authDatabase.Initialise(new DatabaseConnectionString
            {
                Provider = DatabaseProvider.Sqlite, ConnectionString = context.Database.GetDbConnection().ConnectionString
            });
            context.AccountEntitlement.Add(new AccountEntitlementModel
                { Id = auth.Account.Id, EntitlementId = (byte)EntitlementType.CREDDUsage, Amount = 1 });
            await context.SaveChangesAsync();
            account = context.Account.AsNoTracking().Include(row => row.AccountCurrency)
                .Include(row => row.AccountEntitlement).Single();
        }
        var wallet = new AccountCurrencyManager(auth.Account, account, auth.Tables);
        var entitlements = new AccountEntitlementManager(auth.Account, account, null, auth.Tables);
        auth.AccountProxy.SetProperty(nameof(IAccount.CurrencyManager), wallet);
        auth.AccountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlements);

        var model = new CharacterModel { Id = 42, AccountId = auth.Account.Id };
        model.Currency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = (byte)CurrencyType.Credits, Amount = 500 });
        using (var context = character.Context())
        {
            context.Character.Single().AccountId = auth.Account.Id;
            context.CharacterCurrency.AddRange(model.Currency);
            await context.SaveChangesAsync();
            if (rejectCharacter)
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER reject_credd_credits BEFORE UPDATE ON character_currency
                    BEGIN SELECT RAISE(ABORT, 'owned CREDD Character write rejection'); END
                    """);
        }

        using var actor = new SaveActor(character, actorAccount: auth.Account, actorSession: session,
            actorEvents: events, authDatabase: authDatabase);
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

        var request = new ClientCREDDRedeem();
        typeof(ClientCREDDRedeem).GetProperty(nameof(ClientCREDDRedeem.CreddAmount))!.SetValue(request, 2ul);
        new ClientCREDDRedeemHandler(NullLogger<ClientCREDDRedeemHandler>.Instance).HandleMessage(session, request);
        IEnumerable<object> Packets() => network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);

        Assert.Equal(2500ul, Assert.Single(credits).Amount);
        using (var beforeAuth = auth.Context())
        {
            Assert.Equal(100ul, beforeAuth.AccountCurrency.Single().Amount);
            Assert.Equal(1u, beforeAuth.AccountEntitlement.Single().Amount);
        }
        using (var beforeCharacter = character.Context())
            Assert.Equal(500ul, beforeCharacter.CharacterCurrency.Single().Amount);
        // Capture the legacy ordering separately from the save's persisted outcome.
        bool sentBeforeSave = Packets().OfType<ServerCREDDRedeemResult>().Any();

        actor.StartSave();
        await actor.Drain();
        using var settledAuth = auth.Context();
        using var settledCharacter = character.Context();
        Assert.Equal(98ul, settledAuth.AccountCurrency.Single().Amount);
        Assert.Equal(2u, settledAuth.AccountEntitlement.Single().Amount);
        Assert.Equal(rejectCharacter ? 500ul : 2500ul, settledCharacter.CharacterCurrency.Single().Amount);
        Assert.Equal(rejectCharacter ? 1 : 0, network.GetInvocations(nameof(IWorldSession.ForceDisconnect)).Count);
        Assert.Equal(CREDDRedeemResultType.Ok, (CREDDRedeemResultType)Assert.Single(Packets().OfType<ServerCREDDRedeemResult>()).Value);
        Assert.True(sentBeforeSave, "The legacy success packet already precedes both database saves.");
    }
}
