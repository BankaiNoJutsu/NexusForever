using System.Diagnostics;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
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
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class NormalBidCoordinatorTests
{
    [Theory]
    [InlineData("none", "unchanged")] [InlineData("self", "unchanged")]
    [InlineData("offline", "unchanged")] [InlineData("online", "unchanged")]
    [InlineData("none", "before")] [InlineData("none", "after")]
    [InlineData("none", "lost-reply")] [InlineData("none", "retired")]
    [InlineData("none", "bid-result-retry")]
    [InlineData("none", "bid-result-replace")]
    [InlineData("none", "bid-result-superseded")]
    [InlineData("none", "replacement")] [InlineData("online", "prior-retired")]
    [InlineData("none", "poor")]
    [InlineData("online-empty", "unchanged")]
    [InlineData("none", "buyout-direct")] [InlineData("self", "buyout-direct")]
    [InlineData("offline", "buyout-direct")] [InlineData("online", "buyout-direct")]
    [InlineData("online-empty", "buyout-direct")] [InlineData("none", "buyout-mail")]
    [InlineData("none", "buyout-seller")] [InlineData("online", "buyout-seller")]
    [InlineData("none", "buyout-lost-reply")] [InlineData("none", "buyout-before")]
    [InlineData("online", "buyout-result-retry")]
    [InlineData("none", "buyout-won-retry")]
    [InlineData("none", "buyout-result-replace")]
    [InlineData("none", "buyout-replace-after-result")]
    [InlineData("none", "buyout-reload-after-result")]
    [InlineData("online", "buyout-prior-queue-unpolled")]
    [InlineData("none", "buyout-after")] [InlineData("none", "buyout-retired")]
    [InlineData("none", "buyout-replacement")] [InlineData("online", "buyout-prior-retired")]
    [InlineData("none", "buyout-ordinary-rollback")] [InlineData("none", "buyout-ordinary-lost")]
    [InlineData("none", "buyout-ordinary-enrollment")] [InlineData("none", "buyout-seller-cap")]
    [InlineData("none", "buyout-publication-replacement")]
    [InlineData("none", "buyout-mail-replacement-during-won")]
    [InlineData("none", "buyout-ordinary-mail-custody")]
    [InlineData("none", "buyout-ordinary-direct-custody")]
    [InlineData("none", "buyout-ordinary-header-custody")]
    [InlineData("none", "buyout-ordinary-link-custody")]
    [InlineData("none", "buyout-ordinary-rollback-custody")]
    [InlineData("none", "buyout-ordinary-slot-postimage")]
    [InlineData("none", "buyout-ordinary-link-postimage")]
    [InlineData("none", "buyout-expiry-during-save")]
    [InlineData("none", "buyout-overoffer")] [InlineData("none", "buyout-custody")]
    public async Task PublicRequestPreflightDebitAndRefundFinishBeforeTheSingleResultAfterScopeDisposal(string prior, string competing)
    {
        bool buyout = competing.StartsWith("buyout-", StringComparison.Ordinal);
        bool mailDelivery = competing is "buyout-mail" or "buyout-mail-replacement-during-won" or "buyout-ordinary-mail-custody" or "buyout-ordinary-header-custody" or "buyout-ordinary-link-custody" or "buyout-ordinary-link-postimage", sellerOnline = competing is "buyout-seller" or "buyout-seller-cap";
        string fault = buyout ? competing[7..] is "direct" or "mail" or "seller" or "seller-cap" or "overoffer" ? "unchanged" : competing[7..] : competing;
        bool failBidResult = competing.StartsWith("bid-result-", StringComparison.Ordinal)
            || competing is "buyout-result-retry" or "buyout-result-replace";
        if (failBidResult) fault = competing == "bid-result-superseded" ? "after" : "unchanged";
        bool reloadAfterResult = competing == "buyout-reload-after-result";
        bool pausePriorPublication = competing == "buyout-prior-queue-unpolled";
        if (competing is "buyout-won-retry" or "buyout-replace-after-result" || reloadAfterResult || pausePriorPublication) fault = "unchanged";
        bool saleCommitted = buyout && fault is not ("before" or "poor");
        using var f = new Fixture(null); f.Seed();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        ((RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account).SetProperty(nameof(IAccount.Id), 12u);
        ((RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account).SetProperty(nameof(IAccount.RbacManager),
            RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Seller", OriginalName = "Seller" });
            seed.Character.Add(new() { Id = 200, AccountId = 14, Name = "Prior", OriginalName = "Prior" });
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            if (sellerOnline) seed.CharacterCurrency.Add(new() { Id = 100, CurrencyId = 1, Amount = competing == "buyout-seller-cap" ? 1000ul : 100ul });
            if (prior == "online") seed.CharacterCurrency.Add(new() { Id = 200, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(new() { Id = 1000, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None,
                ExpirationTimeLeft = fault == "expiry-during-save" ? 1u : 0u });
            seed.MarketplaceAuction.Add(new() { Id = 1, OwnerCharacterId = 100, ItemId = 1000, Item2Id = 7001, Quantity = 1,
                MinimumBid = 1, BuyoutPrice = buyout ? 10ul : 0ul, CurrentBid = prior == "none" ? 0ul : 5ul,
                TopBidderCharacterId = prior == "none" ? 0ul : prior == "self" ? 42ul : 200ul,
                ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds() });
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = seed.Database.GetDbConnection().ConnectionString });
        }
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);
        ((RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account).SetProperty(nameof(IAccount.Session), session);
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
        ip.SetProperty(nameof(IItemInfo.Id), 7001u); ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 1, MaxCharges = 1 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy); itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var model = Assert.Single(await database.GetCharacters(12));
        var reconciliationDatabase = fault.StartsWith("ordinary-", StringComparison.Ordinal) ? new BuyoutReconciliationDatabase(f, fault) : null;
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, characterDatabase: reconciliationDatabase, actorModel: model, actorSession: session, actorEvents: events,
            inventoryFactory: buyout ? (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables, mailDelivery ? 0u : 20u) : null);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(f.Source.Tables);
        currency.Initialise(actor.Player, model);
        actor.Set(nameof(Player.CurrencyManager), currency);
        if (mailDelivery) actor.Set(nameof(Player.MailManager), new MailManager(actor.Player, model,
            itemManager: items, gameTableManager: f.Source.Tables, deliveryContexts: () => f.Context()));
        await actor.Load();
        actor.Quest.Single().Progress = 2; currency.CurrencyAddAmount(CurrencyType.Credits, 20);
        var previousEvents = new EventQueue();
        var previousSession = RecordingDispatchProxy<IGameSession>.Create(out var previousSessionProxy);
        previousSessionProxy.SetProperty(nameof(IGameSession.Events), previousEvents);
        var previousAccount = RecordingDispatchProxy<IAccount>.Create(out var previousAccountProxy);
        previousAccountProxy.SetProperty(nameof(IAccount.Id), 14u); previousAccountProxy.SetProperty(nameof(IAccount.Session), previousSession);
        previousAccountProxy.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var previousModel = prior is "online" or "online-empty" ? Assert.Single(await database.GetCharacters(14)) : null;
        using var previous = previousModel != null ? new RewardNormalSaveSnapshotTests.Actor(f,
            actorModel: previousModel, actorAccount: previousAccount, actorSession: previousSession, actorEvents: previousEvents,
            inventoryFactory: (owner, model) => EmptyInventory(owner, model, items, f.Source.Tables)) : null;
        if (previous != null)
        {
            var previousCurrency = new CurrencyManager(f.Source.Tables); previousCurrency.Initialise(previous.Player, previousModel);
            previous.Set(nameof(Player.CurrencyManager), previousCurrency); await previous.Load();
        }
        var sellerEvents = new EventQueue();
        var sellerSession = RecordingDispatchProxy<IGameSession>.Create(out var sellerSessionProxy);
        sellerSessionProxy.SetProperty(nameof(IGameSession.Events), sellerEvents);
        var sellerAccount = RecordingDispatchProxy<IAccount>.Create(out var sellerAccountProxy);
        sellerAccountProxy.SetProperty(nameof(IAccount.Id), 13u); sellerAccountProxy.SetProperty(nameof(IAccount.Session), sellerSession);
        sellerAccountProxy.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var sellerModel = sellerOnline ? Assert.Single(await database.GetCharacters(13)) : null;
        using var sellerActor = sellerModel != null ? new RewardNormalSaveSnapshotTests.Actor(f,
            actorModel: sellerModel, actorAccount: sellerAccount, actorSession: sellerSession, actorEvents: sellerEvents,
            inventoryFactory: (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables)) : null;
        if (sellerActor != null)
        {
            var sellerCurrency = new CurrencyManager(f.Source.Tables); sellerCurrency.Initialise(sellerActor.Player, sellerModel);
            sellerActor.Set(nameof(Player.CurrencyManager), sellerCurrency); await sellerActor.Load();
        }
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp);
        pp.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] is ulong id ? id == 42 ? actor.Player : id == 200 ? previous?.Player : id == 100 ? sellerActor?.Player : null : null);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ulong nextMail = 99; ap.SetMethodReturnFactory("get_" + nameof(IAssetManager.NextMailId), () => nextMail++);
        var cleanup = new CleanupManager();
        int replacementPublicationBoundary = -1;
        var marketplace = fault is "unchanged" or "poor" ? new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup)
            : new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup, async command =>
            {
                if (fault is not ("before" or "after"))
                {
                    var outcome = await command.ExecuteAsync();
                    if (fault == "custody")
                    {
                        using var moved = f.Context(); moved.Item.Single().OwnerId = 200; await moved.SaveChangesAsync();
                    }
                    actor.Events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                    {
                        currency.CurrencyAddAmount(CurrencyType.Credits, 5);
                        if (fault == "publication-replacement") currency.CurrencyAddAmount(CurrencyType.Credits, 3);
                        if (fault == "retired") actor.Cleanup();
                        if (fault == "replacement")
                        {
                            sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                            replacementPublicationBoundary = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
                        }
                    }));
                    if (fault == "prior-retired")
                        previous.Events.EnqueueThreadSafe(new PredicateEvent(() => true, previous.Cleanup));
                    if (fault == "lost-reply") throw new IOException("Main bid committed; caller did not receive its outcome.");
                    return outcome;
                }
                if (fault == "after") Assert.True((await command.ExecuteAsync()).Committed);
                using (var change = f.Context())
                {
                    change.CharacterCurrency.Single(row => row.Id == 42).Amount = fault == "before" ? 777ul : 900ul;
                    if (fault == "after" && !buyout) change.MarketplaceAuction.Single().CurrentBid = 12;
                    await change.SaveChangesAsync();
                }
                // The worker submits an intent; only the original actor mutates
                // its CurrencyManager when this event is processed.
                actor.Events.EnqueueThreadSafe(new PredicateEvent(() => true, () => currency.CurrencyAddAmount(CurrencyType.Credits, 5)));
                return fault == "before" ? await command.ExecuteAsync() : await command.ResolveAsync();
            });
        marketplace.Initialise();
        int activeActor = 0, auth = 0, priorAuth = 0, sellerAuth = 0, replies = 0, bidResultAttempts = 0, wonAttempts = 0;
        actor.OnAuth = () => { Assert.Equal(42, activeActor); auth++;
            if (fault == "expiry-during-save" && auth == 2) actor.Player.Inventory.Update(2); };
        if (previous != null) previous.OnAuth = () => { Assert.Equal(200, activeActor); priorAuth++; };
        if (sellerActor != null) sellerActor.OnAuth = () => { Assert.Equal(100, activeActor); sellerAuth++; };
        GenericError result = GenericError.DbFailure;
        bool scopeDisposed = false, priorPublicationPaused = false;
        ulong firstBidResultExpiration = 0;
        sessionProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
            {
                bool replaceDuringWallet = fault == "publication-replacement" && args[0] is ServerCombatReward;
                bool replaceDuringWon = fault == "mail-replacement-during-won" && args[0] is ServerAuctionWon;
                if ((replaceDuringWallet || replaceDuringWon) && replacementPublicationBoundary < 0)
                {
                    sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    replacementPublicationBoundary = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
                }
                if (args[0] is ServerAuctionWon won)
                {
                    Assert.Equal(42, activeActor);
                    Assert.Equal(10ul, won.Auction.CurrentBid);
                    if (pausePriorPublication)
                    {
                        Assert.True(priorPublicationPaused);
                        Assert.Empty(previousSessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                            .Select(call => call.Arguments[0]).OfType<ServerAuctionOutbid>());
                    }
                    if (++wonAttempts == 1 && competing == "buyout-won-retry")
                    {
                        won.Auction.CurrentBid = 999;
                        throw new IOException("Committed buyout won notice failed before enqueue.");
                    }
                }
                if (args[0] is not ServerAuctionBidResult response) return null;
                GenericError error = response.Result; AuctionInfo view = response.Auction;
                Assert.Equal(42, activeActor);
                Assert.Equal(fault is "before" or "poor" ? 0ul : 10ul, view.CurrentBid);
                Assert.Equal(fault is "before" or "poor" ? 0ul : 42ul, view.TopBidderCharacterId);
                using var committed = f.Context();
                ulong expected = fault == "poor" ? 120ul : fault == "before" ? 782ul : fault == "after" ? 905ul
                    : prior == "self" || fault is not ("unchanged" or "unchanged") ? 115ul : 110ul;
                Assert.Equal(expected, committed.CharacterCurrency.Single(row => row.Id == 42).Amount);
                Assert.True(scopeDisposed);
                if (previous != null) Assert.Equal(prior == "online-empty" ? 5ul : 105ul, committed.CharacterCurrency.Single(row => row.Id == 200).Amount);
                Assert.Equal(2u, committed.CharacterQuestObjective.Single().Progress);
                Assert.Equal(fault == "poor" ? 0 : fault == "before" ? 1 : 2 + (previous != null ? 1 : 0) + (sellerOnline ? 1 : 0), committed.MarketplaceBidReceipt.Count());
                if (saleCommitted)
                {
                    Assert.Empty(committed.MarketplaceAuction);
                    var delivered = Assert.Single(committed.Item);
                    Assert.Equal(fault is "custody" or "ordinary-mail-custody" or "ordinary-direct-custody" or "ordinary-rollback-custody" ? 200ul : 42ul, delivered.OwnerId); Assert.Equal((ushort)(mailDelivery ? InventoryLocation.None : InventoryLocation.Inventory), delivered.Location);
                    if (sellerOnline) Assert.Equal(competing == "buyout-seller-cap" ? 1000ul : 109ul, committed.CharacterCurrency.Single(row => row.Id == 100).Amount);
                    else Assert.Equal(9ul, Assert.Single(committed.CharacterMail.Where(row => row.RecipientId == 100)).CurrencyAmount);
                    if (mailDelivery)
                    {
                        var wonMail = Assert.Single(committed.CharacterMail.Where(row => row.RecipientId == 42));
                        var attachment = Assert.Single(committed.CharacterMailAttachment);
                        Assert.Equal(wonMail.Id, attachment.Id); Assert.Equal(1000ul, attachment.ItemGuid); Assert.Equal(fault == "ordinary-link-custody" ? 1u : 0u, attachment.Index);
                    }
                }
                else Assert.Equal(fault is "before" or "poor" ? 0ul : fault == "after" ? 12ul : 10ul, committed.MarketplaceAuction.Single().CurrentBid);
                Assert.Equal(0u, Assert.Single(committed.Item).Charges); // Read-only bid preserves stored zero despite hydration to1.
                if (prior == "offline") Assert.Equal(5ul, Assert.Single(committed.CharacterMail.Where(row => row.RecipientId == 200)).CurrencyAmount);
                else if (!buyout) Assert.Empty(committed.CharacterMail);
                if (++bidResultAttempts == 1 && failBidResult)
                {
                    firstBidResultExpiration = view.ExpirationTime;
                    if (competing is "bid-result-replace" or "buyout-result-replace")
                        sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    view.CurrentBid = 999; // A failed send must not mutate the retry or listing.
                    throw new IOException("Committed auction bid result failed before enqueue.");
                }
                if (failBidResult)
                {
                    Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
                    Assert.InRange(view.ExpirationTime, 1ul, firstBidResultExpiration - 1);
                }
                replies++; result = error;
                if (competing == "buyout-replace-after-result")
                {
                    sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    replacementPublicationBoundary = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
                }
                if (reloadAfterResult)
                {
                    committed.Item.Add(new() { Id = 1001, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None });
                    committed.MarketplaceAuction.Add(new() { Id = 1, OwnerCharacterId = 100, ItemId = 1001, Item2Id = 7001, Quantity = 1,
                        MinimumBid = 12, ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds() });
                    committed.SaveChanges();
                    marketplace.Initialise();
                }
                return null;
            });
        var services = new ServiceCollection();
        services.AddSingleton<IGlobalMarketplaceManager>(marketplace); services.AddSingleton(items);
        services.AddSingleton<ILogger<ClientAuctionBuyOrderSubmitHandler>>(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance);
        services.AddScoped<MarketplaceRequestHelper>(); services.AddScoped<ClientAuctionBuyOrderSubmitHandler>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        actor.Events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
        {
            var request = new ClientAuctionBuyOrderSubmit();
            SetRequest(1, 7001, fault == "poor" ? 121ul : competing == "buyout-overoffer" ? 99ul : 10ul);
            using (var scope = provider.CreateScope())
                scope.ServiceProvider.GetRequiredService<ClientAuctionBuyOrderSubmitHandler>().HandleMessage(session, request);
            scopeDisposed = true;
            SetRequest(999, 999, 999);
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
                .Select(call => call.Arguments[0]).OfType<ServerMarketplaceStatus>());
            Assert.Equal(0, replies);
            void SetRequest(ulong id, uint item, ulong amount)
            {
                typeof(ClientAuctionBuyOrderSubmit).GetProperty(nameof(ClientAuctionBuyOrderSubmit.AuctionId))!.SetValue(request, id);
                typeof(ClientAuctionBuyOrderSubmit).GetProperty(nameof(ClientAuctionBuyOrderSubmit.Item2Id))!.SetValue(request, item);
                typeof(ClientAuctionBuyOrderSubmit).GetProperty(nameof(ClientAuctionBuyOrderSubmit.AmountOffered))!.SetValue(request, amount);
            }
        }));
        bool expectsReply = fault is not ("retired" or "replacement" or "publication-replacement")
            && competing is not ("bid-result-replace" or "bid-result-superseded" or "buyout-result-replace");
        var deadline = Stopwatch.StartNew();
        do
        {
            activeActor = 42; actor.Events.Update(.01);
            if (previous != null && (!priorPublicationPaused || wonAttempts != 0))
            {
                activeActor = 200;
                previous.Events.Update(.01);
                if (pausePriorPublication && priorAuth == 2 && previous.Player.Session.CanProcessIncomingPackets)
                    priorPublicationPaused = true;
            }
            if (sellerActor != null) { activeActor = 100; sellerEvents.Update(.01); Assert.Empty(sellerActor.Errors); }
            activeActor = 0;
            Assert.Empty(actor.Errors); if (previous != null) Assert.Empty(previous.Errors);
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(20),
                "Normal bid coordinator did not settle. " + (deadline.Elapsed >= TimeSpan.FromSeconds(20) ? Diagnose(marketplace, actor) : ""));
            await Task.Delay(5);
        } while ((expectsReply ? replies == 0 : cleanup.IsAccountLocked(actor.Player.Account)) || actor.Events.PendingEvents || actor.Player.IsNormalSaveInProgress
            || previous?.Events.PendingEvents == true || previous?.Player.IsNormalSaveInProgress == true || sellerActor?.Events.PendingEvents == true || sellerActor?.Player.IsNormalSaveInProgress == true);
        Assert.Empty(actor.Errors); Assert.Equal(expectsReply ? 1 : 0, replies);
        Assert.Equal((expectsReply ? 1 : 0) + (failBidResult ? 1 : 0), bidResultAttempts);
        if (expectsReply) Assert.Equal(fault == "poor" ? GenericError.VendorNotEnoughCash : fault is "before" or "custody" or "ordinary-mail-custody" or "ordinary-direct-custody" or "ordinary-header-custody" or "ordinary-link-custody" or "ordinary-rollback-custody" ? GenericError.DbFailure : GenericError.Ok, result);
        Assert.Equal(fault == "poor" ? 1 : fault == "retired" ? 3 : 2, auth);
        Assert.Equal(previous != null ? fault == "prior-retired" ? 3 : 2 : 0, priorAuth);
        if (previous != null)
            Assert.Equal(fault == "prior-retired" ? 0 : 1, previousSessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(call => call.Arguments[0]).OfType<ServerAuctionOutbid>().Count());
        Assert.False(cleanup.IsAccountLocked(actor.Player.Account)); Assert.False(currency.HasPendingContractCurrency);
        Assert.Equal(fault is not ("custody" or "ordinary-mail-custody" or "ordinary-direct-custody" or "ordinary-header-custody" or "ordinary-link-custody" or "ordinary-rollback-custody" or "replacement" or "publication-replacement"), actor.Player.Session.CanProcessIncomingPackets);
        if (fault is "custody" or "ordinary-mail-custody" or "ordinary-direct-custody" or "ordinary-header-custody" or "ordinary-link-custody" or "ordinary-rollback-custody") Assert.True(actor.Player.Inventory.RequiresAuthoritativeReload);
        var wire = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0])
            .Where(message => message is ServerMarketplaceStatus or ServerAuctionBidResult).ToArray();
        Assert.IsType<ServerMarketplaceStatus>(wire[0]);
        if (expectsReply) Assert.IsType<ServerAuctionBidResult>(wire[1]);
        Assert.Equal(1 + bidResultAttempts, wire.Length);
        if (!expectsReply && !failBidResult)
        {
            using var committed = f.Context();
            Assert.Equal(fault == "publication-replacement" ? 118ul : 115ul, committed.CharacterCurrency.Single(row => row.Id == 42).Amount);
            if (saleCommitted) Assert.Empty(committed.MarketplaceAuction);
            else Assert.Equal(10ul, committed.MarketplaceAuction.Single().CurrentBid);
            Assert.Equal(2u, committed.CharacterQuestObjective.Single().Progress);
            Assert.Equal(2, committed.MarketplaceBidReceipt.Count());
        }
        var seller = RecordingDispatchProxy<IPlayer>.Create(out var sellerProxy); sellerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul);
        Assert.Equal(sellerOnline ? 2 : 0, sellerAuth);
        if (reconciliationDatabase != null)
        {
            Assert.Equal(fault == "ordinary-lost" ? 1 : fault == "ordinary-rollback-custody" ? 3 : 2, reconciliationDatabase.Enrollments);
            Assert.Equal(1, reconciliationDatabase.Faults);
        }
        var published = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
        bool buyerDelivery = (expectsReply || competing == "buyout-result-replace") && saleCommitted
            && fault is not ("custody" or "ordinary-mail-custody" or "ordinary-direct-custody" or "ordinary-header-custody" or "ordinary-link-custody" or "ordinary-rollback-custody");
        if (buyout) Assert.Equal(buyerDelivery && !mailDelivery ? 1 : 0, published.OfType<ServerItemAdd>().Count());
        if (replacementPublicationBoundary >= 0) Assert.DoesNotContain(published.Skip(replacementPublicationBoundary),
            message => message is ServerItemAdd or ServerAuctionWon or ServerAuctionBidResult or ServerCombatReward or ServerMailAvailable);
        if (buyout)
        {
            int expectedWonAttempts = buyerDelivery && !reloadAfterResult && competing is not ("buyout-result-replace" or "buyout-replace-after-result")
                ? competing == "buyout-won-retry" ? 2 : 1 : 0;
            Assert.Equal(expectedWonAttempts, wonAttempts);
            Assert.Equal(expectedWonAttempts, published.OfType<ServerAuctionWon>().Count());
            if (expectedWonAttempts != 0)
                Assert.True(Array.FindLastIndex(published, packet => packet is ServerAuctionBidResult)
                    < Array.FindIndex(published, packet => packet is ServerAuctionWon), "Buyout result must precede terminal removal.");
        }
        if (reloadAfterResult) Assert.Equal(12ul, Assert.Single(marketplace.GetOwnedItemAuctions(seller)).MinimumBid);
        else if (saleCommitted) Assert.Empty(marketplace.GetOwnedItemAuctions(seller));
        else Assert.Equal(fault is "before" or "poor" ? 0ul : fault == "after" ? 12ul : 10ul, Assert.Single(marketplace.GetOwnedItemAuctions(seller)).CurrentBid);
        if (fault == "expiry-during-save")
        {
            Assert.Empty(published.OfType<ServerItemDelete>());
            actor.Player.Inventory.Update(.01);
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerItemDelete>());
            activeActor = 42; await actor.Player.SaveDirect(); activeActor = 0; using var expired = f.Context(); Assert.Empty(expired.Item);
            Assert.Equal(115ul, expired.CharacterCurrency.Single(row => row.Id == 42).Amount);
        }
    }

    private sealed class BuyoutReconciliationDatabase(Fixture fixture, string fault) : CharacterDatabase
    {
        private readonly OnceTransactionFault transactionFault = new(fault);
        internal int Enrollments, EnrollmentFaults;
        private int calls;
        internal int Faults => transactionFault.Faults + EnrollmentFaults + (fault is "ordinary-slot-postimage" or "ordinary-link-postimage" && calls >= 2 ? 1 : 0);
        public override Task SaveBidReconciliation(ulong characterId, Action<CharacterContext> action, MarketplaceBidReconciliation receipt)
        {
            calls++;
            if (fault == "ordinary-rollback-custody" && calls == 2)
            {
                using var moved = fixture.Context(); moved.Item.Single(row => row.Id == 1000).OwnerId = 200; moved.SaveChanges();
            }
            if (fault is "ordinary-slot-postimage" or "ordinary-link-postimage")
            {
                using var setup = fixture.Context();
                if (calls == 1)
                {
                    const string slotTrigger = "CREATE TRIGGER corrupt_reconciliation AFTER UPDATE ON character_currency WHEN NEW.id=42 AND NEW.amount=115 BEGIN INSERT INTO item (id,ownerId,itemId,location,bagIndex,stackCount,charges,durability,expirationTimeLeft,soulbound,microchipIds,runeSlots) VALUES(1001,42,7001,1,0,1,0,0,0,0,NULL,NULL); END;";
                    const string mailTrigger = "CREATE TRIGGER corrupt_reconciliation AFTER UPDATE ON character_currency WHEN NEW.id=42 AND NEW.amount=115 BEGIN INSERT INTO item (id,ownerId,itemId,location,bagIndex,stackCount,charges,durability,expirationTimeLeft,soulbound,microchipIds,runeSlots) VALUES(1001,42,7001,65535,0,1,0,0,0,0,NULL,NULL); INSERT INTO character_mail_attachment (id,`index`,itemGuid) SELECT id,1,1001 FROM character_mail WHERE recipientId=42; END;";
                    setup.Database.ExecuteSqlRaw(fault == "ordinary-slot-postimage" ? slotTrigger : mailTrigger);
                }
                else if (calls == 2) setup.Database.ExecuteSqlRaw("DROP TRIGGER corrupt_reconciliation");
            }
            if (fault is "ordinary-mail-custody" or "ordinary-direct-custody" or "ordinary-header-custody" or "ordinary-link-custody" && EnrollmentFaults == 0)
            {
                using var moved = fixture.Context();
                if (fault == "ordinary-header-custody") moved.CharacterMail.Single(row => row.RecipientId == 42).Subject = "Changed current header";
                else if (fault == "ordinary-link-custody") moved.Database.ExecuteSqlRaw("UPDATE character_mail_attachment SET `index`=1 WHERE itemGuid=1000");
                else moved.Item.Single(row => row.Id == 1000).OwnerId = 200;
                moved.SaveChanges(); EnrollmentFaults++;
            }
            return CharacterAtomicSave.ExecuteBidReconciliation(() => fixture.Context(transactionFault), characterId, context =>
            {
                Enrollments++; action(context);
                if (fault == "ordinary-enrollment" && Enrollments == 1)
                { EnrollmentFaults++; throw new IOException("Buyout ordinary enrollment interrupted after staging."); }
            }, receipt);
        }
    }

    private sealed class OnceTransactionFault(string fault) : DbTransactionInterceptor
    {
        internal int Faults;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (fault is "ordinary-rollback" or "ordinary-rollback-custody" && Interlocked.CompareExchange(ref Faults, 1, 0) == 0)
                throw new IOException("Buyout ordinary graph rejected before commit.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => fault == "ordinary-lost" && Interlocked.CompareExchange(ref Faults, 1, 0) == 0
                ? Task.FromException(new IOException("Buyout ordinary graph committed; acknowledgement lost.")) : Task.CompletedTask;
    }

    private static Inventory EmptyInventory(Player owner, CharacterModel model, IItemManager items, IGameTableManager tables, uint capacity = 20)
    {
        Assert.Empty(model.Item);
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string field, object value) => typeof(Inventory).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, capacity, items) });
        Set("deletedItems", new List<IItem>());
        return inventory;
    }

    private static string Diagnose(GlobalMarketplaceManager manager, RewardNormalSaveSnapshotTests.Actor actor)
    {
        static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner);
        var held = (IDictionary)Read(manager, "auctionSettlements"); var operation = held?.Contains(1ul) == true ? held[1ul] : null;
        var normal = Read(actor.Player, "normalRewardSave");
        var quest = normal?.GetType().GetProperty("Quests", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(normal);
        var context = Read(quest, "bound");
        return $"Nonatomic observations: operation={operation != null}; next={Read(operation, "next")}; outcome={Read(operation, "outcome")}; "
            + $"command={Read(operation, "command") != null}; escrow={Read(Read(operation, "command"), "item")}; "
            + $"normalSave={actor.Player.IsNormalSaveInProgress}; events={actor.Events.PendingEvents}; "
            + $"rejected={Read(context, "saveRejection")}; normalSnapshot={normal != null}; errors={string.Join(" | ", actor.Errors.Select(error => error.ToString()))}";
    }
}
