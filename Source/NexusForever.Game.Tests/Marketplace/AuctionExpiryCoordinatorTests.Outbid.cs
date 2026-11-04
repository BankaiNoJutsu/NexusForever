using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Character;
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
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed partial class AuctionExpiryCoordinatorTests
{
    [Theory]
    [InlineData("retry")]
    [InlineData("rebid")]
    [InlineData("rebid-rejected")]
    [InlineData("replace")]
    [InlineData("buyout")]
    [InlineData("buyout-wallet")]
    public async Task PublicBidRetriesOutbidNoticeOnlyWhileTheOriginalOutcomeApplies(string mode)
    {
        using var f = new Fixture(null);
        f.Seed();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Prior bidder", OriginalName = "Prior bidder" });
            seed.Character.Add(new() { Id = 200, AccountId = 14, Name = "Seller", OriginalName = "Seller" });
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            seed.CharacterCurrency.Add(new() { Id = 100, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(new() { Id = 1000, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None });
            seed.MarketplaceAuction.Add(new() { Id = 1, OwnerCharacterId = 200, ItemId = 1000, Item2Id = 7001,
                Quantity = 1, MinimumBid = 1, CurrentBid = 10, TopBidderCharacterId = 100, BuyoutPrice = 80,
                ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds() });
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
        }
        var itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), 7001u);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 1 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
        itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), itemInfo);
        var buyerSession = RecordingDispatchProxy<IWorldSession>.Create(out var buyerProxy);
        var priorSession = RecordingDispatchProxy<IWorldSession>.Create(out var priorProxy);
        using var buyer = await CreateActor(12, buyerSession, buyerProxy);
        using var prior = await CreateActor(13, priorSession, priorProxy);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp);
        dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp);
        pp.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] switch
        {
            42ul => buyer.Player,
            100ul => prior.Player,
            _ => null
        });
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap);
        ap.SetProperty(nameof(IAssetManager.NextMailId), 99ul);
        var cleanup = new CleanupManager();
        var rebidStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRebid = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool buyout = mode is "buyout" or "buyout-wallet";
        int executions = 0;
        var manager = new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup, async command =>
        {
            if (Interlocked.Increment(ref executions) == 2)
            {
                rebidStarted.SetResult();
                await releaseRebid.Task;
            }
            var outcome = await command.ExecuteAsync();
            if (mode == "buyout-wallet")
            {
                Assert.True(outcome.Committed);
                using var later = f.Context();
                later.CharacterCurrency.Single(row => row.Id == 42).Amount = 25;
                await later.SaveChangesAsync();
            }
            return outcome;
        });
        manager.Initialise();
        var handler = new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance,
            items, new MarketplaceRequestHelper(manager));
        ulong firstAmount = buyout ? 80ul : 20ul;
        ulong buyerAfterFirst = mode == "buyout-wallet" ? 25ul : 100ul - firstAmount;
        bool rebids = mode is "rebid" or "rebid-rejected";
        int attempts = 0, notices = 0, firstReplies = 0, rebidReplies = 0;
        Stopwatch heldRebid = null;

        priorProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAuctionBidResult reply)
            {
                Assert.Equal(mode == "rebid-rejected" ? GenericError.DbFailure : GenericError.Ok, reply.Result);
                rebidReplies++;
            }
            if (args[0] is not ServerAuctionOutbid notice) return null;
            Assert.Equal(1ul, notice.Auction.AuctionId);
            Assert.Equal(firstAmount, notice.Auction.CurrentBid);
            using var read = f.Context();
            Assert.Equal(110ul, read.CharacterCurrency.Single(row => row.Id == 100).Amount);
            Assert.Equal(buyerAfterFirst, read.CharacterCurrency.Single(row => row.Id == 42).Amount);
            if (++attempts == 1)
            {
                if (mode == "replace")
                    priorProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                throw new IOException("Outbid notice failed before enqueue.");
            }
            Assert.Equal(1, firstReplies);
            Assert.False(cleanup.IsAccountLocked(prior.Player.Account));
            notices++;
            return null;
        });
        buyerProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAuctionBidResult reply)
            {
                Assert.Equal(GenericError.Ok, reply.Result);
                firstReplies++;
                if (mode == "rebid-rejected")
                {
                    using var setup = f.Context();
                    setup.Database.ExecuteSqlRaw("CREATE TRIGGER reject_rebid BEFORE UPDATE ON marketplace_auction BEGIN SELECT RAISE(ABORT,'rebid fault'); END;");
                }
                if (rebids)
                    prior.Events.EnqueueThreadSafe(new PredicateEvent(() => true, () => Submit(priorSession, 30)));
            }
            return null;
        });
        buyer.Events.EnqueueThreadSafe(new PredicateEvent(() => true, () => Submit(buyerSession, firstAmount)));
        var deadline = Stopwatch.StartNew();
        try
        {
            do
            {
                manager.Update(.01);
                buyer.Events.Update(.01);
                prior.Events.Update(.01);
                Assert.Empty(buyer.Errors);
                Assert.Empty(prior.Errors);
                if (rebidStarted.Task.IsCompleted)
                {
                    heldRebid ??= Stopwatch.StartNew();
                    if (heldRebid.Elapsed >= TimeSpan.FromMilliseconds(1100)) releaseRebid.TrySetResult();
                }
                Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(20), "Outbid publication did not settle.");
                await Task.Delay(5);
            } while (Pending(manager) || buyer.Events.PendingEvents || prior.Events.PendingEvents);
        }
        finally { releaseRebid.TrySetResult(); }

        bool retries = buyout || mode is "retry" or "rebid-rejected";
        Assert.Equal(retries ? 2 : 1, attempts);
        Assert.Equal(retries ? 1 : 0, notices);
        Assert.Equal(1, firstReplies);
        Assert.Equal(rebids ? 1 : 0, rebidReplies);
        Assert.Equal(rebids ? 2 : 1, executions);
        Assert.False(cleanup.IsAccountLocked(buyer.Player.Account));
        Assert.False(cleanup.IsAccountLocked(prior.Player.Account));
        using var final = f.Context();
        Assert.Equal(mode == "rebid" ? 100ul : buyerAfterFirst, final.CharacterCurrency.Single(row => row.Id == 42).Amount);
        Assert.Equal(mode == "rebid" ? 80ul : 110ul, final.CharacterCurrency.Single(row => row.Id == 100).Amount);
        if (buyout)
        {
            Assert.Empty(final.MarketplaceAuction);
            Assert.Equal(42ul, Assert.Single(final.Item).OwnerId);
            Assert.Equal(76ul, Assert.Single(final.CharacterMail).CurrencyAmount);
        }
        else
        {
            var listing = Assert.Single(final.MarketplaceAuction);
            Assert.Equal(mode == "rebid" ? 100ul : 42ul, listing.TopBidderCharacterId);
            Assert.Equal(mode == "rebid" ? 30ul : 20ul, listing.CurrentBid);
            Assert.Null(Assert.Single(final.Item).OwnerId);
        }

        async Task<RewardNormalSaveSnapshotTests.Actor> CreateActor(uint accountId, IWorldSession session,
            RecordingDispatchProxy<IWorldSession> proxy)
        {
            var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            accountProxy.SetProperty(nameof(IAccount.Id), accountId);
            accountProxy.SetProperty(nameof(IAccount.Session), session);
            accountProxy.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
            var events = new EventQueue();
            proxy.SetProperty(nameof(IWorldSession.Events), events);
            var model = Assert.Single(await database.GetCharacters(accountId));
            var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorAccount: account, actorSession: session,
                actorEvents: events, inventoryFactory: (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables));
            proxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
            var currency = new CurrencyManager(f.Source.Tables);
            currency.Initialise(actor.Player, model);
            actor.Set(nameof(Player.CurrencyManager), currency);
            await actor.Load();
            return actor;
        }

        void Submit(IWorldSession session, ulong amount)
        {
            var request = new ClientAuctionBuyOrderSubmit();
            typeof(ClientAuctionBuyOrderSubmit).GetProperty(nameof(ClientAuctionBuyOrderSubmit.AuctionId))!.SetValue(request, 1ul);
            typeof(ClientAuctionBuyOrderSubmit).GetProperty(nameof(ClientAuctionBuyOrderSubmit.Item2Id))!.SetValue(request, 7001u);
            typeof(ClientAuctionBuyOrderSubmit).GetProperty(nameof(ClientAuctionBuyOrderSubmit.AmountOffered))!.SetValue(request, amount);
            handler.HandleMessage(session, request);
        }
    }
}
