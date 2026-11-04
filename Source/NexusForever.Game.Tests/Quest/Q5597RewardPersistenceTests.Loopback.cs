using System.Collections.Immutable;
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
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Tutorial;
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
using Xunit.Abstractions;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class Q5597RewardPersistenceTests
{
    private readonly ITestOutputHelper output;

    public Q5597RewardPersistenceTests(ITestOutputHelper output) => this.output = output;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LoopbackTurnInPublishesSavedRewardsOnce(bool rejectFirstSave, bool tutorialRecovery)
    {
        const uint accountId = 12;
        ushort questId = tutorialRecovery ? StarterTutorialDefinition.ExileMovementQuestId : QuestId;
        const ushort nextQuestId = StarterTutorialDefinition.ExileHoverboardQuestId;
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
            seed.CharacterQuest.Add(new() { Id = 42, QuestId = questId, State = (byte)QuestState.Achieved });
            await seed.SaveChangesAsync();
        }
        ConfigureQuest(f, reclaim: false, questId);
        if (tutorialRecovery)
        {
            var next = RecordingDispatchProxy<IQuestInfo>.Create(out var nextProxy);
            nextProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = nextQuestId, PushedItemIds = [] });
            nextProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
                new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 21324, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 1 })));
            var global = (RecordingDispatchProxy<IGlobalQuestManager>)(object)f.Global;
            global.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestGivers), Array.Empty<uint>());
            global.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), args => (ushort)args[0] switch
            {
                StarterTutorialDefinition.ExileMovementQuestId => f.Info,
                nextQuestId => next,
                _ => null
            });
        }
        var items = CreateItems();
        var messages = new MessageManager(NullLogger<MessageManager>.Instance);
        messages.RegisterNetworkManagerMessagesAndHandlers();
        messages.RegisterNetworkManagerWorldMessages();
        messages.RegisterMessageHandler(typeof(ClientQuestCompleteHandler));
        using var services = new ServiceCollection()
            .AddKeyedTransient<IReadable, ClientEncrypted>(GameMessageOpcode.ClientEncrypted)
            .AddKeyedTransient<IReadable, ClientQuestComplete>(GameMessageOpcode.ClientQuestComplete)
            .AddTransient<ClientEncryptedHandler>().AddTransient<ClientQuestCompleteHandler>().BuildServiceProvider();

        if (rejectFirstSave)
        {
            using (var fault = f.Context())
                await fault.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER q5597_reject_completion BEFORE UPDATE OF state ON character_quest
                    WHEN NEW.id = 42 AND NEW.state = 3
                    BEGIN SELECT RAISE(ABORT, 'Loopback test rejected quest completion'); END;
                    """);
            await RunSession(async (world, client, actor) =>
            {
                await SendWithSaveHeld(world, client, actor);
                await PumpUntil(world, () => world.IsDisconnecting);
                Assert.True(actor.Quests.RequiresAuthoritativeReload);
            });

            var rolledBack = Assert.Single(await database.GetCharacters(accountId));
            Assert.Equal(QuestState.Achieved, (QuestState)Assert.Single(rolledBack.Quest).State);
            Assert.Empty(rolledBack.Item);
            Assert.Empty(rolledBack.Achievement);
            using var recovered = f.Context();
            await recovered.Database.ExecuteSqlRawAsync("DROP TRIGGER q5597_reject_completion");
        }

        (ulong Id, uint ItemId, uint StackCount)[] delivered = null;
        await RunSession(async (world, client, actor) =>
        {
            await SendWithSaveHeld(world, client, actor);
            await PumpUntil(world, () => world.CanProcessIncomingPackets);
            using (var read = f.Context())
            {
                Assert.Equal(QuestState.Completed, (QuestState)read.CharacterQuest.Single(row => row.QuestId == questId).State);
                if (tutorialRecovery)
                    Assert.Equal(QuestState.Accepted, (QuestState)read.CharacterQuest.Single(row => row.QuestId == nextQuestId).State);
                Assert.Equal(2, read.Item.Count());
                Assert.Equal(2u, read.CharacterAchievement.Single().ProgressState);
            }
            using var first = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
            using var second = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
            delivered = new[] { ReadFixtureInventoryItem(first), ReadFixtureInventoryItem(second) }.OrderBy(item => item.ItemId).ToArray();
            using var state = await ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
            Assert.Equal(questId, state.ReadUShort(15));
            Assert.Equal(QuestState.Completed, state.ReadEnum<QuestState>(32));
            if (tutorialRecovery)
            {
                using var next = await ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
                Assert.Equal(nextQuestId, next.ReadUShort(15));
                Assert.Equal(QuestState.Accepted, next.ReadEnum<QuestState>(32));
            }
            else
            {
                using var dialog = await ReadPacket(client, GameMessageOpcode.ServerDialogEnd);
            }
        });

        var saved = Assert.Single(await database.GetCharacters(accountId));
        var savedItems = saved.Item.OrderBy(item => item.ItemId).Select(item => (item.Id, item.ItemId, item.StackCount)).ToArray();
        Assert.Equal(savedItems, delivered);
        Assert.Equal(RewardIds, savedItems.Select(item => item.ItemId));
        Assert.All(savedItems, item => Assert.Equal(1u, item.StackCount));
        Assert.Equal(QuestState.Completed, (QuestState)saved.Quest.Single(row => row.QuestId == questId).State);
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
                Assert.Equal(tutorialRecovery ? 0u : 1u, quest.ReadUInt());
                if (!tutorialRecovery) Assert.Equal(questId, quest.ReadUShort(15));
            }
            if (tutorialRecovery)
            {
                using var next = await ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
                Assert.Equal(nextQuestId, next.ReadUShort(15));
                Assert.Equal(QuestState.Accepted, next.ReadEnum<QuestState>(32));
            }

            await SendTurnIn(client, questId);
            var reply = ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
            await PumpUntil(world, () => reply.IsCompleted);
            using var state = await reply;
            Assert.Equal(questId, state.ReadUShort(15));
            Assert.Equal(QuestState.Completed, state.ReadEnum<QuestState>(32));
        });

        var final = Assert.Single(await database.GetCharacters(accountId));
        Assert.Equal(savedItems, final.Item.OrderBy(item => item.ItemId).Select(item => (item.Id, item.ItemId, item.StackCount)).ToArray());
        Assert.Equal(completed.DateCompleted, Assert.Single(final.Achievement).DateCompleted);

        async Task SendWithSaveHeld(WorldSession world, GameProtocolClient client, Actor actor)
        {
            if (tutorialRecovery)
                actor.Player.TryRecoverStarterTutorialQuestProgression();
            else
            {
                await SendTurnIn(client, questId);
                // The session pumps events before handling input. Stop after the
                // request and before its queued Character save continuation runs.
                await PumpUntil(world, () => !world.CanProcessIncomingPackets || world.IsDisconnecting);
            }
            Assert.False(world.IsDisconnecting);
            Assert.True(actor.Player.IsNormalSaveInProgress);
            Assert.Equal(QuestState.Completed, actor.Quests.GetQuestState(questId));
            if (tutorialRecovery)
            {
                Assert.Equal(QuestState.Accepted, actor.Quests.GetQuestState(nextQuestId));
                actor.Player.TryRecoverStarterTutorialQuestProgression();
            }
            using var read = f.Context();
            Assert.Equal(QuestState.Achieved, (QuestState)read.CharacterQuest.Single().State);
            Assert.Empty(read.Item);
            Assert.Empty(read.CharacterAchievement);
            await AssertTurnInPacketsWithheld(world, client);
        }

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
            using var actor = LoadActor(f, items, 2, model, world, account, auth, questId);
            if (tutorialRecovery)
            {
                var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
                mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 3460 });
                mapProxy.SetMethodReturn(nameof(IBaseMap.Search), Array.Empty<IGridEntity>());
                typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(actor.Player, map);
                actor.Set(nameof(Player.Faction1), Faction.Exile);
                typeof(Player).GetField("globalQuestManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, f.Global);
                typeof(Player).GetField("gameTableManager", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, f.Source.Tables);
                ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables).SetProperty(nameof(IGameTableManager.WorldLocation2),
                    PlayerBindPointTests.Table<WorldLocation2Entry>());
            }
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

    private async Task AssertTurnInPacketsWithheld(WorldSession world, GameProtocolClient client)
    {
        // The marker delimits a flush on this private socket while the save is held.
        world.EnqueueMessageEncrypted(new ServerPlayerEnteredWorld());
        world.FlushPackets();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            var frame = await client.ReadInnerFrame(deadline.Token);
            if (frame.Opcode == GameMessageOpcode.ServerPlayerEnteredWorld) break;
            Assert.False(frame.Opcode is GameMessageOpcode.ServerItemAdd or GameMessageOpcode.ServerQuestStateChange
                or GameMessageOpcode.ServerAchievementUpdate or GameMessageOpcode.ServerDialogEnd, $"Premature {frame.Opcode}");
        }
        output.WriteLine("No item, quest-state, achievement or dialog-end packets while the turn-in save was held.");
    }

    private static Task SendTurnIn(GameProtocolClient client, ushort questId) => client.SendEncrypted(GameMessageOpcode.ClientQuestComplete, writer =>
    {
        writer.Write(questId, 15);
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
