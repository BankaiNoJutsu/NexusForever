using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
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
