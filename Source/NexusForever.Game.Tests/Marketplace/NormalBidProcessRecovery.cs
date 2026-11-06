using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

// Explicit headless child entrypoint. A parent pins/kills the original process
// at a real transaction/publication boundary, then starts independent readers.
public static class NormalBidProcessRecovery
{
    private static readonly JsonSerializerOptions json = new() { WriteIndented = true, IncludeFields = true };
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static void Write(string path, object value)
    {
        if (File.Exists(path)) throw new IOException("Retain prior evidence; choose a new case directory.");
        File.WriteAllText(path + ".pending", JsonSerializer.Serialize(value, json));
        File.Move(path + ".pending", path);
    }

    public static async Task RunAsync(string mode, string caseDirectory, string window)
    {
        Assert.Contains(mode, new[] { "produce", "recover", "retry", "verify", "audit" });
        Assert.Contains(window, new[] { "control", "preflight-before", "financial-before", "financial-after", "reconcile-before", "reconcile-after", "before-result" });
        caseDirectory = Path.GetFullPath(caseDirectory); Directory.CreateDirectory(caseDirectory);
        bool produce = mode == "produce";
        var pause = new CommitPause(caseDirectory, window);
        var options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(caseDirectory, "character.sqlite")};Pooling=False;Default Timeout=10")
            .AddInterceptors(pause).Options;
        if (produce) Assert.False(File.Exists(Path.Combine(caseDirectory, "character.sqlite")));
        else Assert.True(File.Exists(Path.Combine(caseDirectory, "character.sqlite")));
        using var f = new Fixture(options, seedCharacter: produce);
        Write(Path.Combine(caseDirectory, mode + "-identity.json"), new
        {
            Mode = mode, Window = window, ProcessId = Environment.ProcessId,
            StartedUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime(), Exe = Environment.ProcessPath,
            OwnedTemporaryDirectories = new[] { (string)f.Source.GetType().GetField("directory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Source) }
        });
        if (produce)
        {
            f.Seed();
            using var seed = f.Context();
            seed.Character.Single().AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Process seller", OriginalName = "Process seller" });
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(new() { Id = 1000, ItemId = 7001, StackCount = 1, Location = (ushort)InventoryLocation.None });
            seed.MarketplaceAuction.Add(new() { Id = 1, OwnerCharacterId = 100, ItemId = 1000, Item2Id = 7001, Quantity = 1,
                MinimumBid = 1, ExpirationTime = (ulong)DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds() });
            await seed.SaveChangesAsync();
        }
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        account.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
        messages.SetProperty(nameof(IWorldSession.Events), events); account.SetProperty(nameof(IAccount.Session), session);
        var database = new ProbeDatabase(() => f.Context());
        database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(caseDirectory, "character.sqlite")};Pooling=False;Default Timeout=10" });
        var model = Assert.Single(await database.GetCharacters(12));
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfo);
        itemInfo.SetProperty(nameof(IItemInfo.Id), 7001u);
        itemInfo.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 1, MaxCharges = 1 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager); itemManager.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        using var actor = new RewardNormalSaveSnapshotTests.Actor(f, actorModel: model, actorSession: session,
            actorEvents: events, characterDatabase: database, inventoryFactory: (owner, loaded) => EmptyInventory(owner, loaded, items, f.Source.Tables));
        messages.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
        int replies = 0; GenericError? resultCode = null;
        messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAuctionBidResult packet)
            {
                if (produce && window == "before-result") pause.Publication();
                Assert.Equal(7001u, packet.Auction.Item2Id); Assert.Equal(1ul, packet.Auction.AuctionId);
                replies++; resultCode = packet.Result;
            }
            return null;
        });
        await actor.Load();
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] is ulong id && id == 42 ? actor.Player : null);
        var cleanup = new CleanupManager();
        var marketplace = new GlobalMarketplaceManager(databases, f.Source.Tables, null, players, items, cleanup,
            async command =>
            {
                pause.Phase = "financial";
                var result = await command.ExecuteAsync();
                pause.Phase = "reconcile";
                return result;
            });
        marketplace.Initialise();
        string before = Snapshot(f);
        if (!produce)
        {
            bool committed = window is not ("preflight-before" or "financial-before");
            if (mode is "verify" or "audit") committed = true;
            Assert.Equal(0, replies); Assert.Empty(actor.Errors);
            if (mode == "recover")
            {
                AssertState(f, committed, window == "preflight-before" ? 100ul : committed ? 110ul : 120ul,
                    window == "preflight-before" ? 1u : 2u);
                using (var read = f.Context()) Assert.Equal(!committed ? 0 : window is "financial-after" or "reconcile-before" ? 1 : 2, read.MarketplaceBidReceipt.Count());
                Assert.Equal(window == "preflight-before" ? 100ul : committed ? 110ul : 120ul,
                    currency.Single(row => row.Id == CurrencyType.Credits).Amount);
                Write(Path.Combine(caseDirectory, "recover-result.json"), new { Mode = mode, Window = window, Snapshot = before, SnapshotSha256 = Hash(before), ActorCredits = currency.Single(row => row.Id == CurrencyType.Credits).Amount, PriorResultPublished = replies != 0 });
                return;
            }
            if (mode is "verify" or "audit")
            {
                using var previous = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(caseDirectory, "retry-result.json")));
                Assert.Equal(previous.RootElement.GetProperty("AfterSha256").GetString(), Hash(before));
            }
        }
        else
        {
            actor.Quest.Single().Progress = 2; currency.CurrencyAddAmount(CurrencyType.Credits, 20);
            pause.Phase = "preflight"; pause.Armed = true;
        }
        var services = new ServiceCollection().AddSingleton<IGlobalMarketplaceManager>(marketplace).AddSingleton(items)
            .AddSingleton<ILogger<ClientAuctionBuyOrderSubmitHandler>>(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance)
            .AddScoped<MarketplaceRequestHelper>().AddScoped<ClientAuctionBuyOrderSubmitHandler>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        bool scopeDisposed = false;
        events.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
        {
            using var bytes = new MemoryStream();
            using (var writer = new GamePacketWriter(bytes)) { writer.Write(7001u, 18u); writer.Write(1ul); writer.Write(10ul); writer.FlushBits(); }
            using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
            var packet = new ClientAuctionBuyOrderSubmit(); packet.Read(reader);
            using (var scope = provider.CreateScope()) scope.ServiceProvider.GetRequiredService<ClientAuctionBuyOrderSubmitHandler>().HandleMessage(session, packet);
            scopeDisposed = true;
        }));
        var deadline = Stopwatch.StartNew();
        do
        {
            events.Update(.01); Assert.Empty(actor.Errors);
            Assert.True(deadline.Elapsed < TimeSpan.FromSeconds(30), "Public bid did not reach its expected process boundary.");
            await Task.Delay(5);
        } while (replies == 0 || events.PendingEvents || actor.Player.IsNormalSaveInProgress);
        Assert.True(scopeDisposed); Assert.Equal(1, replies); Assert.False(cleanup.IsAccountLocked(actor.Player.Account));
        bool alreadyBid = !produce && (mode is "verify" or "audit" || window is not ("preflight-before" or "financial-before"));
        Assert.Equal(alreadyBid ? GenericError.Params : GenericError.Ok, resultCode);
        ulong expectedCredits = window == "preflight-before" ? 90ul : 110ul;
        Assert.Equal(expectedCredits, currency.Single(row => row.Id == CurrencyType.Credits).Amount);
        AssertState(f, true, expectedCredits, window == "preflight-before" ? 1u : 2u);
        string after = Snapshot(f);
        if (alreadyBid) Assert.Equal(before, after);
        Write(Path.Combine(caseDirectory, mode == "produce" ? "producer-complete.json" : mode + "-result.json"), new
        {
            Mode = mode, Window = window, Replies = replies, Result = resultCode, ScopeDisposed = scopeDisposed,
            BeforeSha256 = Hash(before), After = after, AfterSha256 = Hash(after), ActorCredits = currency.Single(row => row.Id == CurrencyType.Credits).Amount, AlreadyBid = alreadyBid
        });
    }

    private static void AssertState(Fixture f, bool committed, ulong credits, uint progress)
    {
        using var read = f.Context();
        Assert.Equal(credits, read.CharacterCurrency.Single(row => row.Id == 42 && row.CurrencyId == 1).Amount);
        Assert.Equal(progress, read.CharacterQuestObjective.Single(row => row.Id == 42).Progress);
        var listing = Assert.Single(read.MarketplaceAuction); Assert.Equal(committed ? 10ul : 0ul, listing.CurrentBid);
        Assert.Equal(committed ? 42ul : 0ul, listing.TopBidderCharacterId);
        var item = Assert.Single(read.Item); Assert.Equal(1000ul, item.Id); Assert.Null(item.OwnerId);
        Assert.Equal((ushort)InventoryLocation.None, item.Location); Assert.Equal(1u, item.StackCount);
        Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
    }

    private static string Snapshot(Fixture f)
    {
        using var read = f.Context(); using var unit = read.Database.BeginTransaction();
        return JsonSerializer.Serialize(new
        {
            Listing = GlobalMarketplaceManager.AuctionRow.Capture(Assert.Single(read.MarketplaceAuction.AsNoTracking())),
            Escrow = GlobalMarketplaceManager.BidItem.Capture(Assert.Single(read.Item.AsNoTracking())),
            Wallets = read.CharacterCurrency.AsNoTracking().AsEnumerable().OrderBy(row => row.Id).ThenBy(row => row.CurrencyId).Select(row => new { row.Id, row.CurrencyId, row.Amount }).ToArray(),
            Quests = read.CharacterQuest.AsNoTracking().OrderBy(row => row.QuestId).Select(row => new { row.Id, row.QuestId, row.State, row.Flags, row.Timer }).ToArray(),
            Objectives = read.CharacterQuestObjective.AsNoTracking().OrderBy(row => row.Index).Select(row => new { row.Id, row.QuestId, row.Index, row.Progress, row.Timer }).ToArray(),
            Receipts = read.MarketplaceBidReceipt.AsNoTracking().OrderBy(row => row.Id).AsEnumerable().Select(row => new { row.Id, PayloadSha256 = Hash(row.Payload), row.CreatedUtc }).ToArray(),
            Mail = read.CharacterMail.Count(), Links = read.CharacterMailAttachment.Count()
        }, json);
    }

    private static Inventory EmptyInventory(Player owner, CharacterModel model, IItemManager items, IGameTableManager tables)
    {
        Assert.Empty(model.Item);
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Set(string field, object value) => typeof(Inventory).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Set("characterId", owner.CharacterId); Set("player", owner); Set("itemManager", items); Set("gameTableManager", tables);
        Set("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = new Bag(InventoryLocation.Inventory, 20, items) });
        Set("deletedItems", new List<IItem>()); return inventory;
    }

    private sealed class ProbeDatabase(Func<CharacterContext> contexts) : CharacterDatabase
    {
        public override CharacterContext CreateContext() => contexts();
    }

    private sealed class CommitPause(string directory, string window) : DbTransactionInterceptor
    {
        internal bool Armed; internal string Phase;
        private int fired;
        private void Pause(CharacterContext context, bool after)
        {
            if (!Armed || window != Phase + (after ? "-after" : "-before") || Interlocked.CompareExchange(ref fired, 1, 0) != 0) return;
            Write(Path.Combine(directory, "gate.json"), new
            {
                Window = window, Phase, AfterCommit = after, ProcessId = Environment.ProcessId,
                Receipts = context.ChangeTracker.Entries<MarketplaceBidReceiptModel>().Select(row => new { row.Entity.Id, PayloadSha256 = Hash(row.Entity.Payload) }).ToArray(),
                Wallets = context.ChangeTracker.Entries<CharacterCurrencyModel>().Select(row => new { row.Entity.Id, row.Entity.Amount }).ToArray()
            });
            Thread.Sleep(Timeout.Infinite);
        }
        internal void Publication()
        {
            Write(Path.Combine(directory, "gate.json"), new { Window = window, Phase = "publication", ProcessId = Environment.ProcessId, ResultAccepted = false });
            Thread.Sleep(Timeout.Infinite);
        }
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData data, InterceptionResult result)
        { Pause((CharacterContext)data.Context, false); return result; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
            => new(TransactionCommitting(transaction, data, result));
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData data) => Pause((CharacterContext)data.Context, true);
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        { Pause((CharacterContext)data.Context, true); return Task.CompletedTask; }
    }
}
