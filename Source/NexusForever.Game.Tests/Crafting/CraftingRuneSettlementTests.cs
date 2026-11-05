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
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Crafting;

public sealed class CraftingRuneSettlementTests
{
    [Theory]
    [InlineData("install", "commit")]
    [InlineData("install", "rollback")]
    [InlineData("install", "lost")]
    [InlineData("install", "replacement")]
    [InlineData("install", "publication-replacement")]
    [InlineData("recover", "commit")]
    [InlineData("recover", "rollback")]
    [InlineData("recover", "full")]
    [InlineData("recover", "metadata-replacement")]
    [InlineData("clear", "commit")]
    [InlineData("clear", "rollback")]
    [InlineData("add", "commit")]
    [InlineData("add", "rollback")]
    [InlineData("reroll", "commit")]
    [InlineData("reroll", "rollback")]
    public async Task PublicRuneChangeCommitsTheSocketAndInventoryBeforeSuccess(string operation, string outcome)
    {
        using var f = new Fixture(null);
        f.Seed();
        var database = new CharacterDatabase();
        string before = operation is "clear" or "recover" ? "13:900" : "13:0";
        using (var seed = f.Context())
        {
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
            seed.Character.Single().AccountId = 12;
            seed.Item.Add(new() { Id = 904, OwnerId = 42, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                StackCount = 1, RuneSlots = before });
            if (operation == "install")
                seed.Item.Add(new() { Id = 905, OwnerId = 42, ItemId = 900, Location = (ushort)InventoryLocation.Inventory,
                    BagIndex = 1, StackCount = 1 });
            if (outcome == "full")
                for (uint index = 1; index < 20; index++)
                    seed.Item.Add(new() { Id = 1000 + index, OwnerId = 42, ItemId = 7001,
                        Location = (ushort)InventoryLocation.Inventory, BagIndex = index, StackCount = 1 });
            await seed.SaveChangesAsync();
        }
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        var gearEntry = new Item2Entry { Id = 7001, MaxStackCount = 1 };
        var runeEntry = new Item2Entry { Id = 900, Item2CategoryId = 175, Item2TypeId = 356, MaxStackCount = 1 };
        tables.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(gearEntry, runeEntry));
        tables.SetProperty(nameof(IGameTableManager.Item2Category), PlayerBindPointTests.Table(new Item2CategoryEntry { Id = 175, TradeSkillId = 22 }));
        IItemInfo Info(Item2Entry entry)
        {
            var info = RecordingDispatchProxy<IItemInfo>.Create(out var proxy);
            proxy.SetProperty(nameof(IItemInfo.Id), entry.Id);
            proxy.SetProperty(nameof(IItemInfo.Entry), entry);
            return info;
        }
        var gearInfo = Info(gearEntry); var runeInfo = Info(runeEntry);
        Action onRuneInfo = null;
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        itemManager.SetProperty(nameof(IItemManager.NextItemId), 906ul);
        itemManager.SetMethodHandler(nameof(IItemManager.GetItemInfo), args =>
        {
            if ((uint)args[0] == 7001) return gearInfo;
            onRuneInfo?.Invoke(); return runeInfo;
        });
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        network.SetProperty(nameof(IWorldSession.Events), events);
        var account = (RecordingDispatchProxy<IAccount>)(object)f.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u); account.SetProperty(nameof(IAccount.Session), session);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var databaseProxy);
        databaseProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fault = new CommitFault(outcome == "lost");
        var bank = new RealmBankManager(databases, items, async command =>
        {
            entered.TrySetResult(); await release.Task;
            if (outcome is "lost" or "rollback")
                typeof(RealmBankManager.BankSaveCommand).GetField("contexts", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(command, (Func<CharacterContext>)(() => f.Context(fault)));
            await command.ExecuteAsync();
        });
        var model = Assert.Single(await database.GetCharacters(12));
        using var actor = new Actor(f, actorModel: model, actorSession: session, actorEvents: events, characterDatabase: database,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, f.Source.Tables, bank));
        network.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var wallet = new CurrencyManager(f.Source.Tables); wallet.Initialise(actor.Player, model);
        actor.Set(nameof(Player.CurrencyManager), wallet);
        await actor.Load();
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory);
        foreach (var item in model.Item)
            inventory.LoadItem(new NexusForever.Game.Entity.Item(item, items, f.Source.Tables), InventoryLocation.Inventory, item.BagIndex);
        network.Invocations.Clear();
        var replacementInventory = RecordingDispatchProxy<IInventory>.Create(out var foreignInventory);
        if (outcome == "metadata-replacement")
            onRuneInfo = () =>
            {
                var replacement = RecordingDispatchProxy<IPlayer>.Create(out var foreignPlayer);
                foreignPlayer.SetProperty(nameof(IPlayer.Inventory), replacementInventory);
                network.SetProperty(nameof(IWorldSession.Player), replacement);
            };
        network.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (outcome == "publication-replacement" && args[0] is ServerTradeskillSigilResult { TradeskillSigilResult: TradeskillResult.Success })
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            return null;
        });

        try
        {
            switch (operation)
            {
                case "install":
                    var install = new ClientCraftingRuneInstall(); Set(install, "ItemGuid", 904ul); Set(install, "RuneSlotItem2Id", new uint[] { 900 });
                    new ClientCraftingRuneInstallHandler(NullLogger<ClientCraftingRuneInstallHandler>.Instance, f.Source.Tables).HandleMessage(session, install);
                    break;
                case "recover": case "clear":
                    var clear = new ClientCraftingRuneSlotClear(); Set(clear, "ItemGuid", 904ul); Set(clear, "RecoverRune", operation == "recover");
                    new ClientCraftingRuneSlotClearHandler(NullLogger<ClientCraftingRuneSlotClearHandler>.Instance).HandleMessage(session, clear);
                    break;
                case "add":
                    var add = new ClientCraftingRuneSlotAdd(); Set(add, "ItemGuid", 904ul); Set(add, "Type", RuneType.Air);
                    new ClientCraftingRuneSlotAddHandler(NullLogger<ClientCraftingRuneSlotAddHandler>.Instance).HandleMessage(session, add);
                    break;
                case "reroll":
                    var reroll = new ClientCraftingRuneSlotReroll(); Set(reroll, "ItemGuid", 904ul); Set(reroll, "Type", (RuneType)1);
                    new ClientCraftingRuneSlotRerollHandler(NullLogger<ClientCraftingRuneSlotRerollHandler>.Instance).HandleMessage(session, reroll);
                    break;
            }
            Assert.DoesNotContain(Packets().OfType<ServerTradeskillSigilResult>(), p => p.TradeskillSigilResult == TradeskillResult.Success);
            Assert.Empty(Packets().OfType<ServerItemAdd>());
            if (outcome is "full" or "metadata-replacement")
            {
                await actor.Drain();
                Assert.False(entered.Task.IsCompleted);
                Assert.Equal(900u, inventory.GetItem(904ul).RuneSlots[0].RuneItem2Id);
                using var unchanged = f.Context();
                Assert.Equal(before, unchanged.Item.Single(row => row.Id == 904).RuneSlots);
                Assert.DoesNotContain(unchanged.Item, row => row.ItemId == 900);
                if (outcome == "full")
                    Assert.Equal(TradeskillResult.UnknownError, Assert.Single(Packets().OfType<ServerTradeskillSigilResult>()).TradeskillSigilResult);
                else
                    Assert.Empty(foreignInventory.GetInvocations(nameof(IInventory.ItemCreate)));
                return;
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!entered.Task.IsCompleted) { events.Update(.01); await Task.Delay(5, timeout.Token); }
            using var pending = f.Context();
            Assert.Equal(before, pending.Item.Single(row => row.Id == 904).RuneSlots);
            Assert.Equal(operation == "install" ? 2 : 1, pending.Item.Count());
            if (outcome == "replacement")
                network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        }
        finally { release.TrySetResult(); }
        await actor.Drain();
        using var stored = f.Context();
        bool committed = outcome != "rollback";
        string expected = operation switch { "install" => "13:900", "add" => "13:0,7:0", "reroll" => "7:0", _ => "13:0" };
        Assert.Equal(committed ? expected : before, stored.Item.Single(row => row.Id == 904).RuneSlots);
        Assert.Equal(operation == "install" && !committed || operation == "recover" && committed ? 2 : 1, stored.Item.Count());
        Assert.Equal(committed && outcome != "replacement" ? 1 : 0,
            Packets().OfType<ServerTradeskillSigilResult>().Count(p => p.TradeskillSigilResult == TradeskillResult.Success));
        if (outcome != "replacement")
        {
            Assert.Equal(committed ? expected : before, ItemRuneSlotsCodec.Serialize(inventory.GetItem(904ul).RuneSlots));
            Assert.Equal(operation == "install" ? committed ? 0u : 1u : operation == "recover" && committed ? 1u : 0u, inventory.GetItemCount(900));
            Assert.Equal(outcome == "publication-replacement", inventory.RequiresAuthoritativeReload);
        }
        if (outcome is "replacement" or "publication-replacement") Assert.Empty(Packets().OfType<ServerItemAdd>());
        if (outcome is "lost" or "rollback") Assert.True(fault.Fired);
        IEnumerable<object> Packets() => network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]);
    }

    private static void Set(object target, string property, object value) => target.GetType().GetProperty(property)!.SetValue(target, value);

    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int fired;
        internal bool Fired => Volatile.Read(ref fired) != 0;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!after && Interlocked.CompareExchange(ref fired, 1, 0) == 0) throw new IOException("Owned rune commit rejection.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after && Interlocked.CompareExchange(ref fired, 1, 0) == 0
                ? Task.FromException(new IOException("Owned rune commit acknowledgement loss.")) : Task.CompletedTask;
    }
}
