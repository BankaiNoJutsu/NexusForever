using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network;
using NexusForever.Network.Message;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Reward;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public sealed class LoyaltyLifecycleTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusLoyalty_{Guid.NewGuid():N}");
    private readonly DatabaseConnectionString connection;
    private readonly IGameTableManager tables;

    public LoyaltyLifecycleTests()
    {
        Directory.CreateDirectory(directory);
        connection = new() { Provider = NexusForever.Database.DatabaseProvider.Sqlite, ConnectionString = $"Data Source={Path.Combine(directory, "auth.sqlite")};Default Timeout=15" };
        tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
        proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Rewards()));
        proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.AccountItems()
            .Concat(new[] { new AccountItemEntry { Id = 125, AccountCurrencyEnum = 8, AccountCurrencyAmount = 1 } }).ToArray()));
        proxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 8 }));
        using var context = Context(); context.Database.EnsureCreated();
        context.Account.Add(new AccountModel { Id = 1, Email = "loyalty1@example.test", S = "", V = "", GameToken = "", SessionKey = "" });
        context.Account.Add(new AccountModel { Id = 2, Email = "loyalty2@example.test", S = "", V = "", GameToken = "", SessionKey = "" });
        context.SaveChanges();
    }

    [Fact]
    public void ZeroWalletInitializesCanonicalRootWithoutEarnedItemsOrEntitlementChanges()
    {
        Client client = ClientFor();
        client.Loyalty.SendInitialPackets(client.Player);
        using var context = Context();
        AccountLoyaltyModel state = Assert.Single(context.AccountLoyalty);
        Assert.Equal(0ul, state.EarnedPoints);
        Assert.Equal(9u, state.ActiveTrackId);
        Assert.Equal(44, context.AccountLoyaltySlot.Count());
        Assert.Empty(context.AccountPendingItem);
        Assert.Empty(context.AccountEntitlement);
        ServerRewardTracksReload reload = client.Reload();
        Assert.Equal(8, reload.RewardsTracks.Count);
        Assert.Equal(9u, Assert.Single(reload.RewardsTracks, row => row.IsActive).RewardTrackId);
        Assert.Equal(new uint[] { 276, 312, 278, 280, 311, 279 }, reload.RewardsTracks.Single(row => row.RewardTrackId == 9).RewardTrackRewardsIds);
        Assert.All(reload.RewardsTracks, row => Assert.All(row.ClaimedRewards.GetBuffer(), value => Assert.Equal(0, value)));
    }

    [Theory]
    [InlineData(800ul, 1)]
    [InlineData(1600ul, 2)]
    [InlineData(3200ul, 5)]
    [InlineData(4000ul, 6)]
    [InlineData(1_200_000ul, 44)]
    public void ExistingWalletBootstrapsOnceWithCanonicalEarnedAutoClaims(ulong wallet, int claims)
    {
        SeedWallet(wallet);
        Client first = ClientFor(); first.Loyalty.SendInitialPackets(first.Player);
        using (var context = Context())
        {
            AccountLoyaltyModel ledger = context.AccountLoyalty.Single();
            Assert.Equal(wallet, ledger.EarnedPoints);
            Assert.Equal(wallet, ledger.BootstrapWalletAmount);
            Assert.Equal(claims, context.AccountLoyaltySlot.Count(row => row.Claimed));
            Assert.Equal(claims, context.AccountPendingItem.Count());
            Assert.All(context.AccountPendingItem, row => Assert.True(row.ForceSoulbind));
            Assert.Empty(context.AccountEntitlement);
        }
        Client alternate = ClientFor(character: 200); alternate.Loyalty.SendInitialPackets(alternate.Player);
        using var verify = Context();
        Assert.Equal(wallet, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(claims, verify.AccountPendingItem.Count());
        Assert.Equal(first.Reload().RewardsTracks.Select(row => (row.RewardTrackId, row.RewardPointsEarned)), alternate.Reload().RewardsTracks.Select(row => (row.RewardTrackId, row.RewardPointsEarned)));
    }

    [Fact]
    public void FirstNewEarningIsNotCountedBothAsBootstrapAndOperation()
    {
        SeedWallet(800);
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
        Save(client);
        using var context = Context();
        Assert.Equal(800ul, context.AccountLoyalty.Single().BootstrapWalletAmount);
        Assert.Equal(1600ul, context.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(1600ul, context.AccountCurrency.Single().Amount);
        Assert.Equal(2, context.AccountPendingItem.Count());
        Save(client);
        Assert.Equal(2, Rows().Count);
    }

    [Fact]
    public void SpendingNeverDecreasesEarnedPointsOrPreviouslyClaimedBits()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600); Save(client);
        client.Currency.CurrencySubtractAmount(AccountCurrencyType.CosmicReward, 1600); Save(client);
        using var context = Context();
        Assert.Equal(0ul, context.AccountCurrency.Single().Amount);
        Assert.Equal(1600ul, context.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(2, context.AccountLoyaltySlot.Count(row => row.Claimed));
        Assert.Equal(2, context.AccountPendingItem.Count());
    }

    [Fact]
    public void FailedScopeAndRefundCallsCannotMintEarnedProgressEvenAcrossPeriodicSave()
    {
        Client client = ClientFor();
        using (var purchase = client.Currency.BeginMutationScope())
        {
            client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600);
            Save(client); // unrelated periodic save cannot flush a provisional purchase
            using (var before = Context()) Assert.Equal(0ul, before.AccountLoyalty.Single().EarnedPoints);
            client.Currency.CurrencySubtractAmount(AccountCurrencyType.CosmicReward, 1600);
            client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 100);
            client.Currency.CurrencySubtractAmount(AccountCurrencyType.CosmicReward, 100);
        }
        Save(client);
        using var context = Context();
        Assert.Equal(0ul, context.AccountCurrency.Single().Amount);
        Assert.Equal(0ul, context.AccountLoyalty.Single().EarnedPoints);
        Assert.Empty(context.AccountCosmicOperation);
        Assert.Empty(context.AccountPendingItem);
        Assert.Equal(0ul, client.Currency.GetCurrencyAmount(AccountCurrencyType.CosmicReward));
    }

    [Fact]
    public void SuccessfulNestedPurchaseScopeFlushesOnlyThroughItsAuthorizedContext()
    {
        Client client = ClientFor();
        using (var purchase = client.Currency.BeginMutationScope())
        {
            using (var grant = client.Currency.BeginMutationScope())
            {
                client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600);
                grant.Complete();
            }
            Save(client);
            Assert.Empty(Rows());
            using (var context = Context())
            {
                purchase.Enlist(context); client.Account.Save(context); context.SaveChanges();
            }
            purchase.Complete();
        }
        Save(client);
        using var verify = Context();
        Assert.Equal(1600ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Single(verify.AccountCosmicOperation);
        Assert.Equal(2, verify.AccountPendingItem.Count());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CanceledCapturedScopeCannotCommitLaterThroughReusedContext(bool stagedBeforeCancel, bool nested)
    {
        Client client = ClientFor();
        using var context = Context();
        using var transaction = context.Database.BeginTransaction();
        using (var purchase = client.Currency.BeginMutationScope())
        {
            if (nested)
            {
                using var grant = client.Currency.BeginMutationScope();
                client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
                grant.Complete();
            }
            else client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
            purchase.Enlist(context); client.Account.Save(context);
            if (stagedBeforeCancel) context.SaveChanges();
        }
        if (stagedBeforeCancel)
        {
            Assert.Throws<InvalidOperationException>(() => transaction.Commit());
            transaction.Rollback();
        }
        else { context.SaveChanges(); transaction.Commit(); }
        Save(client);
        using var verify = Context();
        Assert.Equal(0ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Empty(verify.AccountCosmicOperation);
        Assert.Empty(verify.AccountPendingItem);
        Assert.Equal(0ul, client.Currency.GetCurrencyAmount(AccountCurrencyType.CosmicReward));
    }

    [Fact]
    public void LegacyCurrencyOwnerWithoutLoyaltyKeepsDirtyCreateAcrossUnrelatedFailedScope()
    {
        Client client = ClientFor();
        ((RecordingDispatchProxy<IAccount>)(object)client.Account).SetProperty(nameof(IAccount.LoyaltyManager), null);
        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 10);
        using (client.Currency.BeginMutationScope()) { }
        using (var context = Context()) { client.Currency.Save(context); context.SaveChanges(); }
        using var verify = Context();
        Assert.Equal(10ul, verify.AccountCurrency.Single().Amount);
        Assert.Empty(verify.AccountLoyalty);
    }

    [Fact]
    public void CompletedChildAfterSqlFlushCannotOutliveCanceledOuterScope()
    {
        Client client = ClientFor();
        using var context = Context(); using var transaction = context.Database.BeginTransaction();
        using (var outer = client.Currency.BeginMutationScope())
        {
            using (var child = client.Currency.BeginMutationScope())
            {
                client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
                child.Enlist(context); client.Account.Save(context); context.SaveChanges();
                child.Complete();
            }
        }
        Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        transaction.Rollback(); Save(client);
        using var verify = Context();
        Assert.Equal(0ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Empty(verify.AccountCosmicOperation); Assert.Empty(verify.AccountPendingItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReconciledStaleWalletEmitsAuthoritativeCurrencyRefresh(bool initialPacketOrder)
    {
        SeedWallet(100);
        Client stale = ClientFor(); Client other = ClientFor(character: 200);
        other.Currency.CurrencySubtractAmount(AccountCurrencyType.CosmicReward, 50); Save(other);
        if (initialPacketOrder)
        {
            stale.Currency.SendInitialPackets();
            stale.Loyalty.SendInitialPackets(stale.Player);
        }
        else
        {
            stale.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 10);
            Save(stale);
        }
        ServerAccountCurrencyGrant refresh = stale.Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerAccountCurrencyGrant>().Last();
        Assert.Equal(initialPacketOrder ? 50ul : 60ul, refresh.AccountCurrency.Amount);
    }

    [Fact]
    public void ReceiptReplayDoesNotOverrideAnotherCharactersExplicitSelection()
    {
        Client stale = ClientFor(); stale.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 4000);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            stale.Account.Save(context); context.SaveChanges();
            transaction.CreateSavepoint("after_earning");
            transaction.RollbackToSavepoint("after_earning");
            transaction.Commit(); // conservative completion keeps the old earning intent
        }
        Client alternate = ClientFor(character: 200);
        Assert.True(alternate.Loyalty.ChooseTrack(alternate.Player, 9));
        Save(stale);
        using var verify = Context();
        Assert.Equal(9u, verify.AccountLoyalty.Single().ActiveTrackId);
        Assert.Single(verify.AccountCosmicOperation);
    }

    [Fact]
    public void FailedCurrencySourceConsumptionRetainsDeleteIntentAndCannotBeRedeemedAfterRelog()
    {
        Client client = ClientFor();
        IAccountInventoryItem source = client.Inventory.AddItem(125, notify: false); Save(client);
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.TakeItem(null, source.Id));
        using (var context = Context()) context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_earning BEFORE INSERT ON account_cosmic_operation BEGIN SELECT RAISE(ABORT,'owned failure'); END");
        Assert.Throws<DbUpdateException>(() => Save(client));
        using (var context = Context())
        {
            Assert.Single(context.AccountInventory);
            Assert.Empty(context.AccountCosmicOperation);
            context.Database.ExecuteSqlRaw("DROP TRIGGER reject_earning");
        }
        Save(client);
        Client relog = ClientFor(character: 200);
        Assert.Null(relog.Inventory.GetItem(source.Id));
        using var verify = Context();
        Assert.Empty(verify.AccountInventory);
        Assert.Equal(1ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Single(verify.AccountCosmicOperation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovalDuringInFlightAccountItemCreateSurvivesCompletion(bool commit)
    {
        Client client = ClientFor(); IAccountInventoryItem source = client.Inventory.AddItem(125, notify: false);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            Assert.True(client.Inventory.RemoveItem(source.Id));
            if (commit) transaction.Commit(); else transaction.Rollback();
        }
        Save(client);
        using var verify = Context(); Assert.Empty(verify.AccountInventory);
    }

    [Fact]
    public void NewAccountItemMutationDuringCreateIsSavedAfterCommit()
    {
        Client client = ClientFor(); IAccountInventoryItem source = client.Inventory.AddItem(125, notify: false);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            source.ClaimState = AccountItemClaimState.AccountMaxed;
            transaction.Commit();
        }
        Save(client);
        using var verify = Context(); Assert.Equal((byte)AccountItemClaimState.AccountMaxed, verify.AccountInventory.Single().ClaimState);
    }

    [Fact]
    public void UncertainNewAccountItemReceiptDoesNotRecreateExternallyConsumedSource()
    {
        Client client = ClientFor(); IAccountInventoryItem source = client.Inventory.AddItem(125, notify: false);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            client.Account.Save(context); context.SaveChanges();
            transaction.CreateSavepoint("after_create"); transaction.RollbackToSavepoint("after_create"); transaction.Commit();
        }
        using (var context = Context()) { context.AccountInventory.RemoveRange(context.AccountInventory); context.SaveChanges(); }
        Save(client);
        using var verify = Context(); Assert.Empty(verify.AccountInventory); Assert.Single(verify.AccountInventoryReceipt);
        Assert.Null(client.Inventory.GetItem(source.Id));
    }

    [Fact]
    public void EarnedBindingMetadataSurvivesPendingClaimAccountSaveAndAlternateCharacterReload()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800); Save(client);
        string group = Assert.Single(Rows()).GroupName;
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.ClaimPendingItemGroup(client.Player, group));
        Save(client);
        using (var context = Context()) Assert.True(context.AccountInventory.Single().ForceSoulbind);
        Client alternate = ClientFor(character: 200);
        Assert.True(Assert.Single(alternate.Inventory).ForceSoulbind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoStaleOwnersRedeemingSameCurrencySourceEarnOnlyOnce(bool legacy)
    {
        ulong id;
        if (legacy)
        {
            id = 41;
            using var context = Context(); context.AccountInventory.Add(new AccountInventoryModel { Id = 1, InventoryId = id, AccountItemId = 125 }); context.SaveChanges();
        }
        else
        {
            Client seed = ClientFor(); id = seed.Inventory.AddItem(125, notify: false).Id; Save(seed);
        }
        Client first = ClientFor(); Client second = ClientFor(character: 200);
        Assert.Equal(AccountOperationResult.Ok, first.Inventory.TakeItem(null, id));
        Assert.Equal(AccountOperationResult.Ok, second.Inventory.TakeItem(null, id));
        using var barrier = new Barrier(2);
        await Task.WhenAll(Task.Run(() => { barrier.SignalAndWait(); Save(first); }), Task.Run(() => { barrier.SignalAndWait(); Save(second); }));
        using var verify = Context();
        Assert.Equal(1ul, verify.AccountCurrency.Single().Amount);
        Assert.Equal(1ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Single(verify.AccountCosmicOperation);
        Assert.Empty(verify.AccountInventory);
    }

    [Fact]
    public void LateOldGenerationDoesNotConsumeReplacementAndNewGenerationEarnsIndependently()
    {
        Client seed = ClientFor(); ulong id = seed.Inventory.AddItem(125, notify: false).Id; Save(seed);
        Client first = ClientFor(); Client stale = ClientFor(character: 200);
        first.Inventory.TakeItem(null, id); stale.Inventory.TakeItem(null, id); Save(first);
        Client replacement = ClientFor(character: 300);
        Assert.Equal(id, replacement.Inventory.AddItem(125, notify: false).Id); Save(replacement);
        Save(stale);
        using (var context = Context()) Assert.Single(context.AccountInventory);
        Assert.Equal(AccountOperationResult.Ok, replacement.Inventory.TakeItem(null, id)); Save(replacement);
        using var verify = Context();
        Assert.Equal(2ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(2ul, verify.AccountCurrency.Single().Amount);
        Assert.Equal(2, verify.AccountCosmicOperation.Count());
        Assert.Empty(verify.AccountInventory);
    }

    [Fact]
    public void MissingPersistedSourceDoesNotCreateNewWalletOrEarnedCredit()
    {
        Client seed = ClientFor(); ulong id = seed.Inventory.AddItem(125, notify: false).Id; Save(seed);
        Client stale = ClientFor();
        using (var context = Context()) { context.AccountInventory.RemoveRange(context.AccountInventory); context.SaveChanges(); }
        stale.Inventory.TakeItem(null, id); Save(stale);
        using var verify = Context();
        Assert.Equal(0ul, verify.AccountCurrency.Single().Amount);
        Assert.Equal(0ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Empty(verify.AccountCosmicOperation);
        Assert.Equal(0ul, stale.Currency.GetCurrencyAmount(AccountCurrencyType.CosmicReward));
    }

    [Fact]
    public void NewSourceConsumedBeforeFirstSaveCommitsItsEarningWithoutCreatingRedeemableRow()
    {
        Client client = ClientFor(); ulong id = client.Inventory.AddItem(125, notify: false).Id;
        Assert.Equal(AccountOperationResult.Ok, client.Inventory.TakeItem(null, id)); Save(client);
        using var verify = Context();
        Assert.Equal(1ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Empty(verify.AccountInventory);
        Assert.Single(verify.AccountCosmicOperation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegisteredChooseAndClaimHandlersReachDurableLoyaltyOwner(bool claim)
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800); Save(client);
        var registry = new MessageManager(NullLogger<MessageManager>.Instance);
        foreach (Type type in typeof(ClientRewardTrackChooseTrack).Assembly.GetTypes()) registry.RegisterMessage(type);
        registry.RegisterMessageHandler(typeof(ClientRewardTrackChooseTrackHandler));
        registry.RegisterMessageHandler(typeof(ClientRewardTrackClaimRewardPointHandler));
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldSession.Account), client.Account); proxy.SetProperty(nameof(IWorldSession.Player), client.Player);
        ((RecordingDispatchProxy<IAccount>)(object)client.Account).SetProperty(nameof(IAccount.Session), session);
        ((RecordingDispatchProxy<IPlayer>)(object)client.Player).SetProperty(nameof(IPlayer.Session), session);
        GameMessageOpcode opcode = claim ? GameMessageOpcode.ClientRewardTrackClaimRewardPoint : GameMessageOpcode.ClientRewardTrackChooseTrack;
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes)) { writer.Write(claim ? 276u : 9u, 14); if (claim) writer.Write(0u); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
        var message = (IReadable)Activator.CreateInstance(registry.GetMessageType(opcode))!; message.Read(reader);
        Type handlerType = claim ? typeof(ClientRewardTrackClaimRewardPointHandler) : typeof(ClientRewardTrackChooseTrackHandler);
        Assert.Equal(handlerType, registry.GetMessageHandlerType(opcode));
        registry.GetMessageHandlerDelegate(opcode)(Activator.CreateInstance(handlerType, new object[] { tables }), session, message);
        var reload = proxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>().Last();
        Assert.Equal(9u, Assert.Single(reload.RewardsTracks, row => row.IsActive).RewardTrackId);
        Assert.Single(Rows());
    }

    [Fact]
    public void FailedAuthFlushRetainsWalletEarningAndClaimIntentForRetry()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600);
        using (var context = Context()) context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_reward BEFORE INSERT ON account_pending_item BEGIN SELECT RAISE(ABORT,'owned failure'); END");
        Assert.Throws<DbUpdateException>(() => Save(client));
        using (var context = Context())
        {
            Assert.Empty(context.AccountLoyalty);
            Assert.Empty(context.AccountCosmicOperation);
            Assert.Empty(context.AccountPendingItem);
            context.Database.ExecuteSqlRaw("DROP TRIGGER reject_reward");
        }
        Save(client); Save(client);
        using var verify = Context();
        Assert.Equal(1600ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(2, verify.AccountPendingItem.Count());
        Assert.Single(verify.AccountCosmicOperation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OuterTransactionControlsEarningAndClaims(bool commit)
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            client.Account.Save(context); await context.SaveChangesAsync();
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        Assert.Equal(commit ? 1 : 0, Rows().Count);
        Save(client);
        Assert.Single(Rows());
        using var verify = Context(); Assert.Equal(800ul, verify.AccountLoyalty.Single().EarnedPoints);
    }

    [Fact]
    public async Task IndependentStaleAccountInstancesApplyDeltasWithoutLosingEarnings()
    {
        Client first = ClientFor(); Client second = ClientFor(character: 200);
        first.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
        second.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
        using var barrier = new Barrier(2);
        await Task.WhenAll(Task.Run(() => { barrier.SignalAndWait(); Save(first); }), Task.Run(() => { barrier.SignalAndWait(); Save(second); }));
        using var context = Context();
        Assert.Equal(1600ul, context.AccountCurrency.Single().Amount);
        Assert.Equal(1600ul, context.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(2, context.AccountCosmicOperation.Count());
        Assert.Equal(2, context.AccountPendingItem.Count());
    }

    [Fact]
    public async Task LaterEarningDuringInFlightSaveRemainsPendingAfterEarlierCompletion()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
        await using (var context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            client.Account.Save(context); await context.SaveChangesAsync();
            client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800);
            await transaction.CommitAsync();
        }
        Assert.Equal(1600ul, client.Currency.GetCurrencyAmount(AccountCurrencyType.CosmicReward));
        Save(client);
        using var verify = Context(); Assert.Equal(1600ul, verify.AccountLoyalty.Single().EarnedPoints); Assert.Equal(2, verify.AccountPendingItem.Count());
    }

    [Fact]
    public void EarnedAutoClaimAndDuplicateExplicitClaimShareOneReceiptAndConsumedRewardIsNotRecreated()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 800); Save(client);
        Assert.True(client.Loyalty.ClaimReward(client.Player, 276, 0));
        Assert.True(client.Loyalty.ClaimReward(client.Player, 276, 0));
        using (var context = Context()) { context.AccountPendingItem.RemoveRange(context.AccountPendingItem); context.SaveChanges(); }
        Assert.True(client.Loyalty.ClaimReward(client.Player, 276, 0));
        Assert.Empty(Rows());
        using var verify = Context(); Assert.Single(verify.AccountLoyaltySlot.Where(row => row.Claimed));
        Assert.Single(verify.AccountPendingItemDelivery.Where(row => row.DeliveryKey.StartsWith("loyalty:")));
    }

    [Fact]
    public void InvalidForeignUnearnedChoiceAndLockedSelectionAreRejected()
    {
        Client client = ClientFor(); client.Loyalty.SendInitialPackets(client.Player);
        Assert.False(client.Loyalty.ClaimReward(client.Player, 276, 0));
        Assert.False(client.Loyalty.ClaimReward(client.Player, 1, 0));
        Assert.False(client.Loyalty.ChooseTrack(client.Player, 10));
        Assert.False(client.Loyalty.ChooseTrack(client.Player, 4));
        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 4000); Save(client);
        Assert.False(client.Loyalty.ClaimReward(client.Player, 276, 1));
        Assert.False(client.Loyalty.ClaimReward(client.Player, 276, uint.MaxValue));
        Assert.True(client.Loyalty.ChooseTrack(client.Player, 10));
        Assert.True(client.Loyalty.ChooseTrack(client.Player, 9));
        Assert.Equal(9u, Assert.Single(client.Reload().RewardsTracks, row => row.IsActive).RewardTrackId);
    }

    [Fact]
    public void AccountIsolationAndOldInventoryCompletionPreserveAdoptedDurableRewards()
    {
        Client client = ClientFor();
        client.Inventory.AddPendingItemGroup([103], group: "local", notify: false);
        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600);
        Save(client); Save(client);
        Assert.Equal(3, Rows().Count);
        Assert.Equal(2, Rows().Count(row => row.GroupName.StartsWith("loyalty:")));
        Client other = ClientFor(2); other.Loyalty.SendInitialPackets(other.Player);
        using var context = Context();
        Assert.Equal(0ul, context.AccountLoyalty.Single(row => row.AccountId == 2).EarnedPoints);
        Assert.Empty(context.AccountPendingItem.Where(row => row.Id == 2));
    }

    [Fact]
    public void HugeEarningCapsAllTiersWithoutOverflowAndNextOverflowDoesNotMintRewards()
    {
        Client client = ClientFor(); client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, ulong.MaxValue); Save(client);
        Assert.Equal(44, Rows().Count);
        Assert.Throws<OverflowException>(() => client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1));
        Save(client);
        using var context = Context(); Assert.Equal(ulong.MaxValue, context.AccountLoyalty.Single().EarnedPoints); Assert.Equal(44, context.AccountPendingItem.Count());
    }

    private Client ClientFor(uint id = 1, ulong character = 100)
    {
        using var context = Context();
        AccountModel model = context.Account.AsNoTracking().Include(row => row.AccountCurrency).Include(row => row.AccountPendingItem)
            .Include(row => row.AccountInventory).Include(row => row.Loyalty).Include(row => row.LoyaltySlots).Single(row => row.Id == id);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), id);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy); accountProxy.SetProperty(nameof(IAccount.Session), session);
        var currency = new AccountCurrencyManager(account, model, tables); accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currency);
        var inventory = new AccountInventoryManager(account, model, null, gameTableManager: tables); accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);
        var database = new AuthDatabase(); database.Initialise(connection);
        var loyalty = new AccountLoyaltyManager(account, tables, database); accountProxy.SetProperty(nameof(IAccount.LoyaltyManager), loyalty);
        accountProxy.SetMethodHandler(nameof(IAccount.Save), args => { var unit = (AuthContext)args[0]; currency.Save(unit); inventory.Save(unit); loyalty.Save(unit); return null; });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Account), account); playerProxy.SetProperty(nameof(IPlayer.CharacterId), character);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = character });
        return new Client(account, player, currency, inventory, loyalty, sessionProxy);
    }

    private AuthContext Context() => new(connection);
    private void Save(Client client) { using var context = Context(); client.Account.Save(context); context.SaveChanges(); }
    private List<AccountPendingItemModel> Rows() { using var context = Context(); return context.AccountPendingItem.AsNoTracking().ToList(); }
    private void SeedWallet(ulong amount) { using var context = Context(); context.AccountCurrency.Add(new AccountCurrencyModel { Id = 1, CurrencyId = 8, Amount = amount }); context.SaveChanges(); }
    private sealed record Client(IAccount Account, IPlayer Player, AccountCurrencyManager Currency, AccountInventoryManager Inventory, AccountLoyaltyManager Loyalty, RecordingDispatchProxy<IGameSession> Session)
    {
        public ServerRewardTracksReload Reload() => Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>().Last();
    }
    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(connection.ConnectionString);
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
