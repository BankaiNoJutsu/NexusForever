using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using System.Collections;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class AuctionCancellationCoordinatorTests
{
    [Theory]
    [InlineData(true, true, true, false, false, "none")]
    [InlineData(true, true, false, false, false, "none")]
    [InlineData(true, true, true, true, false, "none")]
    [InlineData(false, true, false, false, true, "none")]
    [InlineData(true, true, true, false, false, "main-lost")]
    [InlineData(true, true, false, true, true, "main-lost")]
    [InlineData(true, true, true, false, false, "receiver-retire")]
    [InlineData(true, true, true, false, false, "receiver-replace")]
    [InlineData(true, true, true, false, false, "bidder-retire")]
    [InlineData(true, true, false, false, true, "ordinary-rollback")]
    [InlineData(true, true, true, true, false, "ordinary-lost")]
    [InlineData(true, true, true, false, false, "held-competing")]
    [InlineData(true, true, false, true, false, "mail-replace")]
    [InlineData(true, true, true, false, false, "custody-move")]
    [InlineData(false, true, false, false, false, "refused-owner")]
    [InlineData(false, true, false, false, false, "refused-item")]
    [InlineData(false, true, false, false, false, "stored-sockets")]
    [InlineData(false, true, false, false, false, "stored-sockets-rollback")]
    public async Task PublicCancellationReturnsOriginalItemAndRefundsHeldBidThroughOriginalActors(
        bool heldBid, bool receiverOnline, bool bidderOnline, bool mailed, bool missingWallet, string fault)
    {
        bool storedSockets = fault.StartsWith("stored-sockets", StringComparison.Ordinal);
        if (fault == "stored-sockets-rollback") fault = "ordinary-rollback";
        using var f = new Fixture(null); f.Seed();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        account.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Expiry bidder", OriginalName = "Expiry bidder" });
            if (!missingWallet) seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            if (bidderOnline) seed.CharacterCurrency.Add(new() { Id = 100, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(new() { Id = 1000, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None });
            seed.MarketplaceAuction.Add(new() { Id = 1, OwnerCharacterId = fault == "refused-owner" ? 100ul : 42ul, ItemId = 1000, Item2Id = 7001,
                Quantity = 1, MinimumBid = 1, CurrentBid = heldBid ? 10ul : 0ul, TopBidderCharacterId = heldBid ? 100ul : 0ul,
                ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds() });
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
        }
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
        ip.SetProperty(nameof(IItemInfo.Id), 7001u);
        ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 1, MaxCharges = 1, ItemRuneInstanceId = storedSockets ? 1u : 0u });
        if (storedSockets)
            ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.ItemRuneInstance),
                PlayerBindPointTests.Table(new ItemRuneInstanceEntry { Id = 1, DefinedSocketCount = 1, DefinedSocketType00 = 7 }));
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
        itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Events), events); account.SetProperty(nameof(IAccount.Session), session);
        var model = receiverOnline ? Assert.Single(await database.GetCharacters(12)) : null;
        var ordinaryDatabase = fault.StartsWith("ordinary-", StringComparison.Ordinal) ? new ExpiryReconciliationDatabase(f, fault == "ordinary-lost") : null;
        using var actor = receiverOnline ? new RewardNormalSaveSnapshotTests.Actor(f, characterDatabase: ordinaryDatabase, actorModel: model, actorSession: session,
            actorEvents: events, inventoryFactory: (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables, mailed ? 0u : 20u)) : null;
        CurrencyManager currency = null;
        if (actor != null)
        {
            sp.SetProperty(nameof(IWorldSession.Player), actor.Player);
            currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model);
            actor.Set(nameof(Player.CurrencyManager), currency);
            if (mailed) actor.Set(nameof(Player.MailManager), new MailManager(actor.Player, model,
                itemManager: items, gameTableManager: f.Source.Tables, deliveryContexts: () => f.Context()));
            await actor.Load(); actor.Quest.Single().Progress = 2;
            if (!missingWallet) currency.CurrencyAddAmount(CurrencyType.Credits, 20);
        }
        var bidderEvents = new EventQueue(); var bidderSession = RecordingDispatchProxy<IWorldSession>.Create(out var bidderSp);
        bidderSp.SetProperty(nameof(IWorldSession.Events), bidderEvents);
        var bidderAccount = RecordingDispatchProxy<IAccount>.Create(out var bidderAp);
        bidderAp.SetProperty(nameof(IAccount.Id), 13u); bidderAp.SetProperty(nameof(IAccount.Session), bidderSession);
        bidderAp.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var bidderModel = bidderOnline ? Assert.Single(await database.GetCharacters(13)) : null;
        using var bidder = bidderOnline ? new RewardNormalSaveSnapshotTests.Actor(f, actorModel: bidderModel, actorAccount: bidderAccount,
            actorSession: bidderSession, actorEvents: bidderEvents, inventoryFactory: (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables)) : null;
        CurrencyManager bidderCurrency = null;
        if (bidder != null)
        {
            bidderSp.SetProperty(nameof(IWorldSession.Player), bidder.Player);
            bidderCurrency = new CurrencyManager(f.Source.Tables); bidderCurrency.Initialise(bidder.Player, bidderModel);
            bidder.Set(nameof(Player.CurrencyManager), bidderCurrency); await bidder.Load();
            bidderCurrency.CurrencyAddAmount(CurrencyType.Credits, 15);
        }
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp);
        pp.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] is ulong id ? id == 42 ? actor?.Player : id == 100 ? bidder?.Player : null : null);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ulong nextMail = 99;
        ap.SetMethodReturnFactory("get_" + nameof(IAssetManager.NextMailId), () => nextMail++);
        var cleanup = new CleanupManager(); int executions = 0, rejected = 0; bool competingQueued = false;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup, async command =>
        {
            executions++;
            if (fault == "held-competing") { held.TrySetResult(true); await release.Task; }
            var result = await command.ExecuteAsync();
            if (fault == "custody-move")
            {
                using var changed = f.Context(); changed.Item.Single().OwnerId = 100;
                await changed.SaveChangesAsync();
            }
            if (fault != "none" && actor != null) events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
            {
                currency.CurrencyAddAmount(CurrencyType.Credits, 5);
                if (fault == "receiver-retire") actor.Cleanup();
                if (fault == "receiver-replace") sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            }));
            if (fault == "bidder-retire") bidderEvents.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                { bidderCurrency.CurrencyAddAmount(CurrencyType.Credits, 3); bidder.Cleanup(); }));
            if (fault == "main-lost") throw new IOException("Expiry committed; main acknowledgement lost.");
            return result;
        }); manager.Initialise();
        int activeActor = 0, saves = 0, bidderSaves = 0, won = 0, returned = 0, replacementBoundary = -1;
        using var provider = new ServiceCollection().AddSingleton<IGlobalMarketplaceManager>(manager).AddSingleton(items)
            .AddSingleton<ILogger<ClientAuctionCancelHandler>>(NullLogger<ClientAuctionCancelHandler>.Instance)
            .AddScoped<MarketplaceRequestHelper>().AddScoped<ClientAuctionCancelHandler>().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
        {
            using var scope = provider.CreateScope();
            var request = new ClientAuctionCancel();
            typeof(ClientAuctionCancel).GetProperty(nameof(ClientAuctionCancel.AuctionId))!.SetValue(request, 1ul);
            typeof(ClientAuctionCancel).GetProperty(nameof(ClientAuctionCancel.Item2Id))!.SetValue(request, fault == "refused-item" ? 999u : 7001u);
            scope.ServiceProvider.GetRequiredService<ClientAuctionCancelHandler>().HandleMessage(session, request);
            typeof(ClientAuctionCancel).GetProperty(nameof(ClientAuctionCancel.AuctionId))!.SetValue(request, 999ul);
        }));
        ulong ownerBalance = (missingWallet ? 0ul : receiverOnline ? 120ul : 100ul) + (fault != "none" && actor != null ? 5ul : 0ul);
        ulong bidderBalance = 125ul + (fault == "bidder-retire" ? 3ul : 0ul);
        if (actor != null) actor.OnAuth = () => { Assert.Equal(42, activeActor); saves++; };
        if (bidder != null) bidder.OnAuth = () => { Assert.Equal(100, activeActor); bidderSaves++; };
        sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerAuctionCancelResult cancel) return null;
            if (fault.StartsWith("refused-", StringComparison.Ordinal))
            {
                Assert.Equal(fault == "refused-owner" ? GenericError.Params : GenericError.ItemBadId, cancel.Result);
                returned++; return null;
            }
            Assert.Equal(fault == "custody-move" ? GenericError.DbFailure : GenericError.Ok, cancel.Result);
            using var read = f.Context(); CheckSql(read);
            Assert.Equal(42, activeActor);
            returned++;
            if (fault == "mail-replace")
            {
                sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                replacementBoundary = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
            }
            return null;
        });
        var deadline = Stopwatch.StartNew();
        do
        {
            activeActor = 0; manager.Update(1.01);
            if (held.Task.IsCompleted && !competingQueued)
            {
                competingQueued = true;
                events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    Assert.False(actor.Player.Session.CanProcessIncomingPackets);
                    Assert.True(manager.TryQueueAuctionPurchase(actor.Player, 1, 7001, 11,
                        (error, _) => { Assert.Equal(GenericError.DbFailure, error); rejected++; }));
                }));
                bidderEvents.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    Assert.True(manager.TryQueueAuctionCancellation(actor.Player, 1, 7001, (error, _) => { Assert.Equal(GenericError.DbFailure, error); rejected++; }));
                }));
            }
            activeActor = 42; events.Update(.01);
            activeActor = 100; bidderEvents.Update(.01); activeActor = 0;
            if (rejected == 2) release.TrySetResult(true);
            if (actor != null) Assert.Empty(actor.Errors); if (bidder != null) Assert.Empty(bidder.Errors);
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(20), "Owned auction expiry did not settle.");
            await Task.Delay(5);
        } while (Pending(manager) || events.PendingEvents || bidderEvents.PendingEvents || actor?.Player.IsNormalSaveInProgress == true
            || bidder?.Player.IsNormalSaveInProgress == true);
        using var final = f.Context();
        if (fault.StartsWith("refused-", StringComparison.Ordinal))
        {
            Assert.Equal(1, returned); Assert.Equal(0, executions); Assert.Single(final.MarketplaceAuction);
            Assert.Equal(100ul, final.CharacterCurrency.Single(row => row.Id == 42).Amount);
            Assert.Null(final.Item.Single().OwnerId); Assert.Empty(final.MarketplaceBidReceipt); return;
        }
        CheckSql(final); Assert.Equal(1, executions);
        if (fault == "held-competing") Assert.Equal(2, rejected);
        if (ordinaryDatabase != null)
        {
            Assert.Equal(fault == "ordinary-lost" ? 1 : 2, ordinaryDatabase.Enrollments);
            Assert.Equal(1, ordinaryDatabase.Failures);
        }
        bool publishes = receiverOnline && fault is not ("receiver-retire" or "receiver-replace");
        Assert.Equal(0, won); Assert.Equal(publishes ? 1 : 0, returned);
        Assert.Equal(receiverOnline ? fault == "receiver-retire" ? 3 : 2 : 0, saves);
        Assert.Equal(bidderOnline ? fault == "bidder-retire" ? 3 : 2 : 0, bidderSaves);
        Assert.False(cleanup.IsAccountLocked(f.Source.Player.Account));
        var packets = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
        Assert.Empty(packets.OfType<ServerAuctionBidResult>());
        Assert.DoesNotContain(packets, packet => packet is ServerAuctionWon or ServerAuctionExpired or ServerAuctionOutbid);
        Assert.DoesNotContain(bidderSp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)), call => call.Arguments[0] is ServerAuctionOutbid);
        Assert.Equal(publishes && !mailed && fault != "custody-move" ? 1 : 0, packets.OfType<ServerItemAdd>().Count());
        if (storedSockets)
        {
            var wire = Assert.Single(packets.OfType<ServerItemAdd>()).InventoryItem.Item;
            Assert.Single(wire.Glyphs); Assert.Equal(0u, wire.Glyphs[0]);
            Assert.NotEqual(0ul, wire.RandomCircuitData);
            Assert.Equal("7:0", final.Item.Single().RuneSlots);
            var delivered = NexusForever.WorldServer.Network.Message.Handler.Crafting.CraftingRuneRequestHelper.GetInventoryItem(session, 1000ul);
            Assert.Single(delivered.RuneSlots);
            Assert.Equal(NexusForever.Game.Static.Crafting.RuneType.Air, delivered.RuneSlots[0].Type);
            Assert.Equal(NexusForever.Game.Static.Crafting.TradeskillResult.Success,
                NexusForever.WorldServer.Network.Message.Handler.Crafting.CraftingRuneRequestHelper.ClearRuneSlot(session, delivered, 0, false));
        }
        if (fault == "custody-move") Assert.False(actor.Player.Session.CanProcessIncomingPackets);
        if (replacementBoundary >= 0) Assert.DoesNotContain(packets.Skip(replacementBoundary), packet => packet is ServerMailAvailable or ServerItemAdd or ServerCombatReward or ServerAuctionBidResult);
        if (missingWallet && fault == "none") Assert.DoesNotContain(final.CharacterCurrency, row => row.Id == 42);

        void CheckSql(CharacterContext context)
        {
            var wallet = context.CharacterCurrency.SingleOrDefault(row => row.Id == 42);
            Assert.Equal(ownerBalance, wallet?.Amount ?? 0);
            Assert.Equal(receiverOnline ? 2u : 1u, context.CharacterQuestObjective.Single().Progress);
            var delivered = Assert.Single(context.Item);
            Assert.Equal(fault == "custody-move" ? 100ul : 42ul, delivered.OwnerId); Assert.Equal((ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory), delivered.Location);
            Assert.Equal(0u, delivered.Charges); Assert.Empty(context.MarketplaceAuction);
            if (bidderOnline) Assert.Equal(bidderBalance, context.CharacterCurrency.Single(row => row.Id == 100).Amount);
            else if (heldBid) Assert.Equal(10ul, Assert.Single(context.CharacterMail.Where(row => row.RecipientId == 100)).CurrencyAmount);
            if (mailed)
            {
                var deliveredMail = Assert.Single(context.CharacterMail.Where(row => row.RecipientId == 42));
                var link = Assert.Single(context.CharacterMailAttachment); Assert.Equal(deliveredMail.Id, link.Id);
                Assert.Equal(0u, link.Index); Assert.Equal(1000ul, link.ItemGuid);
            }
            else Assert.Empty(context.CharacterMailAttachment);
            Assert.Equal(1 + (receiverOnline ? 1 : 0) + (bidderOnline ? 1 : 0), context.MarketplaceBidReceipt.Count());
        }
    }

    private sealed class ExpiryReconciliationDatabase(Fixture fixture, bool after) : CharacterDatabase
    {
        private readonly OnceFault fault = new(after);
        internal int Enrollments;
        internal int Failures => fault.Failures;
        public override Task SaveBidReconciliation(ulong characterId, Action<CharacterContext> action, MarketplaceBidReconciliation receipt)
            => CharacterAtomicSave.ExecuteBidReconciliation(() => fixture.Context(fault), characterId,
                context => { Enrollments++; action(context); }, receipt);
    }

    private sealed class OnceFault(bool after) : DbTransactionInterceptor
    {
        internal int Failures;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.CompareExchange(ref Failures, 1, 0) == 0) throw new IOException("Expiry ordinary rollback.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.CompareExchange(ref Failures, 1, 0) == 0
                ? Task.FromException(new IOException("Expiry ordinary commit acknowledgement lost.")) : Task.CompletedTask;
    }

    private static bool Pending(GlobalMarketplaceManager manager) =>
        ((IDictionary)typeof(GlobalMarketplaceManager).GetField("auctionSettlements", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).Count != 0
        || ((EventQueue)typeof(GlobalMarketplaceManager).GetField("settlementEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).PendingEvents;

    private static Inventory EmptyInventory(Player owner, CharacterModel model, IItemManager items, IGameTableManager tables, uint capacity = 20)
    {
        Assert.Empty(model.Item);
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, capacity, items) });
        Set("deletedItems", new List<IItem>()); return inventory;
    }
}

