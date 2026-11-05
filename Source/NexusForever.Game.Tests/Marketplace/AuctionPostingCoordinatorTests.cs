using System.Diagnostics;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class AuctionPostingCoordinatorTests
{
    [Fact]
    public Task RejectedAuctionPostReplyRetriesAfterRollback()
        => PublicPostPersistsEscrowAndWholeCharacterBeforePublication(true, "error-retry");

    [Theory]
    [InlineData(true, "none")]
    [InlineData(false, "none")]
    [InlineData(true, "lost")]
    [InlineData(true, "later")]
    [InlineData(true, "rollback")]
    [InlineData(true, "retire")]
    [InlineData(true, "replace")]
    [InlineData(true, "publish")]
    [InlineData(true, "packet-replace")]
    [InlineData(true, "ordinary-rollback")]
    [InlineData(true, "ordinary-lost")]
    [InlineData(true, "custody")]
    [InlineData(true, "ordinary-custody")]
    [InlineData(true, "competing")]
    [InlineData(true, "bound")]
    [InlineData(true, "held-metadata")]
    [InlineData(true, "expiry-commit")]
    [InlineData(true, "expiry-rollback")]
    [InlineData(true, "delete-retry")]
    [InlineData(true, "reply-retry")]
    [InlineData(true, "reply-rebind")]
    [InlineData(true, "delete-return")]
    public async Task PublicPostPersistsEscrowAndWholeCharacterBeforePublication(bool checkItemPublication, string fault)
    {
        bool rejectErrorReply = fault == "error-retry";
        if (rejectErrorReply) fault = "rollback";
        bool timed = fault.StartsWith("expiry-", StringComparison.Ordinal), metadata = fault == "held-metadata";
        bool rollback = fault is "rollback" or "expiry-rollback", custody = fault is "custody" or "ordinary-custody";
        bool suppressed = fault is "retire" or "replace" or "packet-replace" or "reply-rebind";
        bool later = fault is "later" or "rollback" or "retire" or "replace" or "packet-replace" or "ordinary-rollback" or "ordinary-lost" or "custody" or "ordinary-custody";
        using var f = new Fixture(null); f.Seed();
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        account.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(new() { Id = 900, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                BagIndex = 0, StackCount = 3, Charges = 2, Durability = .75f, ExpirationTimeLeft = timed ? 1u : 1200u,
                Soulbound = fault == "bound", MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" });
            await seed.SaveChangesAsync();
            if (rollback) seed.Database.ExecuteSqlRaw("CREATE TRIGGER reject_post BEFORE INSERT ON marketplace_auction BEGIN SELECT RAISE(ABORT, 'auction posting insertion fault'); END;");
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
        }
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
        ip.SetProperty(nameof(IItemInfo.Id), 7001u);
        ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 3 });
        ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
        itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        ulong itemId = 1000;
        itemProxy.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => itemId++);
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Events), events); account.SetProperty(nameof(IAccount.Session), session);
        var model = Assert.Single(await database.GetCharacters(12));
        var ordinary = new ObservedReconciliationDatabase(f, fault);
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorSession: session,
            actorEvents: events, characterDatabase: ordinary, inventoryFactory: (owner, loaded) => LoadedInventory(owner, loaded, items, f.Source.Tables));
        sp.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model);
        actor.Set(nameof(Player.CurrencyManager), currency);
        await actor.Load(); actor.Quest.Single().Progress = 2;
        currency.CurrencyAddAmount(CurrencyType.Credits, 20);
        var original = actor.Player.Inventory.GetItem(900ul);
        var cachedChips = original.MicrochipIds; var cachedRune = original.RuneSlots[0];
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp);
        pp.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var assetProxy); ulong mailId = 99;
        assetProxy.SetMethodReturnFactory("get_" + nameof(IAssetManager.NextMailId), () => mailId++);
        var cleanup = new CleanupManager();
        GlobalMarketplaceManager.BidOutcome executed = null;
        int executions = 0;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup,
            new GlobalMarketplaceManager.AuctionPostingExecution(async command =>
            {
                executions++;
                if (fault == "competing" || timed || metadata) { held.TrySetResult(true); await release.Task; }
                executed = await command.ExecuteAsync();
                if (fault == "custody")
                {
                    using var changed = f.Context(); var moved = changed.Item.Single(row => row.Id == 900);
                    moved.OwnerId = 42; moved.Location = (ushort)InventoryLocation.PlayerBank; await changed.SaveChangesAsync();
                }
                if (later) events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    currency.CurrencyAddAmount(CurrencyType.Credits, 5);
                    actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, info, 4);
                    if (fault == "retire") actor.Cleanup();
                    if (fault == "replace") sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                }));
                if (fault == "lost") throw new IOException("Auction posting committed; acknowledgement lost.");
                return executed;
            }));
        manager.Initialise();
        bool replied = false, publishedBeforeSql = false, successBeforeOrdinary = false, scopeDisposed = false, successBeforeDisposal = false;
        bool competed = false; int replacedAt = -1, rejected = 0, errorAttempts = 0;
        Exception itemMutation = null, inventoryMutation = null;
        Exception durabilityEdit = null, codecEdit = null, runeEdit = null, touchEdit = null;
        GenericError? replyResult = null;
        GenericError? cancelResult = null;
        int removals = 0, removalAttempts = 0, successAttempts = 0;
        bool lockedDuringRetry = false;
        sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemDelete deletion && deletion.Reason == ItemUpdateReason.Auction)
            {
                removalAttempts++;
                if (fault == "delete-retry")
                {
                    if (removalAttempts == 1) throw new IOException("Auction item removal enqueue failed.");
                    lockedDuringRetry |= cleanup.IsAccountLocked(actor.Player.Account);
                }
                if (fault == "delete-return" && removalAttempts == 1)
                {
                    events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                    {
                        ulong auctionId = Assert.Single(manager.GetOwnedItemAuctions(actor.Player)).AuctionId;
                        Assert.True(manager.TryQueueAuctionCancellation(actor.Player, auctionId, 7001, (error, _) => cancelResult = error));
                    }));
                    throw new IOException("Auction item removal enqueue failed before cancellation.");
                }
                removals++;
                using var observed = f.Context();
                publishedBeforeSql |= !observed.MarketplaceAuction.Any(row => row.ItemId == 900)
                    || observed.Item.Single(row => row.Id == 900).OwnerId != null;
                if (fault == "publish")
                {
                    itemMutation = Record.Exception(() => original.StackCount = 9);
                    inventoryMutation = Record.Exception(() => actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, info, 4));
                }
                if (fault == "packet-replace")
                {
                    sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    replacedAt = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
                }
            }
            if (args[0] is ServerAuctionPostResult response)
            {
                if (rejectErrorReply && response.Result != GenericError.Ok)
                {
                    Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
                    Assert.NotEqual(999ul, response.Auction.AuctionId);
                    if (++errorAttempts == 1) { response.Auction.AuctionId = 999; throw new IOException("Owned auction post rejection before acceptance."); }
                    Assert.Equal(2, errorAttempts);
                }
                if (fault == "competing" && response.Result == GenericError.DbFailure) { rejected++; return null; }
                if (response.Result == GenericError.Ok)
                {
                    successAttempts++;
                    if (successAttempts == 1 && fault is "reply-retry" or "reply-rebind")
                    {
                        if (fault == "reply-rebind")
                            sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                        throw new IOException("Auction post result enqueue failed.");
                    }
                    if (fault is "delete-retry" or "reply-retry")
                        lockedDuringRetry |= cleanup.IsAccountLocked(actor.Player.Account);
                }
                replied = true; replyResult = response.Result;
                if (response.Result != GenericError.Ok) return null;
                successBeforeDisposal = !scopeDisposed;
                Assert.Equal(3u, response.Auction.Quantity);
                Assert.Equal(10ul, response.Auction.MinimumBid);
                Assert.Equal(25ul, response.Auction.BuyoutPrice);
                using var observed = f.Context();
                successBeforeOrdinary = observed.CharacterCurrency.Single().Amount != (later ? 125ul : 120ul)
                    || observed.CharacterQuestObjective.Single().Progress != 2;
                replied = true;
            }
            return null;
        });
        using var provider = new ServiceCollection().AddSingleton<IGlobalMarketplaceManager>(manager)
            .AddSingleton<ILogger<ClientAuctionSellOrderSubmitHandler>>(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance)
            .AddScoped<MarketplaceRequestHelper>().AddScoped<ClientAuctionSellOrderSubmitHandler>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
        {
            var request = new ClientAuctionSellOrderSubmit();
            typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.ItemGuid))!.SetValue(request, 900ul);
            typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.MinimumBid))!.SetValue(request, 10ul);
            typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.BuyoutPrice))!.SetValue(request, 25ul);
            using (var scope = provider.CreateScope())
                scope.ServiceProvider.GetRequiredService<ClientAuctionSellOrderSubmitHandler>().HandleMessage(session, request);
            scopeDisposed = true;
            typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.ItemGuid))!.SetValue(request, 777ul);
            typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.MinimumBid))!.SetValue(request, 999ul);
            typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.BuyoutPrice))!.SetValue(request, 1000ul);
        }));
        var deadline = Stopwatch.StartNew();
        do
        {
            if (held.Task.IsCompleted && !competed)
            {
                competed = true;
                events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    if (timed || metadata)
                    {
                        if (timed) actor.Player.Inventory.Update(2);
                        else
                        {
                            durabilityEdit = Record.Exception(() => original.Durability = .5f);
                            codecEdit = Record.Exception(() => cachedChips.Add(93));
                            runeEdit = Record.Exception(() => cachedRune.RuneItem2Id = 9002);
                            touchEdit = Record.Exception(original.TouchRuneSlots);
                        }
                        release.TrySetResult(true); return;
                    }
                    Assert.Throws<InvalidOperationException>(manager.Initialise);
                    using var scope = provider.CreateScope();
                    var request = new ClientAuctionSellOrderSubmit();
                    typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.ItemGuid))!.SetValue(request, 900ul);
                    typeof(ClientAuctionSellOrderSubmit).GetProperty(nameof(ClientAuctionSellOrderSubmit.MinimumBid))!.SetValue(request, 10ul);
                    scope.ServiceProvider.GetRequiredService<ClientAuctionSellOrderSubmitHandler>().HandleMessage(session, request);
                    Assert.Equal(GenericError.ItemNotValidForSlot, manager.PostAuction(actor.Player, original, 10, 25, out _));
                }));
            }
            manager.Update(.01); events.Update(.01);
            if (rejected == 1) release.TrySetResult(true);
            Assert.Empty(actor.Errors);
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(20), "Auction posting did not settle.");
            await Task.Delay(5);
        } while (Pending(manager) || events.PendingEvents || actor.Player.IsNormalSaveInProgress);
        if (fault == "delete-return")
        {
            Assert.Equal(GenericError.Ok, cancelResult);
            Assert.False(replied);
            Assert.Equal(1, removalAttempts);
            Assert.Equal(1, executions);
            Assert.Equal(900ul, actor.Player.Inventory.GetItem(900ul)?.Guid);
            using var cancelled = f.Context();
            Assert.Empty(cancelled.MarketplaceAuction);
            Assert.Equal(42ul, cancelled.Item.Single(row => row.Id == 900).OwnerId);
            Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
            return;
        }
        if (fault == "bound")
        {
            Assert.Equal(GenericError.AuctionCannotFillOrder, replyResult); Assert.Equal(0, executions); Assert.Equal(0, removals);
            using var refused = f.Context(); Assert.Empty(refused.MarketplaceAuction);
            Assert.Equal(42ul, refused.Item.Single().OwnerId); Assert.True(refused.Item.Single().Soulbound);
            Assert.Equal(100ul, refused.CharacterCurrency.Single().Amount); return;
        }
        Assert.Equal(!suppressed, replied);
        if (!suppressed) Assert.True(replyResult == (rollback || custody ? GenericError.DbFailure : GenericError.Ok), $"Reply={replyResult};executed={executed};ordinary={ordinary.Failure}");
        Assert.Equal(1, executions); Assert.Equal(fault == "competing" ? 1 : 0, rejected);
        Assert.Equal(rollback || custody || fault is "retire" or "replace" ? 0 : 1, removals);
        if (fault is "delete-retry" or "reply-retry" or "reply-rebind")
        {
            Assert.Equal(fault == "delete-retry" ? 2 : 1, removalAttempts);
            Assert.Equal(fault == "reply-retry" ? 2 : 1, successAttempts);
            Assert.False(lockedDuringRetry);
            var publication = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
                .Where(call => call.Arguments[0] is ServerItemDelete { Reason: ItemUpdateReason.Auction } or ServerAuctionPostResult)
                .Select(call => call.Arguments[0].GetType()).ToArray();
            Assert.Equal(typeof(ServerItemDelete), publication[0]);
            Assert.Equal(typeof(ServerAuctionPostResult), publication[^1]);
            if (fault == "delete-retry") Assert.Equal(typeof(ServerItemDelete), publication[1]);
        }
        Assert.False(successBeforeDisposal);
        Assert.False(successBeforeOrdinary);
        if (checkItemPublication) Assert.False(publishedBeforeSql, "ItemDelete was published before stored auction escrow existed.");
        else Assert.False(successBeforeOrdinary, "Post success preceded whole-Character currency/quest persistence.");
        using var final = f.Context();
        if (timed)
        {
            var deferred = (System.Collections.IDictionary)typeof(Inventory).GetField("deferredRewardExpirations", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(actor.Player.Inventory);
            Assert.False(deferred?.Contains(original) == true, "Posting retained its detached or rolled-back Item expiration entry.");
            if (rollback)
            {
                Assert.DoesNotContain(final.Item, row => row.Id == 900); Assert.Empty(final.MarketplaceAuction);
                Assert.Single(sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)), call => call.Arguments[0] is ServerItemDelete packet && packet.Reason == ItemUpdateReason.Expired);
                Assert.Equal(120ul, final.CharacterCurrency.Single().Amount);
                Assert.Equal(2u, final.CharacterQuestObjective.Single().Progress); return;
            }
        }
        if (metadata)
        {
            foreach (var error in new[] { durabilityEdit, codecEdit, runeEdit, touchEdit }) Assert.IsType<InvalidOperationException>(error);
            Assert.Throws<InvalidOperationException>(() => original.Durability = .25f);
            Assert.Throws<InvalidOperationException>(() => cachedChips.Clear());
            Assert.Throws<InvalidOperationException>(() => cachedRune.RuneItem2Id = 9003);
        }
        var stored = final.Item.Single(row => row.Id == 900);
        Assert.Equal(rollback || custody ? 42ul : (ulong?)null, stored.OwnerId);
        Assert.Equal((ushort)(custody ? InventoryLocation.PlayerBank : rollback ? InventoryLocation.Inventory : InventoryLocation.None), stored.Location);
        Assert.Equal(900ul, stored.Id); if (!rollback) Assert.Equal(3u, stored.StackCount);
        Assert.Equal(2u, stored.Charges); Assert.Equal(.75f, stored.Durability); Assert.Equal(timed ? 1u : 1200u, stored.ExpirationTimeLeft);
        Assert.Equal("91,92", stored.MicrochipIds); Assert.Equal("10:9001,7:0", stored.RuneSlots);
        if (rollback) Assert.Empty(final.MarketplaceAuction);
        else
        {
            var auction = Assert.Single(final.MarketplaceAuction);
            Assert.Equal(900ul, auction.ItemId); Assert.Equal(42ul, auction.OwnerCharacterId);
            Assert.Equal(10ul, auction.MinimumBid); Assert.Equal(25ul, auction.BuyoutPrice);
            Assert.Throws<InvalidOperationException>(() => original.StackCount = 1);
        }
        if (later)
        {
            uint granted = final.Item.Where(row => row.Id != 900).ToArray().Aggregate(0u, (sum, row) => sum + row.StackCount)
                + (rollback ? stored.StackCount - 3u : 0u);
            Assert.Equal(4u, granted);
        }
        if (fault == "custody") Assert.Single(final.CharacterMailAttachment);
        if (fault == "publish") { Assert.IsType<InvalidOperationException>(itemMutation); Assert.IsType<InvalidOperationException>(inventoryMutation); }
        if (replacedAt >= 0)
            Assert.DoesNotContain(sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Skip(replacedAt),
                call => call.Arguments[0] is ServerItemAdd or ServerAuctionPostResult or ServerCombatReward);
        Assert.Equal(later ? 125ul : 120ul, final.CharacterCurrency.Single().Amount);
        Assert.Equal(2u, final.CharacterQuestObjective.Single().Progress);
        Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
    }

    private static bool Pending(GlobalMarketplaceManager manager) =>
        ((System.Collections.IDictionary)typeof(GlobalMarketplaceManager).GetField("auctionPostings", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!).Count != 0;

    private sealed class ObservedReconciliationDatabase(Fixture f, string fault) : CharacterDatabase
    {
        internal Exception Failure;
        private readonly OrdinaryFault interceptor = new(fault == "ordinary-lost");
        private int calls;
        public override async Task SaveBidReconciliation(ulong character, Action<CharacterContext> save, MarketplaceBidReconciliation receipt)
        {
            if (fault == "ordinary-custody" && Interlocked.Increment(ref calls) == 1)
            {
                using var changed = f.Context(); var moved = changed.Item.Single(row => row.Id == 900);
                moved.OwnerId = 42; moved.Location = (ushort)InventoryLocation.PlayerBank;
                await changed.SaveChangesAsync();
            }
            try { await CharacterAtomicSave.ExecuteBidReconciliation(() => fault is "ordinary-rollback" or "ordinary-lost" ? f.Context(interceptor) : f.Context(), character, save, receipt); }
            catch (Exception error) { Failure = error; throw; }
        }
    }

    private sealed class OrdinaryFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Auction posting ordinary save rolled back.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.Exchange(ref fired, 1) == 0 ? Task.FromException(new IOException("Auction posting ordinary acknowledgement lost.")) : Task.CompletedTask;
    }

    private static Inventory LoadedInventory(Player owner, CharacterModel model, IItemManager items, IGameTableManager tables)
    {
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables);
        var bag = new Bag(InventoryLocation.Inventory, 20, items);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
        Set("deletedItems", new List<IItem>());
        foreach (var row in model.Item) bag.AddItem(new NexusForever.Game.Entity.Item(row, items, tables), row.BagIndex);
        return inventory;
    }
}
