using System.Diagnostics;
using System.Collections;
using System.Collections.Immutable;
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
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class CommodityPostingCoordinatorTests
{
    [Fact]
    public Task RejectedCommodityPostReplyRetriesWithoutAnotherDebit()
        => PublicSubmitSettlesWalletInventoryAndMatchingBeforeSuccess(true, true, false, "error-retry");

    [Theory]
    [InlineData(true, false, false, "none")]
    [InlineData(false, false, false, "none")]
    [InlineData(true, false, true, "none")]
    [InlineData(false, false, true, "none")]
    [InlineData(true, true, false, "none")]
    [InlineData(false, true, false, "none")]
    [InlineData(true, true, true, "none")]
    [InlineData(false, true, true, "none")]
    [InlineData(false, true, false, "reuse-full")]
    [InlineData(false, true, true, "reuse-full")]
    [InlineData(false, false, true, "consume-full")]
    [InlineData(true, false, true, "lost")]
    [InlineData(false, true, true, "lost")]
    [InlineData(true, false, true, "later")]
    [InlineData(false, true, true, "later")]
    [InlineData(true, false, true, "replace")]
    [InlineData(false, true, true, "retire")]
    [InlineData(false, true, true, "reuse-rollback")]
    [InlineData(true, false, true, "online")]
    [InlineData(true, false, true, "counterparty-paused")]
    [InlineData(true, false, true, "counterparty-relisted")]
    [InlineData(true, false, true, "counterparty-reload")]
    [InlineData(false, false, true, "online")]
    [InlineData(true, false, true, "self")]
    [InlineData(false, false, true, "self")]
    [InlineData(true, false, true, "multi")]
    [InlineData(true, false, true, "fill-refresh")]
    [InlineData(true, true, true, "fill-refresh")]
    [InlineData(true, false, true, "fill-replace")]
    [InlineData(true, false, true, "reply-retry")]
    [InlineData(true, false, true, "reply-replace")]
    [InlineData(true, false, false, "reply-relisted")]
    [InlineData(true, false, true, "reply-held")]
    [InlineData(true, false, true, "satchel")]
    [InlineData(true, false, true, "satchel-later")]
    [InlineData(true, false, true, "mail")]
    [InlineData(false, true, true, "mail")]
    [InlineData(false, true, true, "publish")]
    [InlineData(false, true, true, "reuse-packet-replace")]
    [InlineData(true, false, true, "competing")]
    [InlineData(false, false, false, "missing-items")]
    [InlineData(true, true, false, "missing-cash")]
    [InlineData(true, false, true, "earning")]
    [InlineData(true, false, true, "earning-lost")]
    [InlineData(true, false, true, "earning-rollback")]
    [InlineData(true, false, true, "zero-expiration")]
    [InlineData(true, false, true, "routing-split")]
    [InlineData(true, false, true, "routing-merge")]
    [InlineData(true, false, true, "legacy-admission")]
    [InlineData(true, false, true, "legacy-reentrant")]
    [InlineData(true, false, true, "legacy-expiry")]
    [InlineData(true, false, true, "expiry-lookup")]
    public async Task PublicSubmitSettlesWalletInventoryAndMatchingBeforeSuccess(bool buy, bool immediate, bool match, string fault)
    {
        bool rejectErrorReply = fault == "error-retry";
        if (rejectErrorReply) fault = "missing-cash";
        int errorAttempts = 0;
        bool full = fault.StartsWith("reuse", StringComparison.Ordinal) || fault == "consume-full";
        bool later = fault is "later" or "replace" or "retire" or "satchel-later";
        bool failFill = fault is "fill-refresh" or "fill-replace";
        bool failReply = fault is "reply-retry" or "reply-replace" or "reply-relisted" or "reply-held";
        bool pauseOther = fault is "counterparty-paused" or "counterparty-relisted" or "counterparty-reload";
        bool otherOnline = pauseOther || fault is "online" or "fill-replace";
        bool routing = fault.StartsWith("routing", StringComparison.Ordinal), earning = fault.StartsWith("earning", StringComparison.Ordinal);
        bool expiryProbe = fault is "legacy-expiry" or "expiry-lookup", lookupProbe = fault == "expiry-lookup";
        bool self = fault is "self" or "competing", multi = fault == "multi" || routing, satchel = fault.StartsWith("satchel", StringComparison.Ordinal), mail = fault == "mail";
        uint quantity = fault == "routing-split" ? 22u : fault == "routing-merge" ? 20u : 3u;
        uint oppositeQuantity = routing ? quantity / 2 : multi ? 1u : 2u;
        bool refused = fault.StartsWith("missing", StringComparison.Ordinal), suppressed = fault is "replace" or "retire" or "reuse-packet-replace" or "fill-replace" or "reply-replace";
        uint stock = fault == "missing-items" ? 2u : full ? 3u : 10u;
        using var f = new Fixture(null); f.Seed();
        if (earning) ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetProperty(nameof(IQuestInfo.Objectives),
            ImmutableList.Create<IQuestObjectiveInfo>(new NexusForever.Game.Quest.QuestObjectiveInfo(new QuestObjectiveEntry
                { Id = 90010, Type = (uint)QuestObjectiveType.EarnCurrency, Data = (uint)CurrencyType.Credits, Count = 1000 })));
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.GameFormula),
            PlayerBindPointTests.Table(new GameFormulaEntry { Id = 1 }));
        if (satchel) ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.TradeskillMaterial),
            PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 1, Item2IdStatRevolution = 7001 }));
        var ap = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        ap.SetProperty(nameof(IAccount.Id), 12u); ap.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Counterparty", OriginalName = "Counterparty" });
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = fault == "missing-cash" ? 0ul : routing ? 500ul : 100ul });
            if (otherOnline) seed.CharacterCurrency.Add(new() { Id = 100, CurrencyId = 1, Amount = 200 });
            if (satchel) seed.CharacterTradeskillMaterial.Add(new() { Id = 42, MaterialId = 1, Amount = 98 });
            if (!buy) seed.Item.Add(new() { Id = 900, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory, BagIndex = 0, StackCount = stock, Durability = 1 });
            if (match) seed.MarketplaceCommodityOrder.Add(new() { Id = 1, OwnerCharacterId = self ? 42ul : 100ul, Item2Id = 7001, Quantity = oppositeQuantity,
                PricePerUnit = buy ? 10ul : 12ul, Price = (buy ? 10ul : 12ul) * oppositeQuantity, IsBuyOrder = !buy,
                ListTime = (ulong)DateTime.UtcNow.AddMinutes(-1).ToFileTimeUtc(), ExpirationTime = fault == "zero-expiration" ? 0ul : (ulong)DateTime.UtcNow.AddHours(1).ToFileTimeUtc() });
            if (multi)
            {
                seed.Character.Add(new() { Id = 200, AccountId = 14, Name = "Best price", OriginalName = "Best price" });
                seed.MarketplaceCommodityOrder.Add(new() { Id = 2, OwnerCharacterId = 200, Item2Id = 7001, Quantity = oppositeQuantity, PricePerUnit = 9, Price = 9ul * oppositeQuantity,
                    ListTime = (ulong)DateTime.UtcNow.AddMinutes(-1).ToFileTimeUtc(), ExpirationTime = (ulong)DateTime.UtcNow.AddHours(1).ToFileTimeUtc() });
            }
            if (expiryProbe)
            {
                seed.Character.Add(new() { Id = 200, AccountId = 14, Name = "Expiry adapter", OriginalName = "Expiry adapter" });
                seed.MarketplaceCommodityOrder.Add(new() { Id = 2, OwnerCharacterId = 200, Item2Id = 7001, Quantity = 1, PricePerUnit = 10, Price = 10, IsBuyOrder = true,
                    ListTime = (ulong)DateTime.UtcNow.AddHours(-1).ToFileTimeUtc(), ExpirationTime = (ulong)DateTime.UtcNow.AddMinutes(-1).ToFileTimeUtc() });
            }
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = seed.Database.GetDbConnection().ConnectionString });
        }
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
        ip.SetProperty(nameof(IItemInfo.Id), 7001u); ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20 });
        ip.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy); ulong itemId = 1000;
        itemProxy.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => itemId++); itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Events), events); ap.SetProperty(nameof(IAccount.Session), session);
        var model = Assert.Single(await database.GetCharacters(12));
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorSession: session, actorEvents: events,
            characterDatabase: fault is "reuse-rollback" or "earning-rollback" ? new OrdinaryRollback(f) : null,
            inventoryFactory: (owner, loaded) => LoadedInventory(owner, loaded, items, f.Source.Tables, mail ? buy ? 0u : 1u : full || routing ? 1u : 20u));
        sp.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
        if (satchel) actor.Set(nameof(Player.SupplySatchelManager), new SupplySatchelManager(actor.Player, model, f.Source.Tables));
        if (mail || routing) actor.Set(nameof(Player.MailManager), new MailManager(actor.Player, model, itemManager: items, gameTableManager: f.Source.Tables, deliveryContexts: () => f.Context()));
        await actor.Load(); actor.Quest.Single().Progress = 2; currency.CurrencyAddAmount(CurrencyType.Credits, 20);
        IItem original = !buy ? actor.Player.Inventory.GetItem(900ul) : null;
        var otherEvents = new EventQueue(); var otherSession = RecordingDispatchProxy<IWorldSession>.Create(out var otherSp);
        otherSp.SetProperty(nameof(IWorldSession.Events), otherEvents);
        var otherAccount = RecordingDispatchProxy<IAccount>.Create(out var otherAp);
        otherAp.SetProperty(nameof(IAccount.Id), 13u); otherAp.SetProperty(nameof(IAccount.Session), otherSession);
        otherAp.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var otherModel = otherOnline ? Assert.Single(await database.GetCharacters(13)) : null;
        using var other = otherOnline ? new RewardNormalSaveSnapshotTests.Actor(f, actorModel: otherModel, actorAccount: otherAccount,
            actorSession: otherSession, actorEvents: otherEvents, inventoryFactory: (owner, loaded) => LoadedInventory(owner, loaded, items, f.Source.Tables, 20)) : null;
        if (other != null)
        {
            otherSp.SetProperty(nameof(IWorldSession.Player), other.Player);
            var otherCurrency = new CurrencyManager(f.Source.Tables); otherCurrency.Initialise(other.Player, otherModel);
            other.Set(nameof(Player.CurrencyManager), otherCurrency); await other.Load(); otherCurrency.CurrencyAddAmount(CurrencyType.Credits, 15);
        }
        var achievements = (RecordingDispatchProxy<ICharacterAchievementManager>)(object)actor.Player.AchievementManager;
        int achievementBaseline = achievements.GetInvocations("CheckAchievements").Count;
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var expiryAdapter = RecordingDispatchProxy<IPlayer>.Create(out var expiryAdapterProxy);
        var expiryCurrency = RecordingDispatchProxy<ICurrencyManager>.Create(out var expiryCurrencyProxy);
        expiryAdapterProxy.SetProperty(nameof(IPlayer.CharacterId), 200ul); expiryAdapterProxy.SetProperty(nameof(IPlayer.CurrencyManager), expiryCurrency);
        GlobalMarketplaceManager manager = null;
        int expiryLookups = 0; bool lookupProtected = false;
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp); pp.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args =>
        {
            if ((ulong)args[0] == 42) return actor.Player;
            if (!expiryProbe || (ulong)args[0] != 200) return other?.Player;
            if (lookupProbe && ++expiryLookups == 1)
            {
                manager.Update(1.01);
                using var observed = f.Context();
                lookupProtected = expiryLookups == 1 && observed.MarketplaceCommodityOrder.Any(row => row.Id == 2)
                    && expiryCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)).Count == 0;
            }
            return expiryAdapter;
        });
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var assetsProxy); ulong mailId = 99;
        assetsProxy.SetMethodReturnFactory("get_" + nameof(IAssetManager.NextMailId), () => mailId++);
        var cleanup = new CleanupManager();
        int executions = 0;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager = new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup,
            new GlobalMarketplaceManager.CommodityPostingExecution(async command =>
            {
                executions++;
                if (fault == "competing") { held.TrySetResult(true); await release.Task; }
                var outcome = await command.ExecuteAsync();
                if (later) events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    currency.CurrencyAddAmount(CurrencyType.Credits, 5); actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, info, 4);
                    if (fault == "replace") sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    if (fault == "retire") actor.Cleanup();
                }));
                if (fault is "lost" or "earning-lost") throw new IOException("Commodity posting committed; acknowledgement lost.");
                return outcome;
            })); manager.Initialise();
        bool replied = false, onActor = false, scopeDisposed = false, competingQueued = false, insideLegacy = false, expiryProtected = false;
        int rejected = 0, replacementBoundary = -1;
        int fillAttempts = 0, refreshAttempts = 0;
        int replyAttempts = 0;
        bool replyRelisted = false;
        int reentrantRejected = 0;
        int otherSaves = 0;
        bool otherPaused = false, reloaded = false, relisted = false;
        if (pauseOther) other.OnAuth = () => otherSaves++;
        GenericError? legacyResult = null;
        Exception setterMutation = null, apiMutation = null;
        sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (failFill && args[0] is ServerCommodityAuctionFilledPartial)
            {
                fillAttempts++;
                if (fault == "fill-replace")
                    sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                throw new IOException("Commodity fill notice failed before enqueue.");
            }
            if (failFill && args[0] is ServerOwnedCommodityOrders refresh)
            {
                refreshAttempts++;
                Assert.True(replied && onActor);
                Assert.False(Pending(manager));
                Assert.False(cleanup.IsAccountLocked(f.Source.Player.Account));
                if (immediate) Assert.Empty(refresh.Orders);
                else Assert.Equal(1u, Assert.Single(refresh.Orders).Quantity);
                if (!immediate && refreshAttempts == 1)
                    throw new IOException("Commodity owned-order refresh failed before enqueue.");
            }
            if (fault == "publish" && args[0] is ServerCombatReward)
            {
                setterMutation = Record.Exception(() => original.StackCount = 9);
                apiMutation = Record.Exception(() => actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, info, 4));
            }
            if (fault == "reuse-packet-replace" && args[0] is ServerItemDelete)
            {
                sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                replacementBoundary = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
            }
            if (args[0] is not ServerCommodityOrderResult response) return null;
            if (fault == "legacy-reentrant" && insideLegacy && response.Result == GenericError.DbFailure) { reentrantRejected++; return null; }
            if (fault == "competing" && response.Result == GenericError.DbFailure) { rejected++; return null; }
            if (refused)
            {
                if (rejectErrorReply)
                {
                    Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
                    Assert.NotEqual(999u, response.OrderPosted.Quantity);
                    if (++errorAttempts == 1) { response.OrderPosted.Quantity = 999; throw new IOException("Owned commodity post rejection before acceptance."); }
                    Assert.Equal(2, errorAttempts);
                }
                Assert.Equal(fault == "missing-items" ? GenericError.ItemBadId : GenericError.VendorNotEnoughCash, response.Result);
                using var refusal = f.Context(); Check(refusal); replied = true; return null;
            }
            Assert.Equal(GenericError.Ok, response.Result); Assert.True(onActor); Assert.True(scopeDisposed);
            using var read = f.Context(); Check(read);
            if (failReply)
            {
                Assert.False(Pending(manager));
                Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
                Assert.Equal(quantity, response.OrderPosted.Quantity);
                if (++replyAttempts <= (fault == "reply-held" ? 2 : 1))
                {
                    if (fault == "reply-replace")
                        sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    if (fault == "reply-relisted")
                    {
                        var order = read.MarketplaceCommodityOrder.Single();
                        order.Quantity = 4; order.Price = order.PricePerUnit * order.Quantity;
                        read.SaveChanges(); manager.Initialise(); replyRelisted = true;
                    }
                    response.OrderPosted.Quantity = 0;
                    throw new IOException("Commodity post result failed before enqueue.");
                }
            }
            replied = true; return null;
        });
        using var provider = new ServiceCollection().AddSingleton<IGlobalMarketplaceManager>(manager).AddSingleton(items)
            .AddSingleton<IGameTableManager>(f.Source.Tables)
            .AddSingleton<ILogger<ClientCommoditySellOrderSubmitHandler>>(NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance)
            .AddSingleton<ILogger<ClientCommodityOrderCancelHandler>>(NullLogger<ClientCommodityOrderCancelHandler>.Instance)
            .AddScoped<MarketplaceRequestHelper>().AddScoped<ClientCommoditySellOrderSubmitHandler>().AddScoped<ClientCommodityOrderCancelHandler>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
        {
            using (var scope = provider.CreateScope())
            {
                var request = new ClientCommoditySellOrderSubmit();
                request.Order.Item2Id = 7001; request.Order.Quantity = quantity; request.Order.PricePerUnit = buy ? 12ul : 10ul;
                request.Order.Price = request.Order.PricePerUnit * quantity; request.Order.IsBuyOrder = buy; request.Order.ForceImmediate = immediate;
                void Submit() => scope.ServiceProvider.GetRequiredService<ClientCommoditySellOrderSubmitHandler>().HandleMessage(session, request);
                if (fault.StartsWith("legacy-", StringComparison.Ordinal))
                {
                    var adapter = RecordingDispatchProxy<IPlayer>.Create(out var adapterProxy);
                    var inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
                    var money = RecordingDispatchProxy<ICurrencyManager>.Create(out var moneyProxy);
                    adapterProxy.SetProperty(nameof(IPlayer.CharacterId), 999ul); adapterProxy.SetProperty(nameof(IPlayer.Account), f.Source.Player.Account);
                    adapterProxy.SetProperty(nameof(IPlayer.Inventory), inventory); adapterProxy.SetProperty(nameof(IPlayer.CurrencyManager), money);
                    inventoryProxy.SetProperty(nameof(IInventory.SupportsTransactionalSoulboundDelivery), true);
                    inventoryProxy.SetMethodHandler(nameof(IInventory.TryAcquireMutationLease), args =>
                    {
                        if (fault == "legacy-admission") Submit(); args[0] = new NoopLease(); return true;
                    });
                    moneyProxy.SetMethodHandler(nameof(ICurrencyManager.CanAfford), _ =>
                    {
                        if (fault == "legacy-reentrant") Submit();
                        if (expiryProbe)
                        {
                            manager.Update(1.01);
                            using var observed = f.Context();
                            expiryProtected = observed.MarketplaceCommodityOrder.Any(row => row.Id == 2)
                                && expiryCurrencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)).Count == 0;
                        }
                        return false;
                    });
                    insideLegacy = true;
                    legacyResult = manager.PostCommodityOrder(adapter, new CommodityOrder { Item2Id = 7001, Quantity = 1, PricePerUnit = 12, Price = 12, IsBuyOrder = true }, out _, items);
                    insideLegacy = false;
                    if (reentrantRejected == 1 || expiryProbe) Submit();
                }
                else { if (lookupProbe) manager.Update(1.01); Submit(); }
                request.Order.Item2Id = 7002; request.Order.Quantity = 999; request.Order.PricePerUnit = 1;
                request.Order.Price = 999; request.Order.IsBuyOrder = !buy;
            }
            scopeDisposed = true;
        }));
        var deadline = Stopwatch.StartNew();
        do
        {
            if (held.Task.IsCompleted && !competingQueued)
            {
                competingQueued = true;
                events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    Assert.Throws<InvalidOperationException>(manager.Initialise);
                    var adapter = RecordingDispatchProxy<IPlayer>.Create(out _);
                    Assert.Equal(GenericError.DbFailure, manager.PostCommodityOrder(adapter, new CommodityOrder { Item2Id = 7001 }, out _, items));
                    using var scope = provider.CreateScope();
                    var post = new ClientCommoditySellOrderSubmit(); post.Order.Item2Id = 7001; post.Order.Quantity = 1; post.Order.PricePerUnit = 12; post.Order.Price = 12; post.Order.IsBuyOrder = true;
                    scope.ServiceProvider.GetRequiredService<ClientCommoditySellOrderSubmitHandler>().HandleMessage(session, post);
                    var cancel = new ClientCommodityOrderCancel();
                    typeof(ClientCommodityOrderCancel).GetProperty(nameof(ClientCommodityOrderCancel.CommodityOrderId))!.SetValue(cancel, 1ul);
                    typeof(ClientCommodityOrderCancel).GetProperty(nameof(ClientCommodityOrderCancel.Item2Id))!.SetValue(cancel, 7001u);
                    scope.ServiceProvider.GetRequiredService<ClientCommodityOrderCancelHandler>().HandleMessage(session, cancel);
                }));
            }
            manager.Update(.01); onActor = true; events.Update(.01);
            if (!otherPaused || replied)
            {
                if (otherPaused)
                {
                    Assert.False(Pending(manager));
                    Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
                    Assert.False(cleanup.IsAccountLocked(other.Player.Account));
                    if (fault == "counterparty-relisted" && !relisted)
                    {
                        using var changed = f.Context();
                        changed.MarketplaceCommodityOrder.Add(new() { Id = 1, OwnerCharacterId = 100, Item2Id = 7001,
                            Quantity = 4, PricePerUnit = 20, Price = 80, ListTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                            ExpirationTime = (ulong)DateTime.UtcNow.AddHours(1).ToFileTimeUtc() });
                        changed.SaveChanges(); manager.Initialise(); relisted = true;
                    }
                    if (fault == "counterparty-reload" && !reloaded) { manager.Initialise(); reloaded = true; }
                }
                otherEvents.Update(.01);
            }
            if (pauseOther && otherSaves == 2 && otherSession.CanProcessIncomingPackets
                && !other.Player.IsNormalSaveInProgress && Pending(manager)) otherPaused = true;
            onActor = false;
            if (rejected == 2) release.TrySetResult(true);
            Assert.Empty(actor.Errors); if (other != null) Assert.Empty(other.Errors);
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(20), "Commodity posting did not settle."); await Task.Delay(5);
        } while (Pending(manager) || events.PendingEvents || otherEvents.PendingEvents || actor.Player.IsNormalSaveInProgress || other?.Player.IsNormalSaveInProgress == true);
        using var final = f.Context(); Check(final); Assert.False(cleanup.IsAccountLocked(f.Source.Player.Account));
        Assert.Equal(!suppressed, replied); Assert.Equal(refused ? 0 : 1, executions);
        if (failReply)
        {
            var emitted = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
            Assert.Equal(suppressed ? 1 : fault == "reply-held" ? 3 : 2, replyAttempts);
            Assert.Equal(match ? 1 : 0, emitted.OfType<ServerCommodityAuctionFilledPartial>().Count());
            if (suppressed) Assert.Empty(emitted.OfType<ServerOwnedCommodityOrders>());
            else
            {
                var refreshes = emitted.OfType<ServerOwnedCommodityOrders>().ToArray();
                Assert.Equal(2, refreshes.Length);
                Assert.All(refreshes, refresh => Assert.Equal(replyRelisted ? 4u : 1u, Assert.Single(refresh.Orders).Quantity));
                var resultIndices = Enumerable.Range(0, emitted.Length).Where(index => emitted[index] is ServerCommodityOrderResult).ToArray();
                int firstRefresh = Array.IndexOf(emitted, refreshes[0]);
                Assert.InRange(resultIndices[^1], firstRefresh + 1, Array.IndexOf(emitted, refreshes[1]) - 1);
                if (fault == "reply-held")
                    Assert.True(firstRefresh < resultIndices[1]);
            }
        }
        if (pauseOther)
        {
            Assert.True(otherPaused);
            var notices = otherSp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
            if (relisted || reloaded)
            {
                Assert.Empty(notices.OfType<ServerCommodityAuctionFilledPartial>());
                var orders = Assert.Single(notices.OfType<ServerOwnedCommodityOrders>()).Orders;
                if (relisted) Assert.Equal(4u, Assert.Single(orders).Quantity);
                else Assert.Empty(orders);
            }
            else Assert.Single(notices.OfType<ServerCommodityAuctionFilledPartial>());
            var requester = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
            Assert.Equal(1u, Assert.Single(Assert.Single(requester.OfType<ServerOwnedCommodityOrders>()).Orders).Quantity);
            Assert.True(Array.FindIndex(requester, packet => packet is ServerCommodityOrderResult)
                < Array.FindIndex(requester, packet => packet is ServerOwnedCommodityOrders));
        }
        if (failFill)
        {
            Assert.Equal(1, fillAttempts);
            Assert.Equal(suppressed ? 0 : immediate ? 1 : 2, refreshAttempts);
            if (otherOnline)
                Assert.Single(otherSp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)),
                    call => call.Arguments[0] is ServerCommodityAuctionFilledPartial);
        }
        if (fault == "legacy-admission") Assert.Equal(GenericError.DbFailure, legacyResult.GetValueOrDefault());
        if (fault == "legacy-reentrant") { Assert.Equal(GenericError.VendorNotEnoughCash, legacyResult.GetValueOrDefault()); Assert.Equal(1, reentrantRejected); }
        if (fault == "legacy-expiry") { Assert.Equal(GenericError.VendorNotEnoughCash, legacyResult.GetValueOrDefault()); Assert.True(expiryProtected, "Legacy expiry bypassed the held commodity book."); }
        if (lookupProbe) Assert.True(lookupProtected, "First expiry actor lookup reentered without admission.");
        Assert.Equal(ExpectedCredits(), achievements.GetInvocations("CheckAchievements").Skip(achievementBaseline)
            .Where(call => call.Arguments[1] is AchievementType.CurrencyEarned && (uint)call.Arguments[2] == (uint)CurrencyType.Credits)
            .Select(call => (uint)call.Arguments[4]));
        if (fault == "competing") Assert.Equal(2, rejected);
        if (fault == "publish") { Assert.IsType<InvalidOperationException>(setterMutation); Assert.IsType<InvalidOperationException>(apiMutation); }
        if (replacementBoundary >= 0)
            Assert.DoesNotContain(sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Skip(replacementBoundary),
                call => call.Arguments[0] is ServerItemAdd or ServerCommodityOrderResult or ServerCommodityAuctionFilledPartial or ServerMailAvailable);
        if (full)
        {
            Assert.DoesNotContain(final.Item, row => row.Id == 900);
            Assert.Throws<InvalidOperationException>(() => original.StackCount = 1);
        }
        if (multi && !routing)
        {
            var fills = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerCommodityAuctionFilledPartial>().ToArray();
            Assert.Equal(new uint[] { 2, 1 }, fills.Select(packet => packet.OrderFilled.Quantity));
            Assert.Equal(new ulong[] { 8, 9 }, final.CharacterMail.Select(row => row.CurrencyAmount).ToArray().Order());
        }

        void Check(CharacterContext context)
        {
            if (refused)
            {
                Assert.Equal(fault == "missing-cash" ? 20ul : 120ul, context.CharacterCurrency.Single(row => row.Id == 42).Amount);
                Assert.Equal(2u, context.CharacterQuestObjective.Single().Progress);
                Assert.Empty(context.MarketplaceCommodityOrder); Assert.Empty(context.MarketplaceBidReceipt);
                Assert.Equal(buy ? 0 : stock, context.Item.ToArray().Aggregate(0u, (sum, row) => sum + row.StackCount)); return;
            }
            ulong expectedBalance = buy ? match ? immediate ? 100ul : 88ul : immediate ? 120ul : 84ul : match ? 139ul : 120ul;
            if (self) expectedBalance = buy ? 107ul : 143ul;
            if (multi) expectedBalance = 89;
            if (routing) expectedBalance = 520 - quantity / 2 * 19ul;
            if (later) expectedBalance += 5;
            Assert.Equal(expectedBalance, context.CharacterCurrency.Single(row => row.Id == 42).Amount);
            Assert.Equal(earning ? 22u + ExpectedCredits().Aggregate(0u, (sum, value) => sum + value) : 2u, context.CharacterQuestObjective.Single().Progress);
            var ownBook = context.MarketplaceCommodityOrder.Where(row => (!expiryProbe || row.OwnerCharacterId != 200)
                && (!relisted || row.Id != 1));
            Assert.Equal(immediate || routing ? 0 : 1, ownBook.Count());
            if (!immediate && !routing)
            {
                var order = ownBook.Single();
                Assert.Equal(42ul, order.OwnerCharacterId); Assert.Equal(replyRelisted ? 4u : match ? 1u : 3u, order.Quantity);
                Assert.Equal(7001u, order.Item2Id);
                Assert.Equal(order.PricePerUnit * order.Quantity, order.Price);
            }
            ulong ownedQuantity = context.Item.Where(row => row.OwnerId == 42).ToArray().Aggregate(0ul, (sum, row) => sum + row.StackCount);
            ulong expectedItems = buy ? match ? 2ul : 0ul : stock - (immediate ? match ? 2u : 0u : 3u);
            if (routing) expectedItems = quantity;
            if (self && !buy) expectedItems += 2;
            if (satchel) { expectedItems -= 2; Assert.Equal((ushort)100, context.CharacterTradeskillMaterial.Single().Amount); }
            Assert.Equal(expectedItems + (later ? 4ul : 0ul), ownedQuantity);
            Assert.Equal(otherOnline ? 3 : 2, context.MarketplaceBidReceipt.Count());
            if (otherOnline)
            {
                Assert.Equal(buy ? 234ul : 219ul, context.CharacterCurrency.Single(row => row.Id == 100).Amount);
                Assert.Equal(buy ? 0 : 2, context.Item.Where(row => row.OwnerId == 100).ToArray().Sum(row => (int)row.StackCount));
            }
            if (routing)
            {
                var direct = Assert.Single(context.Item.Where(row => row.OwnerId == 42 && row.Location == (ushort)InventoryLocation.Inventory));
                var mailed = Assert.Single(context.Item.Where(row => row.OwnerId == 42 && row.Location == (ushort)InventoryLocation.None));
                Assert.Equal(quantity / 2, direct.StackCount); Assert.Equal(quantity / 2, mailed.StackCount);
            }
        }
        uint[] ExpectedCredits()
        {
            if (refused) return [];
            List<uint> result = [];
            if (self) { result.Add(19); result.Add(4); }
            else if (buy && match)
            {
                if (multi) { result.Add(oppositeQuantity * 3); result.Add(oppositeQuantity * 2); }
                else result.Add(4);
            }
            else if (!buy && match) result.Add(19);
            if (buy && immediate) result.Add(match ? 12u : 36u);
            if (later) result.Add(5);
            return result.ToArray();
        }
    }

    private sealed class NoopLease : IDisposable { public void Dispose() { } }

    private static bool Pending(GlobalMarketplaceManager manager) =>
        ((IDictionary)typeof(GlobalMarketplaceManager).GetField("commodityPostings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).Count != 0
        || ((EventQueue)typeof(GlobalMarketplaceManager).GetField("settlementEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).PendingEvents;

    private sealed class OrdinaryRollback(Fixture f) : CharacterDatabase
    {
        private readonly RollbackOnce fault = new();
        public override Task SaveBidReconciliation(ulong characterId, Action<CharacterContext> action, MarketplaceBidReconciliation receipt)
            => CharacterAtomicSave.ExecuteBidReconciliation(() => f.Context(fault), characterId, action, receipt);
    }
    private sealed class RollbackOnce : DbTransactionInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Commodity posting ordinary save rolled back.");
            return new(result);
        }
    }

    private static Inventory LoadedInventory(Player owner, CharacterModel model, IItemManager items, IGameTableManager tables, uint capacity)
    {
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables);
        var bag = new Bag(InventoryLocation.Inventory, capacity, items);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag }); Set("deletedItems", new List<IItem>());
        foreach (var row in model.Item) bag.AddItem(new NexusForever.Game.Entity.Item(row, items, tables), row.BagIndex);
        return inventory;
    }
}
