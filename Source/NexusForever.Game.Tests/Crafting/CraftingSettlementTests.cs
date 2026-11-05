using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Crafting;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.WorldServer.Crafting;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;
using NexusForever.WorldServer.Network.Message.Handler.Authentication;
using NexusForever.WorldServer.Service;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Crafting;

public sealed class CraftingSettlementTests
{
    private readonly DbContextOptions<CharacterContext> providerCharacters;
    private readonly DatabaseConnectionString providerAuth;
    private readonly DatabaseConnectionString providerForeignAuth;
    private bool retryingSqlite;
    public CraftingSettlementTests() { }
    internal CraftingSettlementTests(DbContextOptions<CharacterContext> characters, DatabaseConnectionString auth,
        DatabaseConnectionString foreignAuth = null)
    { providerCharacters = characters; providerAuth = auth; providerForeignAuth = foreignAuth; }

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("ack-lost")]
    public async Task RetryingDatabasesKeepCraftingDeliveryAndAdmissionTransactionsInsideTheirStrategies(string outcome)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"nf-crafting-retries-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var options = new DbContextOptionsBuilder<CharacterContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "character.sqlite")};Pooling=False")
                .ReplaceService<IExecutionStrategyFactory, RetryFactory>().Options;
            await new CraftingSettlementTests(options, null) { retryingSqlite = true }
                .CraftingPublishesItsFrozenInventoryAndResultAfterTheCharacterSave("simple", "account", outcome);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("simple", "direct", "commit")]
    [InlineData("simple", "direct", "rollback")]
    [InlineData("simple", "direct", "lost")]
    [InlineData("simple", "direct", "replacement")]
    [InlineData("complex", "direct", "commit")]
    [InlineData("manual", "direct", "commit")]
    [InlineData("auto", "direct", "commit")]
    [InlineData("simple", "satchel", "commit")]
    [InlineData("simple", "satchel", "rollback")]
    [InlineData("simple", "loot", "commit")]
    [InlineData("simple", "loot", "rollback")]
    [InlineData("simple", "loot", "wallet-replacement")]
    [InlineData("simple", "virtual", "commit")]
    [InlineData("simple", "virtual", "rollback")]
    [InlineData("simple", "virtual", "lost")]
    [InlineData("simple", "virtual", "replacement")]
    [InlineData("simple", "virtual", "map-replacement")]
    [InlineData("simple", "virtual", "map-readmission")]
    [InlineData("simple", "virtual", "event-replacement")]
    [InlineData("simple", "account", "commit")]
    [InlineData("simple", "account", "rollback")]
    [InlineData("simple", "account", "lost")]
    [InlineData("simple", "account", "replacement")]
    [InlineData("simple", "account", "auth-rollback")]
    [InlineData("simple", "account", "auth-lost")]
    [InlineData("simple", "account", "ack-rollback")]
    [InlineData("simple", "account", "ack-lost")]
    [InlineData("simple", "account", "late-account-items")]
    [InlineData("simple", "account", "late-loot-retry")]
    [InlineData("simple", "account", "late-wallet")]
    [InlineData("simple", "account", "admission-replacement")]
    [InlineData("simple", "account", "admission-before-character")]
    [InlineData("simple", "account", "auth-route-replacement")]
    [InlineData("simple", "account", "wallet-cap")]
    [InlineData("simple", "account", "pending-spend")]
    [InlineData("simple", "account-deleted", "lost")]
    [InlineData("simple", "account-realm", "lost")]
    [InlineData("simple", "account-consumed", "ack-rollback")]
    [InlineData("complex", "account", "commit")]
    [InlineData("manual", "account", "commit")]
    [InlineData("auto", "account-cosmic", "commit")]
    [InlineData("simple", "account-wallet", "commit")]
    [InlineData("simple", "account-item", "commit")]
    [InlineData("simple", "account-cosmic", "commit")]
    [InlineData("simple", "account-cosmic", "auth-lost")]
    [InlineData("simple", "account-cosmic", "auth-rollback")]
    [InlineData("simple", "account-cosmic", "rollback")]
    [InlineData("simple", "account-cosmic", "loyalty-cap")]
    [InlineData("simple", "account", "outbox")]
    [InlineData("auto", "account-cosmic", "outbox")]
    [InlineData("simple", "account", "outbox-race")]
    public async Task CraftingPublishesItsFrozenInventoryAndResultAfterTheCharacterSave(string request, string output, string outcome)
    {
        using var f = new Fixture(providerCharacters, output == "virtual" ? new QuestObjectiveEntry
            { Id = 90010, Type = (uint)QuestObjectiveType.VirtualCollect, Data = 265, Count = 5 } : null);
        f.Seed();
        var database = new HeldDatabase(f, outcome);
        using (var seed = f.Context())
        {
            database.Initialise(new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
            seed.Character.Single().AccountId = 12;
            if (output == "satchel")
                seed.CharacterTradeskillMaterial.Add(new() { Id = 42, MaterialId = 55, Amount = 2 });
            else
                seed.Item.Add(new() { Id = 905, OwnerId = 42, ItemId = 8001,
                    Location = (ushort)InventoryLocation.Inventory, StackCount = 2, Durability = 1f });
            await seed.SaveChangesAsync();
        }
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        bool accountOutput = output.StartsWith("account", StringComparison.Ordinal);
        bool cosmicOutput = output == "account-cosmic";
        bool walletOutput = accountOutput && output != "account-item";
        bool itemOutput = accountOutput && output != "account-wallet";
        bool generated = output is "loot" or "virtual" || accountOutput;
        var schematic = new TradeskillSchematic2Entry { Id = 5001, Item2IdOutput = generated ? 0u : 7001u,
            OutputCount = 1, LootId = generated ? 9001u : 0u, Item2IdMaterial00 = 8001, MaterialCost00 = 2 };
        tables.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), PlayerBindPointTests.Table(schematic));
        tables.SetProperty(nameof(IGameTableManager.TradeskillMaterial), PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 55, Item2IdStatRevolution = 8001 }));
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
        tables.SetProperty(nameof(IGameTableManager.VirtualItem), PlayerBindPointTests.Table(new VirtualItemEntry { Id = 265 }));
        tables.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 14 }, new AccountCurrencyTypeEntry { Id = 8 }));
        tables.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(new[] { new AccountItemEntry { Id = 1101 } }
            .Concat(cosmicOutput ? LoyaltyCatalogBuild16042Fixture.AccountItems() : []).ToArray()));
        if (cosmicOutput)
        {
            tables.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
            tables.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Rewards()));
        }
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        var infos = new Dictionary<uint, IItemInfo>();
        foreach (uint id in new uint[] { 7001, 8001 })
        {
            var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
            ip.SetProperty(nameof(IItemInfo.Id), id);
            ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = id, MaxStackCount = id == 8001 ? 20u : 1u });
            ip.SetMethodReturn(nameof(IItemInfo.IsStackable), id == 8001);
            infos.Add(id, info);
        }
        itemManager.SetMethodHandler(nameof(IItemManager.GetItemInfo), args => infos.GetValueOrDefault((uint)args[0]));
        itemManager.SetProperty(nameof(IItemManager.NextItemId), 906ul);
        var messages = RecordingDispatchProxy<IMessageManager>.Create(out var messageManager);
        messageManager.SetMethodHandler(nameof(IMessageManager.GetOpcode), args => args[0].GetType().GetCustomAttribute<MessageAttribute>()?.Opcode);
        var session = new WorldSession(messages, null, null, null);
        typeof(GameSession).GetField("encryption", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(session, new PacketCrypt(123ul));
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u); account.SetProperty(nameof(IAccount.Session), session);
        AuthDatabase auth = null;
        DatabaseConnectionString authConnection = null;
        string foreignAuthPath = null;
        var authFault = new AwardFault(outcome == "auth-lost", allocator: outcome == "late-loot-retry") { Armed = outcome != "late-loot-retry" };
        if (accountOutput)
        {
            using var source = f.Context();
            bool faultAuth = outcome is "auth-rollback" or "auth-lost" or "late-loot-retry";
            auth = faultAuth || retryingSqlite ? new AwardDatabase(faultAuth ? authFault : null, retryingSqlite) : new AuthDatabase();
            authConnection = providerAuth ?? new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
                ConnectionString = $"Data Source={Path.Combine(Path.GetDirectoryName(source.Database.GetDbConnection().DataSource)!, "crafting-auth.sqlite")};Pooling=False" };
            auth.Initialise(authConnection);
            foreignAuthPath = Path.Combine(Path.GetDirectoryName(source.Database.GetDbConnection().DataSource)!, "crafting-foreign.sqlite");
            using var seed = auth.CreateContext(); seed.Database.EnsureCreated();
            var accountModel = new AccountModel { Id = 12, Email = "crafting-12@example.local", SessionKey = "00112233445566778899AABBCCDDEEFF" };
            seed.Account.Add(accountModel); seed.AccountWorldSession.Add(new() { AccountId = 12, Epoch = 1 });
            if (outcome is "wallet-cap" or "pending-spend") seed.AccountCurrency.Add(new() { Id = 12, CurrencyId = 14, Amount = ulong.MaxValue });
            if (outcome == "loyalty-cap") seed.AccountLoyalty.Add(new() { AccountId = 12, EarnedPoints = ulong.MaxValue, ActiveTrackId = 9 });
            seed.SaveChanges();
            account.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
            account.SetProperty(nameof(IAccount.CurrencyManager), new AccountCurrencyManager(f.Source.Player.Account,
                outcome == "wallet-cap" ? new AccountModel { Id = 12 } : accountModel, f.Source.Tables));
            account.SetProperty(nameof(IAccount.InventoryManager), new AccountInventoryManager(f.Source.Player.Account, accountModel, null,
                gameTableManager: f.Source.Tables, inventoryDatabase: auth));
            if (cosmicOutput) account.SetProperty(nameof(IAccount.LoyaltyManager), new AccountLoyaltyManager(f.Source.Player.Account, f.Source.Tables, auth));
        }
        var model = Assert.Single(await database.GetCharacters(12));
        using var actor = new Actor(f, actorModel: model, actorSession: session, actorEvents: session.Events,
            characterDatabase: database, authDatabase: auth,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, f.Source.Tables, null));
        if (auth != null) account.SetMethodHandler(nameof(IAccount.Save), args =>
        {
            var context = (AuthContext)args[0];
            actor.Player.Account.CurrencyManager.Save(context); actor.Player.Account.InventoryManager.Save(context);
            return null;
        });
        session.Player = actor.Player;
        actor.Set(nameof(Player.Guid), 77u);
        var wallet = new CurrencyManager(f.Source.Tables); wallet.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), wallet);
        actor.Set(nameof(Player.SupplySatchelManager), new SupplySatchelManager(actor.Player, model, f.Source.Tables));
        var publicEvents = RecordingDispatchProxy<IPublicEventManager>.Create(out var eventProxy);
        var replacementEvents = RecordingDispatchProxy<IPublicEventManager>.Create(out var replacementEventProxy);
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var replacementMap = RecordingDispatchProxy<IBaseMap>.Create(out var replacementMapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEvents);
        replacementMapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), replacementEvents);
        if (output == "virtual") actor.Set(nameof(Player.Map), map);
        bool eventSawCommittedState = false;
        eventProxy.SetMethodHandler(nameof(IPublicEventManager.UpdateObjective), _ =>
        {
            using var committed = f.Context();
            eventSawCommittedState = committed.Item.Count(row => row.ItemId == 7001) == 1
                && !committed.Item.Any(row => row.ItemId == 8001)
                && committed.CharacterQuestObjective.Single().Progress == 3u;
            return null;
        });
        await actor.Load();
        if (outcome == "pending-spend") actor.Player.Account.CurrencyManager.CurrencySubtractAmount(AccountCurrencyType.ProtostarPromissoryNote, ulong.MaxValue);
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory);
        foreach (var row in model.Item)
            inventory.LoadItem(new NexusForever.Game.Entity.Item(row, items, f.Source.Tables), InventoryLocation.Inventory, row.BagIndex);
        var modifiers = RecordingDispatchProxy<ICraftingModifierSessionStore>.Create(out var modifier);
        modifier.SetMethodHandler(nameof(ICraftingModifierSessionStore.TryBuildModifierItemCounts), args =>
        { args[3] = new Dictionary<uint, uint>(); args[4] = string.Empty; return true; });
        var delivery = new GlobalLootManager(null, itemManager: items, gameTableManager: f.Source.Tables);
        var loot = RecordingDispatchProxy<IGlobalLootManager>.Create(out var lp);
        lp.SetMethodHandler(nameof(IGlobalLootManager.TryGenerateLoot), args =>
        { args[3] = accountOutput
            ? new GeneratedLootItem[] { new(LootItemType.StaticItem, 7001, 1) }
                .Concat(walletOutput ? [new GeneratedLootItem(LootItemType.AccountCurrency, 14, 7)] : [])
                .Concat(itemOutput ? [new GeneratedLootItem(LootItemType.AccountItem, 1101, 1)] : [])
                .Concat(cosmicOutput ? [new GeneratedLootItem(LootItemType.AccountCurrency, 8, 800)] : []).ToArray()
            : new GeneratedLootItem[] { new(LootItemType.StaticItem, 7001, 1), output == "virtual" ? new(LootItemType.VirtualItem, 265, 2) : new(LootItemType.Cash, 1, 7) };
            args[4] = string.Empty; return true; });
        lp.SetMethodHandler(nameof(IGlobalLootManager.CanDeliverGeneratedLoot), args =>
        { bool allowed = delivery.CanDeliverGeneratedLoot((IPlayer)args[0], (IEnumerable<GeneratedLootItem>)args[1], out string reason); args[2] = reason; return allowed; });
        lp.SetMethodHandler(nameof(IGlobalLootManager.GiveGeneratedLoot), args =>
        { delivery.GiveGeneratedLoot((IPlayer)args[0], (IEnumerable<GeneratedLootItem>)args[1], (uint)args[2], (bool)args[3], (uint)args[4]); return null; });
        var queue = (ConcurrentQueue<ServerGamePacket>)typeof(GameSession).GetField("outgoingPackets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
        queue.Clear(); database.Armed = true;
        var laterItems = new List<IAccountInventoryItem>();
        if (outcome == "wallet-replacement")
            database.Committed = () => actor.Set(nameof(Player.CurrencyManager), RecordingDispatchProxy<ICurrencyManager>.Create(out _));
        if (outcome == "admission-replacement") database.Committed = () =>
        {
            using var replacement = auth.CreateContext();
            replacement.AccountWorldSession.Where(row => row.AccountId == 12).ExecuteUpdate(set => set.SetProperty(row => row.Epoch, 2ul));
        };
        if (outcome == "auth-route-replacement") database.Committed = () =>
        {
            if (providerForeignAuth != null)
            {
                // A fully valid destination carrying the original reservation
                // must still receive no award when its database route changes.
                var foreign = new AuthDatabase(); foreign.Initialise(providerForeignAuth);
                using var original = auth.CreateContext(); using var copied = foreign.CreateContext();
                copied.Account.Add(original.Account.AsNoTracking().Single());
                copied.AccountWorldSession.Add(original.AccountWorldSession.AsNoTracking().Single());
                copied.AccountCraftingReceipt.Add(original.AccountCraftingReceipt.AsNoTracking().Single());
                copied.SaveChanges(); auth.Initialise(providerForeignAuth);
            }
            else auth.Initialise(new DatabaseConnectionString
                { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignAuthPath};Pooling=False" });
        };
        if (outcome == "outbox-race") database.Committed = () =>
            Assert.Equal(1, new CraftingAccountOutbox(database.CreateContext, auth.CreateContext, 1, f.Source.Tables)
                .DrainAsync().GetAwaiter().GetResult());
        try
        {
            switch (request)
            {
                case "simple":
                    var simple = new ClientCraftingSimpleCraft(); Set(simple, "TradeskillSchematic2Id", 5001u);
                    new ClientCraftingSimpleCraftHandler(NullLogger<ClientCraftingSimpleCraftHandler>.Instance, f.Source.Tables, items, loot, modifiers).HandleMessage(session, simple);
                    break;
                case "complex":
                    var complex = new ClientCraftingComplexCraft(); Set(complex, "TradeskillSchematic2Id", 5001u);
                    new ClientCraftingComplexCraftHandler(NullLogger<ClientCraftingComplexCraftHandler>.Instance, f.Source.Tables, items, loot, modifiers).HandleMessage(session, complex);
                    break;
                case "manual":
                    var manual = new ClientCraftingCraftItem(); Set(manual, "TradeskillSchematic2Id", 5001u); Set(manual, "SchematicCount", 1u);
                    new ClientCraftingCraftItemHandler(NullLogger<ClientCraftingCraftItemHandler>.Instance, f.Source.Tables, items, loot, modifiers).HandleMessage(session, manual);
                    break;
                default:
                    var auto = new ClientCraftingCraftItemAutoCraft(); Set(auto, "TradeskillSchematic2Id", 5001u); Set(auto, "SchematicCount", 1u);
                    new ClientCraftingCraftItemAutoCraftHandler(NullLogger<ClientCraftingCraftItemAutoCraftHandler>.Instance, f.Source.Tables, items, loot, modifiers).HandleMessage(session, auto);
                    break;
            }
            Assert.Equal(1u, inventory.GetItemCount(7001));
            Assert.Empty(queue);
            Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
            if (outcome is "wallet-cap" or "loyalty-cap")
            {
                await actor.Drain(allowErrors: true);
                Assert.False(database.Entered.Task.IsCompleted); Assert.True(session.IsDisconnecting);
                using var unpaid = f.Context();
                Assert.Equal(2u, unpaid.Item.Single().StackCount); Assert.Empty(unpaid.CharacterCraftingAccountOutbox);
                using var bounded = auth.CreateContext();
                if (outcome == "wallet-cap") Assert.Equal(ulong.MaxValue, bounded.AccountCurrency.Single(row => row.CurrencyId == 14).Amount);
                else Assert.Equal(ulong.MaxValue, bounded.AccountLoyalty.Single().EarnedPoints);
                Assert.Empty(bounded.AccountInventory); Assert.Empty(bounded.AccountPendingItem); Assert.Empty(queue);
                Assert.False(bounded.AccountCraftingReceipt.Single().Delivered);
                bounded.AccountWorldSession.Single().Epoch = 2; await bounded.SaveChangesAsync();
                var reconnect = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
                await CraftingAccountRecovery.RecoverAtAdmissionAsync(reconnect, auth, database, 1, tables: null);
                Assert.True(bounded.AccountCraftingReceipt.AsNoTracking().Single().Cancelled);
                Assert.Empty(reconnect.AccountInventory);
                Assert.Empty(bounded.AccountEntitlement);
                return;
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!database.Entered.Task.IsCompleted) { session.Events.Update(.01); await Task.Delay(5, timeout.Token); }
            using var pending = f.Context();
            Assert.DoesNotContain(pending.Item, row => row.ItemId == 7001);
            if (auth != null)
            {
                using var uncommitted = auth.CreateContext();
                Assert.Equal(0ul, uncommitted.AccountCurrency.Where(row => row.CurrencyId == 14).SingleOrDefault()?.Amount ?? 0ul);
                Assert.Empty(uncommitted.AccountInventory);
                var reservation = Assert.Single(uncommitted.AccountCraftingReceipt);
                Assert.False(reservation.Delivered); Assert.False(reservation.Cancelled); Assert.False(reservation.Completed);
                Assert.Empty(pending.CharacterCraftingAccountOutbox);
            }
            if (output == "virtual") Assert.Equal(1u, pending.CharacterQuestObjective.Single().Progress);
            if (output == "satchel") Assert.Equal((ushort)2, pending.CharacterTradeskillMaterial.Single().Amount);
            else Assert.Equal(2u, pending.Item.Single().StackCount);
            if (outcome == "outbox")
            {
                var worker = new CraftingAccountOutbox(database.CreateContext, auth.CreateContext, 1, f.Source.Tables);
                Assert.Equal(0, await worker.DrainAsync());
                using var waiting = auth.CreateContext();
                var receipt = waiting.AccountCraftingReceipt.Single();
                Assert.False(receipt.Delivered); Assert.False(receipt.Cancelled); Assert.False(receipt.Completed);
                Assert.Empty(pending.CharacterCraftingAccountOutbox);
            }
            if (outcome is "replacement" or "outbox") session.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
            if (outcome == "late-account-items")
            {
                laterItems.Add(actor.Player.Account.InventoryManager.AddItem(1101, notify: false));
                var staleManager = new AccountInventoryManager(actor.Player.Account, new AccountModel { Id = 12 }, null,
                    gameTableManager: f.Source.Tables, inventoryDatabase: auth);
                laterItems.Add(staleManager.AddItem(1101, notify: false));
                Assert.NotEqual(laterItems[0].Id, laterItems[1].Id);
                Assert.All(laterItems, item => Assert.False(item.HasTargetPlayerIdentity));
            }
            if (outcome == "late-wallet") actor.Player.Account.CurrencyManager.CurrencyAddAmount(AccountCurrencyType.ProtostarPromissoryNote, 3);
            if (outcome == "late-loot-retry")
            {
                authFault.Armed = true;
                ulong counter;
                using (var before = auth.CreateContext()) counter = before.Account.Single().NextInventoryId;
                var manager = Assert.IsType<AccountInventoryManager>(actor.Player.Account.InventoryManager);
                Assert.ThrowsAny<Exception>(() => delivery.GiveGeneratedLoot(actor.Player, [new GeneratedLootItem(LootItemType.AccountItem, 1101, 2)], ownerUnitId: 0, sendGrantedNotify: false));
                Assert.True(authFault.Fired); Assert.Empty(manager); Assert.Empty(queue);
                using (var failed = auth.CreateContext()) Assert.Equal(counter, failed.Account.AsNoTracking().Single().NextInventoryId);
                delivery.GiveGeneratedLoot(actor.Player, [new GeneratedLootItem(LootItemType.AccountItem, 1101, 2)], ownerUnitId: 0, sendGrantedNotify: false);
                laterItems.AddRange(manager);
                Assert.Equal(2, laterItems.Count); Assert.Equal(2, laterItems.Select(item => item.Id).Distinct().Count());
                Assert.All(laterItems, item => Assert.False(item.HasTargetPlayerIdentity));
            }
            if (outcome == "admission-before-character")
            {
                using var admission = auth.CreateContext();
                admission.AccountWorldSession.Single().Epoch = 2; await admission.SaveChangesAsync();
                var reconnect = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
                await CraftingAccountRecovery.RecoverAtAdmissionAsync(reconnect, auth, database, 1, f.Source.Tables);
                Assert.Empty(reconnect.AccountInventory);
                Assert.Equal(0ul, reconnect.AccountCurrency.SingleOrDefault(row => row.CurrencyId == 14)?.Amount ?? 0ul);
            }
            if (outcome == "map-replacement") actor.Set(nameof(Player.Map), replacementMap);
            if (outcome == "map-readmission") ((IPlayerMapAdmission)actor.Player).BeginMapAdmission();
            if (outcome == "event-replacement") mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), replacementEvents);
        }
        finally { database.Release.TrySetResult(); }
        await actor.Drain(allowErrors: outcome is "rollback" or "lost" or "auth-rollback" or "ack-rollback" or "admission-before-character");
        if (outcome == "outbox")
        {
            using (var before = auth.CreateContext())
            {
                Assert.False(before.AccountCraftingReceipt.Single().Delivered);
                Assert.Equal(1ul, before.AccountWorldSession.Single().Epoch);
            }
            var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var databaseProxy);
            databaseProxy.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) =>
                method.GetGenericArguments()[0] == typeof(AuthDatabase) ? auth : database);
            var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
            realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
            using var service = new CraftingAccountOutboxHostedService(databases, realm, f.Source.Tables,
                NullLogger<CraftingAccountOutboxHostedService>.Instance);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await service.StartAsync(timeout.Token);
            try
            {
                while (true)
                {
                    using var delivered = auth.CreateContext();
                    if (delivered.AccountCraftingReceipt.Single().Completed) break;
                    await Task.Delay(10, timeout.Token);
                }
            }
            finally { await service.StopAsync(timeout.Token); }
            Assert.True(service.ExecuteTask.IsCompletedSuccessfully);
            using var observed = auth.CreateContext();
            Assert.Equal(1ul, observed.AccountWorldSession.Single().Epoch);
            Assert.Empty(queue);
        }
        if (outcome == "auth-route-replacement")
        {
            if (providerForeignAuth == null) Assert.False(File.Exists(foreignAuthPath));
            else
            {
                using var foreign = auth.CreateContext();
                Assert.Empty(foreign.AccountInventory); Assert.Empty(foreign.AccountCurrency);
                var copied = foreign.AccountCraftingReceipt.Single(); Assert.False(copied.Delivered); Assert.False(copied.Completed);
            }
            auth.Initialise(authConnection);
        }
        using var stored = f.Context();
        bool committed = outcome is not ("rollback" or "admission-before-character");
        Assert.Equal(committed ? 1 : 0, stored.Item.Count(row => row.ItemId == 7001));
        if (output == "satchel") Assert.Equal(committed ? (ushort)0 : (ushort)2, stored.CharacterTradeskillMaterial.Single().Amount);
        else Assert.Equal(committed ? 0 : 1, stored.Item.Count(row => row.ItemId == 8001));
        if (output == "loot") Assert.Equal(committed ? 7ul : 0ul, stored.CharacterCurrency.SingleOrDefault()?.Amount ?? 0ul);
        if (output == "virtual") Assert.Equal(committed ? 3u : 1u, stored.CharacterQuestObjective.Single().Progress);
        if (auth != null)
        {
            using var awarded = auth.CreateContext();
            bool delivered = outcome is "commit" or "auth-lost" or "ack-lost" or "ack-rollback" or "late-account-items" or "late-loot-retry" or "late-wallet" or "pending-spend" or "admission-replacement" or "outbox" or "outbox-race";
            Assert.Equal((delivered && walletOutput ? 7ul : 0ul, delivered && itemOutput ? 1 : 0),
                (awarded.AccountCurrency.SingleOrDefault(row => row.CurrencyId == 14)?.Amount ?? 0ul, awarded.AccountInventory.Count()));
            Assert.Empty(awarded.AccountEntitlement);
            var receipt = Assert.Single(awarded.AccountCraftingReceipt);
            Assert.Equal(delivered, receipt.Delivered);
            Assert.Equal(outcome == "admission-before-character", receipt.Cancelled);
            Assert.Equal(outcome is "commit" or "auth-lost" or "ack-lost" or "late-account-items" or "late-loot-retry" or "late-wallet" or "pending-spend" or "admission-replacement" or "admission-before-character" or "outbox" or "outbox-race", receipt.Completed);
            if (committed) Assert.Equal(outcome is "commit" or "auth-lost" or "ack-lost" or "late-account-items" or "late-loot-retry" or "late-wallet" or "pending-spend" or "admission-replacement" or "outbox" or "outbox-race", stored.CharacterCraftingAccountOutbox.Single().Delivered);
            else if (outcome == "admission-before-character") Assert.True(stored.CharacterCraftingAccountOutbox.Single().Cancelled);
            else Assert.Empty(stored.CharacterCraftingAccountOutbox);
            if (cosmicOutput && delivered)
            {
                Assert.Equal(800ul, awarded.AccountCurrency.Single(row => row.CurrencyId == 8).Amount);
                Assert.Equal(800ul, awarded.AccountLoyalty.Single().EarnedPoints);
                Assert.Single(awarded.AccountPendingItem);
                Assert.Single(awarded.AccountLoyaltySlot.Where(row => row.Claimed));
                Assert.Equal(outcome == "outbox" ? 0ul : 800ul, actor.Player.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.CosmicReward));
            }
            else if (cosmicOutput)
            {
                Assert.Equal(0ul, awarded.AccountLoyalty.Single().EarnedPoints);
                Assert.Empty(awarded.AccountPendingItem);
                Assert.Empty(awarded.AccountLoyaltySlot.Where(row => row.Claimed));
            }
            if (outcome is "auth-rollback" or "auth-lost") Assert.True(authFault.Fired);
            if (outcome is "ack-rollback" or "ack-lost") Assert.True(database.AcknowledgementFaultFired);
            if (outcome == "pending-spend") Assert.Single(awarded.AccountCurrencyOperation.Where(row => row.Subtract && row.Amount == ulong.MaxValue));
            if (outcome is "auth-rollback" or "ack-rollback" or "admission-replacement" or "admission-before-character" or "auth-route-replacement") Assert.True(session.IsDisconnecting);
            if (outcome == "admission-replacement") Assert.Equal(0ul, actor.Player.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
            if (outcome == "late-wallet")
            {
                Assert.Equal(10ul, actor.Player.Account.CurrencyManager.GetCurrencyAmount(AccountCurrencyType.ProtostarPromissoryNote));
                await auth.Save(context => actor.Player.Account.CurrencyManager.Save(context));
                Assert.Equal(10ul, awarded.AccountCurrency.AsNoTracking().Single(row => row.CurrencyId == 14).Amount);
                Assert.Single(awarded.AccountCurrencyOperation.Where(row => row.Amount == 3));
            }
            if (laterItems.Count != 0)
            {
                ulong deliveredId = awarded.AccountInventory.Single().InventoryId;
                Assert.DoesNotContain(laterItems, item => item.Id == deliveredId);
                await auth.Save(context => { foreach (var item in laterItems) item.Save(context); });
                Assert.Equal(3, awarded.AccountInventory.Count());
            }
        }
        bool liveSession = outcome is "commit" or "auth-lost" or "ack-lost" or "late-account-items" or "late-loot-retry" or "late-wallet" or "pending-spend" or "map-replacement" or "map-readmission" or "event-replacement" or "outbox-race";
        if (liveSession)
        {
            var finish = Assert.Single(queue, packet => InnerOpcode(packet) == GameMessageOpcode.ServerCraftingFinish);
            using var body = new GamePacketReader(new MemoryStream(Plaintext(finish)[2..]));
            Assert.True(body.ReadBit());
            Assert.Equal(5001u, body.ReadUInt(15));
            Assert.Equal(7001u, body.ReadUInt(18));
            Assert.Equal(1, database.Attempts);
        }
        else
        {
            Assert.DoesNotContain(queue, packet => InnerOpcode(packet) == GameMessageOpcode.ServerCraftingFinish);
            Assert.DoesNotContain(queue, packet => InnerOpcode(packet) == GameMessageOpcode.ServerItemAdd);
        }
        if (outcome is "rollback" or "lost") { Assert.True(database.FaultFired); Assert.True(session.IsDisconnecting); }
        if (output == "virtual" && outcome == "commit")
        {
            Assert.True(eventSawCommittedState);
            var progress = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
            Assert.Same(actor.Player, progress.Arguments[0]);
            Assert.Equal(PublicEventObjectiveType.VirtualCollect, progress.Arguments[1]);
            Assert.Equal(265u, progress.Arguments[2]); Assert.Equal(2, progress.Arguments[3]);
        }
        else Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Empty(replacementEventProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        if (auth != null && committed)
        {
            // A fresh admission finishes the same committed source, including a
            // process loss between Character commit and Auth delivery.
            ulong freshEpoch;
            bool consumedBeforeRecovery = output == "account-consumed";
            if (consumedBeforeRecovery)
            {
                using var consumed = auth.CreateContext();
                await consumed.AccountInventory.ExecuteDeleteAsync();
                await consumed.AccountCurrency.Where(row => row.CurrencyId == 14).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 2ul));
            }
            if (output == "account-deleted")
            {
                using var deleted = f.Context(); await deleted.Character.ExecuteDeleteAsync();
                await CraftingAccountRecovery.RecoverAtAdmissionAsync(new AccountModel { Id = 12, WorldSessionEpoch = 1 }, auth, database, 2, f.Source.Tables);
                using var wrongRealm = auth.CreateContext();
                var reserved = Assert.Single(wrongRealm.AccountCraftingReceipt);
                Assert.False(reserved.Delivered); Assert.False(reserved.Completed);
                Assert.Empty(wrongRealm.AccountInventory);
                Assert.Equal(0ul, wrongRealm.AccountCurrency.SingleOrDefault(row => row.CurrencyId == 14)?.Amount ?? 0ul);
            }
            AccountModel fresh;
            if (output == "account-realm") fresh = await AdmitThroughRealmHandler(auth, database, f.Source.Tables);
            else
            {
                using var admitted = auth.CreateContext();
                var admission = admitted.AccountWorldSession.Single(); freshEpoch = admission.Epoch = checked(admission.Epoch + 1ul);
                await admitted.SaveChangesAsync();
                fresh = new AccountModel { Id = 12, WorldSessionEpoch = freshEpoch };
            }
            await CraftingAccountRecovery.RecoverAtAdmissionAsync(fresh, auth, database, 1, f.Source.Tables);
            await CraftingAccountRecovery.RecoverAtAdmissionAsync(fresh, auth, database, 1, f.Source.Tables);
            using var recovered = auth.CreateContext();
            Assert.Equal(walletOutput ? consumedBeforeRecovery ? 2ul : outcome == "late-wallet" ? 10ul : 7ul : 0ul, recovered.AccountCurrency.SingleOrDefault(row => row.CurrencyId == 14)?.Amount ?? 0ul);
            Assert.Equal((itemOutput && !consumedBeforeRecovery ? 1 : 0) + laterItems.Count, recovered.AccountInventory.Count()); Assert.True(recovered.AccountCraftingReceipt.Single().Completed);
            Assert.Equal((itemOutput && !consumedBeforeRecovery ? 1 : 0) + laterItems.Count, fresh.AccountInventory.Count);
            if (cosmicOutput) { Assert.Equal(800ul, fresh.Loyalty.EarnedPoints); Assert.Single(fresh.AccountPendingItem); }
            using var confirmed = f.Context(); Assert.True(confirmed.CharacterCraftingAccountOutbox.Single().Delivered);
            await recovered.AccountInventory.Where(row => row.CreationKey.StartsWith("craft-award:")).ExecuteDeleteAsync();
            await recovered.AccountCurrency.Where(row => row.CurrencyId == 14).ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 2ul));
            await CraftingAccountRecovery.RecoverAtAdmissionAsync(fresh, auth, database, 1, f.Source.Tables);
            Assert.Equal(laterItems.Count, fresh.AccountInventory.Count);
            Assert.DoesNotContain(fresh.AccountInventory, row => row.CreationKey.StartsWith("craft-award:", StringComparison.Ordinal));
            if (walletOutput) Assert.Equal(2ul, fresh.AccountCurrency.Single(row => row.CurrencyId == 14).Amount);
            Assert.Equal((itemOutput ? 1 : 0) + laterItems.Count, recovered.AccountInventoryReceipt.Count()); Assert.Empty(recovered.AccountEntitlement);
        }
        else if (auth != null)
        {
            using var admission = auth.CreateContext();
            var epoch = admission.AccountWorldSession.Single(); epoch.Epoch++; await admission.SaveChangesAsync();
            var fresh = new AccountModel { Id = 12, WorldSessionEpoch = epoch.Epoch };
            await CraftingAccountRecovery.RecoverAtAdmissionAsync(fresh, auth, database, 1, tables: null);
            Assert.Empty(fresh.AccountInventory);
            Assert.Equal(0ul, fresh.AccountCurrency.SingleOrDefault(row => row.CurrencyId == 14)?.Amount ?? 0ul);
            using var cancelled = auth.CreateContext();
            Assert.True(cancelled.AccountCraftingReceipt.Single().Completed); Assert.True(cancelled.AccountCraftingReceipt.Single().Cancelled);
            Assert.Empty(cancelled.AccountInventoryReceipt);
            using var source = f.Context(); Assert.True(source.CharacterCraftingAccountOutbox.Single().Cancelled);
        }
    }

    private static async Task<AccountModel> AdmitThroughRealmHandler(AuthDatabase auth, CharacterDatabase character, IGameTableManager tables)
    {
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var databaseProxy);
        databaseProxy.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) => method.GetGenericArguments()[0] == typeof(AuthDatabase) ? auth : character);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network); var events = new NexusForever.Shared.Game.Events.EventQueue();
        network.SetProperty(nameof(IWorldSession.Events), events);
        AccountModel admitted = null; Exception error = null; events.UnhandledExceptionHandler = value => error = value;
        network.SetMethodHandler(nameof(IWorldSession.Initialise), args =>
        {
            admitted = Assert.IsType<AccountModel>(args[0]);
            Assert.Equal(7ul, admitted.AccountCurrency.Single(row => row.CurrencyId == 14).Amount);
            Assert.Single(admitted.AccountInventory);
            using var source = character.CreateContext(); Assert.True(source.CharacterCraftingAccountOutbox.Single().Delivered);
            return null;
        });
        var hello = new ClientHelloRealm(); Set(hello, nameof(ClientHelloRealm.AccountId), 12u);
        Set(hello, nameof(ClientHelloRealm.AccountString), "crafting-12@example.local");
        Set(hello, nameof(ClientHelloRealm.SessionKey), Convert.FromHexString("00112233445566778899AABBCCDDEEFF"));
        new ClientHelloRealmHandler(databases, realm, tables).HandleMessage(session, hello);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (admitted == null && error == null && !timeout.IsCancellationRequested) { events.Update(.01); await Task.Delay(5); }
        Assert.Null(error); Assert.NotNull(admitted);
        Assert.Single(network.GetInvocations(nameof(IWorldSession.SetEncryptionKey)));
        Assert.Equal(2ul, admitted.WorldSessionEpoch);
        return admitted;
    }

    private static GameMessageOpcode InnerOpcode(ServerGamePacket packet)
    {
        if (packet.Opcode != GameMessageOpcode.ServerRealmEncrypted) return packet.Opcode;
        return (GameMessageOpcode)BitConverter.ToUInt16(Plaintext(packet));
    }
    private static byte[] Plaintext(ServerGamePacket packet)
    { byte[] cipher = packet.Data[4..]; return new PacketCrypt(123ul).Decrypt(cipher, cipher.Length); }
    private static void Set(object target, string property, object value) => target.GetType().GetProperty(property)!.SetValue(target, value);
    private sealed class HeldDatabase(Fixture fixture, string outcome) : CharacterDatabase
    {
        internal bool Armed;
        internal int Attempts;
        internal Action Committed;
        private readonly Fault fault = new(outcome == "lost");
        internal bool FaultFired => fault.Fired;
        private readonly AwardFault acknowledgementFault = new(outcome == "ack-lost", character: true);
        internal bool AcknowledgementFaultFired => acknowledgementFault.Fired;
        public override CharacterContext CreateContext() => outcome is "ack-rollback" or "ack-lost"
            ? fixture.Context(acknowledgementFault) : base.CreateContext();
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task SaveAtomic(ulong character, Action<CharacterContext> save)
        {
            if (!Armed) { await base.SaveAtomic(character, save); return; }
            Attempts++; Entered.TrySetResult(); await Release.Task;
            await CharacterAtomicSave.Execute(() => outcome is "rollback" or "lost" ? fixture.Context(fault) : fixture.Context(), character, save);
            Committed?.Invoke();
        }
    }
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new Retry(dependencies);
    }
    private sealed class Retry(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception error) => error is TimeoutException;
    }
    private sealed class AwardDatabase(AwardFault fault, bool retrying = false) : AuthDatabase
    {
        public override AuthContext CreateContext()
        {
            using var configured = base.CreateContext();
            var options = new DbContextOptionsBuilder<AuthContext>();
            options.UseConfiguration(new DatabaseConnectionString { Provider = configured.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = configured.Database.GetDbConnection().ConnectionString });
            if (retrying) options.ReplaceService<IExecutionStrategyFactory, RetryFactory>();
            if (fault != null) options.AddInterceptors(fault);
            return new(options.Options);
        }
    }
    private sealed class AwardFault(bool after, bool character = false, bool allocator = false) : DbTransactionInterceptor
    {
        internal bool Fired;
        internal bool Armed = true;
        private bool Owns(DbContext context) => Armed && !Fired && (character
            ? context is CharacterContext source && source.ChangeTracker.Entries<NexusForever.Database.Character.Model.CharacterCraftingAccountOutboxModel>().Any(entry => entry.Entity.Delivered)
            : context is AuthContext target && (allocator
                ? !target.AccountCraftingReceipt.Local.Any() && target.AccountPendingItemDelivery.Local.Any(receipt => receipt.PendingItemId == 0)
                : target.AccountCraftingReceipt.Local.Any(receipt => receipt.Delivered)));
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Owns(data.Context)) { Fired = true; throw new IOException("Owned crafting award commit rejection."); }
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (after && Owns(data.Context)) { Fired = true; return Task.FromException(new IOException("Owned crafting award acknowledgement loss.")); }
            return Task.CompletedTask;
        }
    }
    private sealed class Fault(bool after) : DbTransactionInterceptor
    {
        internal bool Fired { get; private set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (after) return new(result);
            Fired = true; throw new IOException("Owned craft commit rejection.");
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (!after) return Task.CompletedTask;
            Fired = true; return Task.FromException(new IOException("Owned craft acknowledgement loss."));
        }
    }
}
