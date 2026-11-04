using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Message.Handler;
using NexusForever.Network.Message.Model;
using NexusForever.Network.World.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.WorldServer;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Quest;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class Q5597RewardPersistenceTests
{
    [Fact]
    public async Task Q5597LoopbackTurnInCleanupAndFreshSessionPublishSavedRewardsOnce()
    {
        const uint accountId = 12;
        using var f = new Fixture(null);
        using var authConnection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = $"q5597-{Guid.NewGuid():N}", Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared, Pooling = false
        }.ConnectionString);
        await authConnection.OpenAsync();
        var auth = new AuthDatabase();
        auth.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = authConnection.ConnectionString });
        using (var context = auth.CreateContext()) await context.Database.EnsureCreatedAsync();

        var database = new CharacterDatabase();
        using (var seed = f.Context())
        {
            database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString });
            seed.Character.Single().AccountId = accountId;
            seed.CharacterQuest.Add(new() { Id = 42, QuestId = QuestId, State = (byte)QuestState.Achieved });
            await seed.SaveChangesAsync();
        }
        ConfigureQuest(f, reclaim: false);
        var items = CreateItems();
        var messages = new MessageManager(NullLogger<MessageManager>.Instance);
        messages.RegisterNetworkManagerMessagesAndHandlers();
        messages.RegisterNetworkManagerWorldMessages();
        messages.RegisterMessageHandler(typeof(ClientQuestCompleteHandler));
        using var services = new ServiceCollection()
            .AddKeyedTransient<IReadable, ClientEncrypted>(GameMessageOpcode.ClientEncrypted)
            .AddKeyedTransient<IReadable, ClientQuestComplete>(GameMessageOpcode.ClientQuestComplete)
            .AddTransient<ClientEncryptedHandler>().AddTransient<ClientQuestCompleteHandler>().BuildServiceProvider();

        (ulong Id, uint ItemId, uint StackCount)[] delivered = null;
        await RunSession(async (world, client, actor) =>
        {
            await SendTurnIn(client);
            await PumpUntil(world, () => actor.Quests.GetQuestState(QuestId) == QuestState.Completed);
            using var first = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
            using var second = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
            delivered = new[] { ReadFixtureInventoryItem(first), ReadFixtureInventoryItem(second) }.OrderBy(item => item.ItemId).ToArray();
        });

        var saved = Assert.Single(await database.GetCharacters(accountId));
        var savedItems = saved.Item.OrderBy(item => item.ItemId).Select(item => (item.Id, item.ItemId, item.StackCount)).ToArray();
        Assert.Equal(savedItems, delivered);
        Assert.Equal(RewardIds, savedItems.Select(item => item.ItemId));
        Assert.All(savedItems, item => Assert.Equal(1u, item.StackCount));
        Assert.Equal(QuestState.Completed, (QuestState)Assert.Single(saved.Quest).State);
        var completed = Assert.Single(saved.Achievement);
        Assert.Equal(AchievementId, completed.AchievementId);
        Assert.Equal(2u, completed.ProgressState);
        Assert.NotNull(completed.DateCompleted);
        ulong completionTime = (ulong)DateTime.SpecifyKind(completed.DateCompleted!.Value, DateTimeKind.Utc).ToFileTimeUtc();

        await RunSession(async (world, client, actor) =>
        {
            PublishFixtureBootstrap(f, actor);
            world.FlushPackets();
            using (var inventory = await ReadPacket(client, GameMessageOpcode.ServerPlayerCreate))
            {
                Assert.Equal(2u, inventory.ReadUInt());
                var wireItems = new[] { ReadFixtureInventoryItem(inventory), ReadFixtureInventoryItem(inventory) };
                Assert.Equal(savedItems, wireItems.OrderBy(item => item.ItemId).ToArray());
            }
            using (var achievement = await ReadPacket(client, GameMessageOpcode.ServerAchievementInit))
            {
                Assert.Equal(1u, achievement.ReadUInt());
                Assert.Equal(AchievementId, achievement.ReadUShort(15));
                Assert.Equal(2u, achievement.ReadUInt());
                Assert.Equal(0u, achievement.ReadUInt());
                Assert.Equal(completionTime, achievement.ReadULong());
            }
            using (var quest = await ReadPacket(client, GameMessageOpcode.ServerQuestInit))
            {
                Assert.Equal(1u, quest.ReadUInt());
                Assert.Equal(QuestId, quest.ReadUShort(15));
            }

            await SendTurnIn(client);
            var reply = ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
            await PumpUntil(world, () => reply.IsCompleted);
            using var state = await reply;
            Assert.Equal(QuestId, state.ReadUShort(15));
            Assert.Equal(QuestState.Completed, state.ReadEnum<QuestState>(32));
        });

        var final = Assert.Single(await database.GetCharacters(accountId));
        Assert.Equal(savedItems, final.Item.OrderBy(item => item.ItemId).Select(item => (item.Id, item.ItemId, item.StackCount)).ToArray());
        Assert.Equal(completed.DateCompleted, Assert.Single(final.Achievement).DateCompleted);

        async Task RunSession(Func<WorldSession, GameProtocolClient, Actor, Task> exercise)
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            await using var client = new GameProtocolClient("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port,
                PacketCrypt.GetKeyFromAuthBuildAndMessage());
            await client.ConnectAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var socket = await listener.AcceptSocketAsync(deadline.Token);
            listener.Stop();
            var world = new WorldSession(messages, services, null, RecordingDispatchProxy<ILoginQueueManager>.Create(out _));
            world.OnAccept(socket);
            world.FlushPackets();
            Assert.Equal(GameMessageOpcode.ServerHello, (await client.ReadInnerFrame(deadline.Token)).Opcode);

            // Only the fixture creates the account and prepared character actor.
            var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            accountProxy.SetProperty(nameof(IAccount.Id), accountId);
            accountProxy.SetProperty(nameof(IAccount.Session), world);
            typeof(WorldSession).GetProperty(nameof(WorldSession.Account))!.SetValue(world, account);
            var model = Assert.Single(await database.GetCharacters(accountId));
            using var actor = LoadActor(f, items, 2, model, world, account, auth);
            var logout = new LogoutManager(actor.Player);
            logout.OnTimerFinished += actor.Cleanup;
            actor.Set(nameof(Player.LogoutManager), logout);
            world.Player = actor.Player;
            await actor.Load();
            await exercise(world, client, actor);

            await client.DisposeAsync();
            await PumpUntil(world, world.CanDispose);
            Assert.Null(world.Player);
            Assert.Empty(actor.Errors);
        }
    }

    private static Task SendTurnIn(GameProtocolClient client) => client.SendEncrypted(GameMessageOpcode.ClientQuestComplete, writer =>
    {
        writer.Write(QuestId, 15);
        writer.Write((ushort)0, 15);
        writer.Write(false);
    });

    private static async Task PumpUntil(WorldSession world, Func<bool> complete)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!complete()) { world.Update(.01); await Task.Delay(5, deadline.Token); }
    }

    private static async Task<GamePacketReader> ReadPacket(GameProtocolClient client, GameMessageOpcode expected)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            var frame = await client.ReadInnerFrame(deadline.Token);
            if (frame.Opcode == expected) return new GamePacketReader(new MemoryStream(frame.Body));
        }
    }

    private static (ulong Id, uint ItemId, uint StackCount) ReadFixtureInventoryItem(GamePacketReader reader)
    {
        ulong id = reader.ReadULong();
        _ = reader.ReadULong(); // maker
        uint itemId = reader.ReadUInt(18);
        var location = new ItemLocation();
        location.Read(reader);
        uint count = reader.ReadUInt();
        _ = reader.ReadUInt(); // charges
        _ = reader.ReadULong(); _ = reader.ReadUInt(); _ = reader.ReadULong(); // circuits, glyph, thresholds
        _ = reader.ReadUInt(); _ = reader.ReadUInt(); _ = reader.ReadByte(); // durability and reserved fields
        _ = reader.ReadUInt(); _ = reader.ReadUInt(); _ = reader.ReadUInt(); // dye, flags, expiry
        for (int i = 0; i < 2; i++) { _ = reader.ReadUInt(3); _ = reader.ReadUInt(); _ = reader.ReadUInt(); }
        _ = reader.ReadUInt(18); // power core
        Assert.Equal(0u, reader.ReadUInt(3)); // microchips
        Assert.Equal(0u, reader.ReadUInt(4)); // glyphs
        Assert.Equal(0u, reader.ReadUInt(6)); // trade partners
        _ = reader.ReadUInt(); _ = reader.ReadUInt(6); // effective level, update reason
        return (id, itemId, count);
    }

    private static void PublishFixtureBootstrap(Fixture f, Actor actor)
    {
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.Class), PlayerBindPointTests.Table(new ClassEntry { Id = (uint)actor.Player.Class }));
        tables.SetProperty(nameof(IGameTableManager.XpPerLevel), PlayerBindPointTests.Table(new XpPerLevelEntry { Id = 1 }));
        typeof(Player).GetField("gameTableManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, f.Source.Tables);
        typeof(Player).GetField("attributePointAllocations", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, new uint[6]);
        var account = (RecordingDispatchProxy<IAccount>)(object)actor.Player.Account;
        foreach (var property in typeof(IAccount).GetProperties().Where(p => p.PropertyType.IsInterface && p.Name.EndsWith("Manager", StringComparison.Ordinal)))
            account.SetProperty(property.Name, typeof(RecordingDispatchProxy<>).MakeGenericType(property.PropertyType)
                .GetMethod("Create")!.Invoke(null, new object[] { null }));
        var currencies = (RecordingDispatchProxy<ICurrencyManager>)(object)actor.Player.CurrencyManager;
        currencies.SetMethodReturnFactory("GetEnumerator", () => Enumerable.Empty<ICurrency>().GetEnumerator());
        var entitlements = (RecordingDispatchProxy<ICharacterEntitlementManager>)(object)actor.Player.EntitlementManager;
        entitlements.SetMethodReturnFactory("GetEnumerator", () => Enumerable.Empty<ICharacterEntitlement>().GetEnumerator());
        var satchel = (RecordingDispatchProxy<ISupplySatchelManager>)(object)actor.Player.SupplySatchelManager;
        satchel.SetMethodReturn(nameof(ISupplySatchelManager.BuildNetworkPacket), new ushort[512]);

        // Invoke the production packet burst and Quest snapshot at the fixture's world-entry boundary.
        typeof(Player).GetMethod("SendMapPacketBurst", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(actor.Player, [new List<ServerPlayerCreate.Faction.FactionReputation>()]);
        actor.Quests.SendInitialPackets();
    }
}
