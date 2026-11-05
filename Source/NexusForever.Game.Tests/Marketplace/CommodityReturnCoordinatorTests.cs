using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using System.Data.Common;
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
using NexusForever.Game.Static.Marketplace;
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

public sealed class CommodityReturnCoordinatorTests
{
    [Fact]
    public Task RejectedCommodityCancellationReplyRetriesAfterCustodyConflict()
        => PublicCancellationAndRealExpiryReturnEscrowAndFungibleItems(false, false, true, false, "error-retry");

    [Theory]
    [InlineData(true, false, true, false, "none")]
    [InlineData(true, false, true, true, "none")]
    [InlineData(true, true, true, false, "none")]
    [InlineData(true, true, true, false, "notice-retry")]
    [InlineData(true, true, true, false, "notice-replace")]
    [InlineData(true, false, true, false, "notice-retry")]
    [InlineData(false, false, true, false, "notice-replace")]
    [InlineData(true, true, false, false, "none")]
    [InlineData(true, false, true, false, "lost")]
    [InlineData(true, true, false, false, "lost")]
    [InlineData(true, false, true, false, "replace")]
    [InlineData(true, false, true, false, "retire")]
    [InlineData(true, false, true, false, "competing")]
    [InlineData(false, false, true, false, "none")]
    [InlineData(false, false, true, true, "none")]
    [InlineData(false, false, true, false, "multi")]
    [InlineData(false, false, true, false, "mail")]
    [InlineData(false, true, true, false, "mail-queue-paused")]
    [InlineData(false, true, true, false, "none")]
    [InlineData(false, true, false, false, "none")]
    [InlineData(false, false, true, false, "lost")]
    [InlineData(false, false, true, false, "replace")]
    [InlineData(false, false, true, false, "merge")]
    [InlineData(false, false, true, false, "satchel")]
    [InlineData(false, false, true, false, "satchel-new")]
    [InlineData(false, false, true, false, "satchel-later")]
    [InlineData(false, false, true, false, "satchel-rollback")]
    [InlineData(false, false, true, false, "custody-main")]
    [InlineData(false, false, true, false, "custody-late")]
    [InlineData(false, false, true, false, "duplicate-late")]
    [InlineData(false, false, true, false, "packet-replace")]
    [InlineData(false, false, true, false, "mail-replace")]
    [InlineData(false, false, true, false, "custody-grant")]
    [InlineData(false, false, true, false, "satchel-rebase")]
    [InlineData(false, false, true, false, "satchel-publish")]
    [InlineData(false, false, true, false, "custody-grant-late")]
    [InlineData(false, false, true, false, "custody-grant-retry")]
    [InlineData(false, false, true, false, "custody-grant-late-bound")]
    [InlineData(false, false, true, false, "satchel-wallet-publish")]
    [InlineData(false, false, true, false, "wallet-item-publish")]
    [InlineData(false, false, true, false, "wallet-item-setter-publish")]
    [InlineData(false, false, true, false, "wallet-item-durability-publish")]
    [InlineData(false, false, true, false, "wallet-item-chip-publish")]
    [InlineData(false, false, true, false, "wallet-item-rune-publish")]
    public async Task PublicCancellationAndRealExpiryReturnEscrowAndFungibleItems(bool buy, bool expiry, bool online, bool absent, string fault)
    {
        bool rejectErrorReply = fault == "error-retry";
        if (rejectErrorReply) fault = "custody-main";
        int errorAttempts = 0;
        using var f = new Fixture(null); f.Seed();
        bool failNotice = fault is "notice-retry" or "notice-replace";
        bool addLaterCurrency = fault != "none" && !failNotice;
        uint quantity = fault == "multi" ? 45u : 3u;
        bool mailed = !buy && (!online || fault.StartsWith("mail", StringComparison.Ordinal));
        ulong price = quantity * 10ul;
        bool satchel = fault.StartsWith("satchel", StringComparison.Ordinal);
        bool grantRecovery = fault.StartsWith("custody-grant", StringComparison.Ordinal);
        bool boundRecovery = fault == "custody-grant-late-bound";
        ulong custodyItem = boundRecovery ? 900ul : 1000ul;
        ushort materialBefore = fault is "satchel-rollback" or "satchel-rebase" ? (ushort)95 : fault == "satchel-new" ? (ushort)0 : (ushort)98;
        uint laterItems = fault == "satchel-later" || grantRecovery ? 4u : fault is "satchel-rollback" or "satchel-rebase" ? 1u : 0u;
        if (satchel) ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.TradeskillMaterial),
            PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 1, Item2IdStatRevolution = 7001 }));
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var ap = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        ap.SetProperty(nameof(IAccount.Id), 12u); ap.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            if (!absent) seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            if (satchel && fault != "satchel-new") seed.CharacterTradeskillMaterial.Add(new() { Id = 42, MaterialId = 1, Amount = materialBefore });
            if (fault == "merge" || boundRecovery) seed.Item.Add(new() { Id = 900, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                BagIndex = 0, StackCount = boundRecovery ? 10u : 18u, Soulbound = true, Durability = .65f });
            seed.MarketplaceCommodityOrder.Add(new() { Id = 1, OwnerCharacterId = 42, Item2Id = 7001, Quantity = quantity,
                PricePerUnit = 10, Price = price, IsBuyOrder = buy, ListTime = (ulong)DateTime.UtcNow.AddHours(-1).ToFileTimeUtc(),
                ExpirationTime = (ulong)DateTime.UtcNow.AddMinutes(expiry ? -1 : 30).ToFileTimeUtc() });
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
        var model = online ? Assert.Single(await database.GetCharacters(12)) : null;
        var ordinary = fault is "satchel-rollback" or "satchel-rebase" or "duplicate-late" or "custody-grant-retry" ? new ReconciliationDatabase(f, fault == "duplicate-late", fault == "satchel-rebase", fault == "custody-grant-retry") : null;
        using var actor = online ? new RewardNormalSaveSnapshotTests.Actor(f, characterDatabase: ordinary, actorModel: model, actorSession: session,
            actorEvents: events, inventoryFactory: (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables, mailed ? 0u : 20u)) : null;
        CurrencyManager currency = null;
        if (actor != null)
        {
            sp.SetProperty(nameof(IWorldSession.Player), actor.Player);
            currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
            if (satchel) actor.Set(nameof(Player.SupplySatchelManager), new SupplySatchelManager(actor.Player, model, f.Source.Tables));
            if (mailed) actor.Set(nameof(Player.MailManager), new MailManager(actor.Player, model, itemManager: items, gameTableManager: f.Source.Tables, deliveryContexts: () => f.Context()));
            await actor.Load(); actor.Quest.Single().Progress = 2;
            if (!absent) currency.CurrencyAddAmount(CurrencyType.Credits, 20);
        }
        int authSaves = 0;
        if (fault is "satchel-rollback" or "satchel-rebase" or "custody-late" or "custody-grant-late" or "custody-grant-late-bound") actor.OnAuth = () =>
        {
            if (++authSaves != 2) return;
            if (fault is "satchel-rollback" or "satchel-rebase") currency.CurrencyAddAmount(CurrencyType.Credits, 1);
            else { using var changed = f.Context(); changed.Item.Single(row => row.Id == custodyItem).Location = (ushort)InventoryLocation.PlayerBank; changed.SaveChanges(); }
        };
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var pp); pp.SetMethodHandler(nameof(IPlayerManager.GetPlayer), _ => actor?.Player);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var assetsProxy); ulong mailId = 99;
        assetsProxy.SetMethodReturnFactory("get_" + nameof(IAssetManager.NextMailId), () => mailId++);
        var cleanup = new CleanupManager(); int executions = 0, rejected = 0;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new GlobalMarketplaceManager(databases, f.Source.Tables, assets, players, items, cleanup,
            new GlobalMarketplaceManager.CommodityReturnExecution(async command =>
            {
                executions++; if (fault == "competing") { held.TrySetResult(true); await release.Task; }
                var result = await command.ExecuteAsync();
                if (fault is "custody-main" or "custody-grant")
                { using var changed = f.Context(); changed.Item.Single(row => row.Id == custodyItem).Location = (ushort)InventoryLocation.PlayerBank; await changed.SaveChangesAsync(); }
                if (actor != null && addLaterCurrency) events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                {
                    currency.CurrencyAddAmount(CurrencyType.Credits, 5);
                    if (laterItems != 0) actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, info, laterItems);
                    if (fault == "replace") sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    if (fault == "retire") actor.Cleanup();
                }));
                if (fault == "lost") throw new IOException("Commodity committed; acknowledgement lost.");
                return result;
            })); manager.Initialise();
        int packets = 0, noticeAttempts = 0, replacementBoundary = -1; bool onActor = false, scopeDisposed = false;
        bool custodyConflict = fault.StartsWith("custody-", StringComparison.Ordinal);
        Exception publicationMutation = null, directPublicationMutation = null;
        ulong balance = online ? (absent ? 0ul : 120ul) + (buy ? price : 0ul) + (addLaterCurrency ? 5ul : 0ul) + (fault is "satchel-rollback" or "satchel-rebase" ? 1ul : 0ul) : 100ul;
        sp.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward && fault.StartsWith("wallet-item-", StringComparison.Ordinal) && fault != "wallet-item-publish")
            {
                IItem item = actor.Player.Inventory.GetItem(1000ul);
                publicationMutation = Record.Exception(() =>
                {
                    switch (fault)
                    {
                        case "wallet-item-setter-publish": item.StackCount = 4; break;
                        case "wallet-item-durability-publish": item.Durability = .5f; break;
                        case "wallet-item-chip-publish": item.MicrochipIds.Add(33); break;
                        case "wallet-item-rune-publish": item.RuneSlots.Add(new()); break;
                    }
                });
            }
            if (fault == "wallet-item-publish" && args[0] is ServerCombatReward)
                publicationMutation = Record.Exception(() => actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, info, 4));
            if (fault == "satchel-wallet-publish" && args[0] is ServerCombatReward)
            {
                publicationMutation = Record.Exception(() => actor.Player.SupplySatchelManager.RemoveAmount((ushort)1, 1));
                directPublicationMutation = Record.Exception(() => ((IEnumerable<ITradeskillMaterial>)actor.Player.SupplySatchelManager).Single().Amount = 99);
            }
            if (fault == "satchel-publish" && args[0] is ServerItemAdd)
            {
                publicationMutation = Record.Exception(() => actor.Player.SupplySatchelManager.RemoveAmount((ushort)1, 1));
                directPublicationMutation = Record.Exception(() => ((IEnumerable<ITradeskillMaterial>)actor.Player.SupplySatchelManager).Single().Amount = 99);
            }
            if (fault == "packet-replace" && args[0] is ServerItemAdd)
            {
                sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                replacementBoundary = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count; return null;
            }
            if (args[0] is ServerCommodityOrderResult failed)
            {
                if (rejectErrorReply)
                {
                    Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
                    Assert.NotEqual(999u, failed.OrderPosted.Quantity);
                    if (++errorAttempts == 1) { failed.OrderPosted.Quantity = 999; throw new IOException("Owned commodity cancellation rejection before acceptance."); }
                    Assert.Equal(2, errorAttempts);
                }
                Assert.True(custodyConflict); Assert.Equal(GenericError.DbFailure, failed.Result);
                using var failedRead = f.Context(); Check(failedRead); packets++; return null;
            }
            if (args[0] is not ServerCommodityAuctionRemoved removed) return null;
            using var read = f.Context(); Check(read);
            Assert.True(onActor); if (!expiry) Assert.True(scopeDisposed);
            Assert.Equal(expiry ? AuctionEventType.Expire : AuctionEventType.Cancel, removed.Type);
            Assert.False(custodyConflict); Assert.Equal(1ul, removed.OrderRemoved.CommodityOrderId);
            if (failNotice)
            {
                Assert.False(Pending(manager));
                Assert.False(cleanup.IsAccountLocked(f.Source.Player.Account));
                Assert.Equal(7001u, removed.OrderRemoved.Item2Id);
                Assert.Equal(quantity, removed.OrderRemoved.Quantity);
                Assert.Equal(price, removed.OrderRemoved.Price);
                if (++noticeAttempts == 1)
                {
                    if (fault == "notice-replace")
                        sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    removed.OrderRemoved.Quantity = 0;
                    throw new IOException("Removal notice failed before enqueue.");
                }
            }
            packets++;
            if (fault == "mail-replace")
            {
                sp.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                replacementBoundary = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Count;
            }
            return null;
        });
        using var provider = new ServiceCollection().AddSingleton<IGlobalMarketplaceManager>(manager).AddSingleton(items)
            .AddSingleton<ILogger<ClientCommodityOrderCancelHandler>>(NullLogger<ClientCommodityOrderCancelHandler>.Instance)
            .AddScoped<MarketplaceRequestHelper>().AddScoped<ClientCommodityOrderCancelHandler>().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        if (!expiry) events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
        {
            var request = new ClientCommodityOrderCancel();
            typeof(ClientCommodityOrderCancel).GetProperty(nameof(ClientCommodityOrderCancel.CommodityOrderId))!.SetValue(request, 1ul);
            typeof(ClientCommodityOrderCancel).GetProperty(nameof(ClientCommodityOrderCancel.Item2Id))!.SetValue(request, 7001u);
            typeof(ClientCommodityOrderCancel).GetProperty(nameof(ClientCommodityOrderCancel.IsBuyOrder))!.SetValue(request, buy);
            using (var scope = provider.CreateScope()) scope.ServiceProvider.GetRequiredService<ClientCommodityOrderCancelHandler>().HandleMessage(session, request);
            scopeDisposed = true; typeof(ClientCommodityOrderCancel).GetProperty(nameof(ClientCommodityOrderCancel.CommodityOrderId))!.SetValue(request, 999ul);
        }));
        int saveCount = 0;
        if (fault == "mail-queue-paused") actor.OnAuth = () => saveCount++;
        var deadline = Stopwatch.StartNew(); bool competed = false, pausedMailbox = false;
        do
        {
            manager.Update(1.01);
            if (held.Task.IsCompleted && !competed)
            {
                competed = true;
                events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
                    manager.TryQueueCommodityCancellation(actor.Player, 1, 7001, true, (error, _) => { Assert.Equal(GenericError.DbFailure, error); rejected++; })));
            }
            onActor = true;
            if (pausedMailbox && !Pending(manager)) Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
            if (!pausedMailbox || !Pending(manager)) events.Update(.01);
            onActor = false;
            if (fault == "mail-queue-paused" && saveCount == 2 && session.CanProcessIncomingPackets
                && !actor.Player.IsNormalSaveInProgress && Pending(manager)) pausedMailbox = true;
            if (rejected == 1) release.TrySetResult(true);
            if (actor != null) Assert.Empty(actor.Errors);
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(20), "Commodity return did not settle."); await Task.Delay(5);
        } while (Pending(manager) || events.PendingEvents || actor?.Player.IsNormalSaveInProgress == true);
        using var final = f.Context(); Check(final); Assert.Equal(1, executions);
        Assert.Equal(online && fault is not ("replace" or "retire" or "packet-replace" or "notice-replace") ? 1 : 0, packets);
        if (failNotice) Assert.Equal(fault == "notice-retry" ? 2 : 1, noticeAttempts);
        var emitted = sp.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
        if (fault == "mail-queue-paused")
        {
            Assert.True(pausedMailbox);
            Assert.Single(emitted.OfType<ServerMailAvailable>());
        }
        if (custodyConflict) { Assert.False(actor.Player.Session.CanProcessIncomingPackets); Assert.Empty(emitted.OfType<ServerItemAdd>()); }
        if (replacementBoundary >= 0) Assert.DoesNotContain(emitted.Skip(replacementBoundary), packet => packet is ServerItemAdd or ServerMailAvailable or ServerCommodityAuctionRemoved);
        if (fault == "duplicate-late") Assert.Equal(1, ordinary.Injections);
        if (fault is "satchel-publish" or "satchel-wallet-publish")
        {
            Assert.All(emitted.OfType<ServerSupplySatchelUpdate>(), packet => Assert.Equal(final.CharacterTradeskillMaterial.Single().Amount, packet.StackCount));
            Assert.IsType<InvalidOperationException>(publicationMutation);
            Assert.IsType<InvalidOperationException>(directPublicationMutation);
        }
        if (fault == "competing") Assert.Equal(1, rejected);
        if (fault == "wallet-item-publish") Assert.IsType<InvalidOperationException>(publicationMutation);
        if (fault.StartsWith("wallet-item-", StringComparison.Ordinal) && fault != "wallet-item-publish")
        {
            var stored = final.Item.Single();
            var live = actor.Player.Inventory.GetItem(1000ul);
            var wire = Assert.Single(emitted.OfType<ServerItemAdd>()).InventoryItem.Item;
            Assert.Equal(stored.StackCount, wire.StackCount);
            Assert.Equal(stored.Durability, wire.Durability);
            Assert.Equal(stored.StackCount, live.StackCount);
            Assert.Equal(stored.Durability, live.Durability);
            Assert.Empty(live.MicrochipIds); Assert.Empty(live.RuneSlots);
            Assert.IsType<InvalidOperationException>(publicationMutation);
        }
        Assert.False(cleanup.IsAccountLocked(f.Source.Player.Account));

        void Check(CharacterContext context)
        {
            Assert.Empty(context.MarketplaceCommodityOrder);
            Assert.Equal(balance, context.CharacterCurrency.SingleOrDefault(row => row.Id == 42)?.Amount ?? 0);
            if (buy) { Assert.Empty(context.Item); Assert.Empty(context.CharacterMailAttachment); }
            else
            {
                uint materialAfter = satchel ? Math.Min(100u, materialBefore + quantity + laterItems) : 0;
                if (fault == "satchel-rebase") materialAfter = 81;
                ulong itemTotal = fault == "satchel-rebase" ? 0ul : quantity + laterItems + (boundRecovery ? 10ul : fault == "merge" ? 18ul : 0ul) - (satchel ? materialAfter - materialBefore : 0);
                Assert.Equal(itemTotal, context.Item.ToArray().Aggregate(0ul, (sum, row) => sum + row.StackCount));
                if (satchel) Assert.Equal(materialAfter, context.CharacterTradeskillMaterial.Single().Amount);
                Assert.All(context.Item, row => { Assert.Equal(42ul, row.OwnerId); Assert.Equal(boundRecovery || row.Id == 900, row.Soulbound); Assert.Equal(0u, row.Charges);
                    if (grantRecovery && row.Id != custodyItem)
                        Assert.True(row.Location is (ushort)InventoryLocation.Inventory or (ushort)InventoryLocation.None);
                    else Assert.Equal((ushort)(mailed ? InventoryLocation.None : custodyConflict ? InventoryLocation.PlayerBank : InventoryLocation.Inventory), row.Location); });
                if (mailed) Assert.Equal(Assert.Single(context.Item).Id, Assert.Single(context.CharacterMailAttachment).ItemGuid);
                else
                {
                    if (grantRecovery)
                    {
                        var recovered = Assert.Single(context.Item.Where(row => row.Id != custodyItem));
                        Assert.Equal(4u, recovered.StackCount); Assert.Equal((ushort)InventoryLocation.None, recovered.Location);
                        Assert.Equal(recovered.Id, Assert.Single(context.CharacterMailAttachment).ItemGuid);
                        Assert.Equal(42ul, Assert.Single(context.CharacterMail).RecipientId);
                        if (boundRecovery) Assert.Equal(.65f, recovered.Durability);
                    }
                    else Assert.Empty(context.CharacterMailAttachment);
                    Assert.All(context.Item, row => Assert.InRange(row.StackCount, 1u, 20u));
                }
            }
            Assert.Equal(online ? 2u : 1u, context.CharacterQuestObjective.Single().Progress);
            Assert.Equal(online ? fault is "satchel-rollback" or "satchel-rebase" ? 3 : 2 : 1, context.MarketplaceBidReceipt.Count());
            if (buy && !online) Assert.Equal(price, Assert.Single(context.CharacterMail).CurrencyAmount);
            else if (mailed) Assert.Equal(0ul, Assert.Single(context.CharacterMail).CurrencyAmount);
            else if (!grantRecovery) Assert.Empty(context.CharacterMail);
        }
    }

    private sealed class ReconciliationDatabase(Fixture fixture, bool duplicate, bool replaceMaterial, bool replaceItem = false) : CharacterDatabase
    {
        private readonly RollbackOnce fault = new();
        private readonly DuplicateSlot duplicateSlot = new();
        internal int Injections => duplicateSlot.Injections;
        private int replaced;
        public override async Task SaveBidReconciliation(ulong characterId, Action<CharacterContext> action, MarketplaceBidReconciliation receipt)
        {
            try { await CharacterAtomicSave.ExecuteBidReconciliation(() => duplicate ? fixture.Context(duplicateSlot) : fixture.Context(fault), characterId, action, receipt); }
            catch (BidReconciliationRolledBackException) when ((replaceMaterial || replaceItem) && Interlocked.CompareExchange(ref replaced, 1, 0) == 0)
            {
                using var changed = fixture.Context();
                if (replaceItem) changed.Item.Single(row => row.Id == 1000).Location = (ushort)InventoryLocation.PlayerBank;
                else changed.CharacterTradeskillMaterial.Single().Amount = 80;
                await changed.SaveChangesAsync(); throw;
            }
        }
    }
    private sealed class DuplicateSlot : SaveChangesInterceptor
    {
        internal int Injections;
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data, int result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref Injections, 1, 0) == 0)
            {
                var context = (CharacterContext)data.Context!;
                var type = context.Model.FindEntityType(typeof(ItemModel))!;
                var table = StoreObjectIdentifier.Table("item", null);
                var columns = type.GetProperties().Select(property => (property.Name, Column: property.GetColumnName(table)!)).ToArray();
                string target = string.Join(",", columns.Select(property => $"`{property.Column}`"));
                string source = string.Join(",", columns.Select(property => property.Name == nameof(ItemModel.Id) ? "3000" : $"`{property.Column}`"));
                // Only model-owned identifiers and fixed disposable-fixture literals.
                string sql = $"INSERT INTO `item` ({target}) SELECT {source} FROM `item` WHERE id=1000";
                Assert.Equal(1, context.Database.ExecuteSqlRaw(sql));
            }
            return new(result);
        }
    }
    private sealed class RollbackOnce : DbTransactionInterceptor
    {
        private int failed;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref failed, 1) == 0) throw new IOException("Commodity ordinary rollback.");
            return new(result);
        }
    }

    private static bool Pending(GlobalMarketplaceManager manager) =>
        ((IDictionary)typeof(GlobalMarketplaceManager).GetField("commodityReturns", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).Count != 0
        || ((EventQueue)typeof(GlobalMarketplaceManager).GetField("settlementEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).PendingEvents;
    private static Inventory EmptyInventory(Player owner, CharacterModel model, IItemManager items, IGameTableManager tables, uint capacity)
    {
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables);
        var bag = new Bag(InventoryLocation.Inventory, capacity, items);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
        Set("deletedItems", new List<IItem>());
        foreach (var row in model.Item)
        {
            var item = new NexusForever.Game.Entity.Item(row, items, tables); bag.AddItem(item, row.BagIndex);
        }
        return inventory;
    }
}
