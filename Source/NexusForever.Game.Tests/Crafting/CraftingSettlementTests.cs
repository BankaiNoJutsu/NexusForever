using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.WorldServer.Crafting;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Crafting;

public sealed class CraftingSettlementTests
{
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
    public async Task CraftingPublishesItsFrozenInventoryAndResultAfterTheCharacterSave(string request, string output, string outcome)
    {
        using var f = new Fixture(null);
        f.Seed();
        var database = new HeldDatabase(f, outcome);
        using (var seed = f.Context())
        {
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
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
        var schematic = new TradeskillSchematic2Entry { Id = 5001, Item2IdOutput = output == "loot" ? 0u : 7001u,
            OutputCount = 1, LootId = output == "loot" ? 9001u : 0u, Item2IdMaterial00 = 8001, MaterialCost00 = 2 };
        tables.SetProperty(nameof(IGameTableManager.TradeskillSchematic2), PlayerBindPointTests.Table(schematic));
        tables.SetProperty(nameof(IGameTableManager.TradeskillMaterial), PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 55, Item2IdStatRevolution = 8001 }));
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
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
        var model = Assert.Single(await database.GetCharacters(12));
        using var actor = new Actor(f, actorModel: model, actorSession: session, actorEvents: session.Events,
            characterDatabase: database, inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, f.Source.Tables, null));
        session.Player = actor.Player;
        actor.Set(nameof(Player.Guid), 77u);
        var wallet = new CurrencyManager(f.Source.Tables); wallet.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), wallet);
        actor.Set(nameof(Player.SupplySatchelManager), new SupplySatchelManager(actor.Player, model, f.Source.Tables));
        await actor.Load();
        var inventory = Assert.IsType<Inventory>(actor.Player.Inventory);
        foreach (var row in model.Item)
            inventory.LoadItem(new NexusForever.Game.Entity.Item(row, items, f.Source.Tables), InventoryLocation.Inventory, row.BagIndex);
        var modifiers = RecordingDispatchProxy<ICraftingModifierSessionStore>.Create(out var modifier);
        modifier.SetMethodHandler(nameof(ICraftingModifierSessionStore.TryBuildModifierItemCounts), args =>
        { args[3] = new Dictionary<uint, uint>(); args[4] = string.Empty; return true; });
        var delivery = new GlobalLootManager(null, itemManager: items, gameTableManager: f.Source.Tables);
        var loot = RecordingDispatchProxy<IGlobalLootManager>.Create(out var lp);
        lp.SetMethodHandler(nameof(IGlobalLootManager.TryGenerateLoot), args =>
        { args[3] = new GeneratedLootItem[] { new(LootItemType.StaticItem, 7001, 1), new(LootItemType.Cash, 1, 7) }; args[4] = string.Empty; return true; });
        lp.SetMethodHandler(nameof(IGlobalLootManager.CanDeliverGeneratedLoot), args =>
        { bool allowed = delivery.CanDeliverGeneratedLoot((IPlayer)args[0], (IEnumerable<GeneratedLootItem>)args[1], out string reason); args[2] = reason; return allowed; });
        lp.SetMethodHandler(nameof(IGlobalLootManager.GiveGeneratedLoot), args =>
        { delivery.GiveGeneratedLoot((IPlayer)args[0], (IEnumerable<GeneratedLootItem>)args[1], (uint)args[2], (bool)args[3], (uint)args[4]); return null; });
        var queue = (ConcurrentQueue<ServerGamePacket>)typeof(GameSession).GetField("outgoingPackets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
        queue.Clear(); database.Armed = true;
        if (outcome == "wallet-replacement")
            database.Committed = () => actor.Set(nameof(Player.CurrencyManager), RecordingDispatchProxy<ICurrencyManager>.Create(out _));
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
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!database.Entered.Task.IsCompleted) { session.Events.Update(.01); await Task.Delay(5, timeout.Token); }
            using var pending = f.Context();
            Assert.DoesNotContain(pending.Item, row => row.ItemId == 7001);
            if (output == "satchel") Assert.Equal((ushort)2, pending.CharacterTradeskillMaterial.Single().Amount);
            else Assert.Equal(2u, pending.Item.Single().StackCount);
            if (outcome == "replacement") session.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
        }
        finally { database.Release.TrySetResult(); }
        await actor.Drain(allowErrors: outcome is "rollback" or "lost");
        using var stored = f.Context();
        bool committed = outcome != "rollback";
        Assert.Equal(committed ? 1 : 0, stored.Item.Count(row => row.ItemId == 7001));
        if (output == "satchel") Assert.Equal(committed ? (ushort)0 : (ushort)2, stored.CharacterTradeskillMaterial.Single().Amount);
        else Assert.Equal(committed ? 0 : 1, stored.Item.Count(row => row.ItemId == 8001));
        if (output == "loot") Assert.Equal(committed ? 7ul : 0ul, stored.CharacterCurrency.SingleOrDefault()?.Amount ?? 0ul);
        if (outcome == "commit")
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
