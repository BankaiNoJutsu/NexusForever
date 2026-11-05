using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Item;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Vendor;

public sealed class VendorRepairSettlementTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public VendorRepairSettlementTests() { }
    internal VendorRepairSettlementTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("lost")]
    [InlineData("replacement")]
    [InlineData("single")]
    [InlineData("all")]
    [InlineData("changed")]
    [InlineData("retry")]
    [InlineData("publication-replacement")]
    public async Task RepairHandlerSettlesDurabilityAndCreditsBeforePublishingTheirUpdates(string outcome)
    {
        using var f = new Fixture(options);
        f.Seed();
        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            database.Initialise(new DatabaseConnectionString
            {
                Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString
            });
            seed.Character.Single().AccountId = 12;
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            seed.Item.Add(new() { Id = 904, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                StackCount = 1, Durability = .5f });
            if (outcome is "single" or "all")
                seed.Item.Add(new() { Id = 905, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                    BagIndex = 1, StackCount = 1, Durability = .9f });
            await seed.SaveChangesAsync();
        }
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
        tables.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(new GameFormulaEntry { Id = 0x22f, Datafloat03 = .5f }));
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfo);
        itemInfo.SetProperty(nameof(IItemInfo.Id), 7001u);
        itemInfo.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 1 });
        itemInfo.SetMethodReturn(nameof(IItemInfo.IsEquippable), true);
        itemInfo.SetMethodHandler(nameof(IItemInfo.GetVendorBuyCurrency), args => (byte)args[0] == 0 ? CurrencyType.Credits : CurrencyType.None);
        itemInfo.SetMethodHandler(nameof(IItemInfo.GetVendorBuyAmount), args => (byte)args[0] == 0 ? 100u : 0u);
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        itemManager.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        network.SetProperty(nameof(IWorldSession.Events), events);
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u);
        account.SetProperty(nameof(IAccount.Session), session);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var databaseProxy);
        databaseProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fault = new CommitFault(outcome == "lost");
        var bank = new RealmBankManager(databases, items, async command =>
        {
            entered.TrySetResult();
            await release.Task;
            if (outcome is "lost" or "rollback")
                typeof(RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(command, (Func<CharacterContext>)(() => f.Context(fault)));
            await command.ExecuteAsync();
        });
        var model = Assert.Single(await database.GetCharacters(12));
        using var actor = new Actor(f, actorModel: model, actorSession: session, actorEvents: events, characterDatabase: database,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, f.Source.Tables, bank));
        network.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var credits = new CurrencyManager(f.Source.Tables);
        credits.Initialise(actor.Player, model);
        actor.Set(nameof(Player.CurrencyManager), credits);
        await actor.Load();
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory);
        foreach (var item in model.Item)
            inventory.LoadItem(new NexusForever.Game.Entity.Item(item, items, f.Source.Tables), InventoryLocation.Inventory, item.BagIndex);
        var vendor = RecordingDispatchProxy<IVendorInfo>.Create(out _);
        typeof(Player).GetField("selectedVendorInfo", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, vendor);
        network.Invocations.Clear();
        var accepted = new List<object>();
        int durabilityAttempts = 0;
        network.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemDurability update)
            {
                using var committed = f.Context();
                Assert.Equal(1f, committed.Item.Single(item => item.Id == update.ItemGuid).Durability);
                Assert.Equal(outcome == "all" ? 70ul : 75ul, committed.CharacterCurrency.Single().Amount);
                if (++durabilityAttempts == 1 && outcome == "retry")
                    throw new IOException("Owned first durability enqueue rejection.");
            }
            accepted.Add(args[0]);
            if (outcome == "publication-replacement" && args[0] is ServerCombatReward)
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            return null;
        });
        var handler = new ClientRepairItemVendorHandler(NullLogger<ClientRepairItemVendorHandler>.Instance, f.Source.Tables);
        var request = new ClientRepairItemVendor();
        if (outcome == "single")
            typeof(ClientRepairItemVendor).GetProperty(nameof(ClientRepairItemVendor.ItemIdentity))!.SetValue(request, 904ul);
        if (outcome == "changed")
            actor.OnAuth = () => inventory.GetItem(904ul).Durability = .25f;

        try
        {
            handler.HandleMessage(session, request);
            Assert.Empty(Packets().OfType<ServerCombatReward>());
            Assert.Empty(Packets().OfType<ServerItemDurability>());
            if (outcome == "changed")
            {
                await actor.Drain();
                Assert.False(entered.Task.IsCompleted);
                Assert.Equal(.25f, inventory.GetItem(904ul).Durability);
                Assert.Equal(100ul, Assert.Single(credits).Amount);
                actor.OnAuth = null;
                await actor.Player.SaveDirect();
                using var later = f.Context();
                Assert.Equal(.25f, later.Item.Single().Durability);
                Assert.Equal(100ul, later.CharacterCurrency.Single().Amount);
                return;
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!entered.Task.IsCompleted)
            {
                events.Update(.01);
                await Task.Delay(5, timeout.Token);
            }
            Assert.Equal(100ul, Assert.Single(credits).Amount);
            Assert.Empty(Packets().OfType<ServerItemDurability>());
            using var pending = f.Context();
            Assert.Equal(.5f, pending.Item.Single(item => item.Id == 904ul).Durability);
            Assert.Equal(100ul, pending.CharacterCurrency.Single().Amount);
            if (outcome == "replacement")
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        }
        finally { release.TrySetResult(); }

        await actor.Drain();
        using var stored = f.Context();
        if (outcome is "lost" or "rollback")
            Assert.True(fault.Fired);
        bool committed = outcome != "rollback";
        ulong expectedCredits = outcome == "all" ? 70ul : committed ? 75ul : 100ul;
        Assert.Equal(committed ? 1f : .5f, stored.Item.Single(item => item.Id == 904ul).Durability);
        if (outcome is "single" or "all")
            Assert.Equal(outcome == "all" ? 1f : .9f, stored.Item.Single(item => item.Id == 905ul).Durability);
        Assert.Equal(expectedCredits, stored.CharacterCurrency.Single().Amount);
        if (outcome == "replacement")
            Assert.Empty(Packets().OfType<ServerCombatReward>());
        else
        {
            Assert.Equal(committed ? 1f : .5f, inventory.GetItem(904ul).Durability);
            Assert.Equal(expectedCredits, Assert.Single(credits).Amount);
            Assert.Equal(committed ? 1 : 0, Packets().OfType<ServerCombatReward>().Count());
            Assert.Equal(outcome == "publication-replacement", inventory.RequiresAuthoritativeReload);
        }

        var updates = accepted.OfType<ServerItemDurability>().ToArray();
        if (!committed || outcome is "replacement" or "publication-replacement")
            Assert.Empty(updates);
        else
        {
            Assert.Equal(outcome == "all" ? new ulong[] { 904, 905 } : [904ul], updates.Select(update => update.ItemGuid).Order());
            Assert.All(updates, update => Assert.Equal(1f, update.Durability));
            Assert.Equal(outcome is "all" or "retry" ? 2 : 1, durabilityAttempts);
            Assert.IsType<ServerCombatReward>(accepted.First());
        }

        IEnumerable<object> Packets() => network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]);
    }

    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        internal bool Fired => Volatile.Read(ref fired) != 0;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.CompareExchange(ref fired, 1, 0) == 0)
                throw new IOException("Owned repair commit rejection.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.CompareExchange(ref fired, 1, 0) == 0
                ? Task.FromException(new IOException("Owned repair commit acknowledgement loss.")) : Task.CompletedTask;
    }
}
