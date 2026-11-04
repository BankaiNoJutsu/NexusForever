using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Reputation;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Tutorial;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.Message;
using NexusForever.Network.Message.Handler;
using NexusForever.Network.Session;
using NexusForever.Network.Message.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Script;
using NexusForever.WorldServer;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;
using NexusForever.WorldServer.Network.Message.Handler.Quest;
using Xunit.Abstractions;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using ConversionSaveDatabase = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.ConversionSaveDatabase;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class Q5597RewardPersistenceTests
{
    private readonly ITestOutputHelper output;
    private readonly DbContextOptions<CharacterContext> conversionOptions;
    private const uint FixtureFanfareSpellId = 91001;
    private const Property FixtureLevelProperty = Property.BaseHealth;

    public Q5597RewardPersistenceTests(ITestOutputHelper output) => this.output = output;

    internal Q5597RewardPersistenceTests(ITestOutputHelper output, DbContextOptions<CharacterContext> conversionOptions)
        : this(output)
    {
        this.conversionOptions = conversionOptions;
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(true, false, true, false, false)]
    [InlineData(false, false, true, true, false)]
    [InlineData(false, false, false, false, true)]
    [InlineData(true, false, false, false, true)]
    public Task LoopbackTurnInPublishesSavedRewardsOnce(bool rejectFirstSave, bool tutorialRecovery, bool levelReward,
        bool replaceMapAdmission, bool autoAccept)
        => RunQuestLoopback(rejectFirstSave, tutorialRecovery, levelReward, replaceMapAdmission, autoAccept);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public Task LoopbackAcceptancePublishesOnlyAfterTheCharacterSave(bool rejectFirstSave, bool itemStarter)
        => RunQuestLoopback(rejectFirstSave, acceptOnly: true, itemStarter: itemStarter);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public Task LoopbackSharedQuestRepliesOnlyForCommittedAcceptance(bool rejectFirstSave, bool replaceSharer)
        => RunQuestLoopback(rejectFirstSave, acceptOnly: true, share: true, replaceSharer: replaceSharer);

    [Theory]
    [InlineData("success")]
    [InlineData("lost-ack")]
    [InlineData("retry")]
    [InlineData("rollback")]
    public Task LoopbackItemConversionPublishesOnlyAfterItsCharacterSave(string outcome)
        => RunQuestLoopback(false, itemConversion: outcome);

    [Theory]
    [InlineData(1u, "success")]
    [InlineData(2u, "retry")]
    [InlineData(3u, "lost-ack")]
    [InlineData(3u, "rollback")]
    [InlineData(1u, "missing-faction")]
    [InlineData(3u, "missing-faction")]
    public Task LoopbackReputationConversionCommitsTheDebitAndStandingTogether(uint conversionType, string outcome)
        => RunQuestLoopback(false, itemConversion: outcome, conversionType: conversionType);

    private async Task RunQuestLoopback(bool rejectFirstSave, bool tutorialRecovery = false, bool levelReward = false,
        bool replaceMapAdmission = false, bool autoAccept = false, bool acceptOnly = false, bool itemStarter = false,
        bool share = false, bool replaceSharer = false, string itemConversion = null, uint conversionType = 0)
    {
        const uint accountId = 12;
        const uint sharerGuid = 99;
        var sharer = RecordingDispatchProxy<IPlayer>.Create(out var sharerPlayer);
        var sharerSession = RecordingDispatchProxy<IGameSession>.Create(out var originalSharer);
        var replacementSession = RecordingDispatchProxy<IGameSession>.Create(out var replacementSharer);
        sharerPlayer.SetProperty(nameof(IPlayer.Session), sharerSession);
        const ulong starterId = 50;
        const uint conversionPrice = 1;
        const uint reputationReward = 5;
        const Faction reputationFaction = Faction.Exile;
        bool reputationConversion = conversionType != 0;
        bool spendsCurrency = conversionType == 3;
        uint starterDefinition = RewardIds[0];
        ushort questId = tutorialRecovery ? StarterTutorialDefinition.ExileMovementQuestId : QuestId;
        const ushort nextQuestId = StarterTutorialDefinition.ExileHoverboardQuestId;
        RecordingDispatchProxy<IWorldSession> observer = null;
        RecordingDispatchProxy<IWorldSession> retiredObserver = null;
        RecordingDispatchProxy<IWorldSession> lateObserverSession = null;
        RecordingDispatchProxy<IAccount> retiredObserverAccount = null;
        using var f = new Fixture(itemConversion == null ? null : conversionOptions);
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
            database.Initialise(new DatabaseConnectionString
            {
                Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = seed.Database.GetDbConnection().ConnectionString
            });
            seed.Character.Single().AccountId = accountId;
            if (levelReward)
            {
                seed.Character.Single().Level = 1;
                seed.CharacterStat.AddRange(
                    new CharacterStatModel { Id = 42, Stat = (byte)Stat.Health, Value = 9f },
                    new CharacterStatModel { Id = 42, Stat = (byte)Stat.Shield, Value = 14f });
            }
            if (!autoAccept && !acceptOnly)
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = questId, State = (byte)QuestState.Achieved });
            if (itemStarter || itemConversion != null && !spendsCurrency)
                seed.Item.Add(new() { Id = starterId, OwnerId = 42, ItemId = starterDefinition,
                    Location = (ushort)InventoryLocation.Inventory, StackCount = 1 });
            if (spendsCurrency)
                seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = (byte)CurrencyType.Credits, Amount = 40 });
            await seed.SaveChangesAsync();
        }
        ConfigureQuest(f, reclaim: autoAccept || acceptOnly, questId);
        if (share) f.Info.Entry.QuestShareEnum = 1;
        if (autoAccept || acceptOnly)
        {
            var info = (RecordingDispatchProxy<IQuestInfo>)(object)f.Info;
            if (autoAccept) info.SetMethodReturn(nameof(IQuestInfo.IsAutoComplete), true);
            info.SetProperty(nameof(IQuestInfo.PrerequisiteQuests), ImmutableList<Quest2Entry>.Empty);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)f.Global)
                .SetMethodReturn(nameof(IGlobalQuestManager.GetQuestGivers), new[] { Receiver });
        }
        if (levelReward)
            ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetMethodReturn(nameof(IQuestInfo.GetRewardExperience), 120u);
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
        if (itemStarter)
        {
            var starter = items.GetItemInfo(starterDefinition).Entry;
            starter.Quest2IdActivation = questId;
            starter.MaxStackCount = 10;
        }
        var conversionDatabase = itemConversion == null ? null : new ConversionSaveDatabase(f, itemConversion);
        if (conversionDatabase != null)
        {
            var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
            tables.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(items.GetItemInfo(PushedItem).Entry));
            if (spendsCurrency)
                tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry
                    { Id = (uint)CurrencyType.Credits }));
            tables.SetProperty(nameof(IGameTableManager.ResourceConversion), PlayerBindPointTests.Table(new ResourceConversionEntry
            {
                Id = 10, ResourceConversionTypeEnum = conversionType,
                SourceId = spendsCurrency ? (uint)CurrencyType.Credits : starterDefinition, SourceCount = conversionPrice,
                TargetId = reputationConversion ? (uint)reputationFaction : PushedItem,
                TargetCount = reputationConversion ? reputationReward : 1
            }));
        }
        var messages = new MessageManager(NullLogger<MessageManager>.Instance);
        messages.RegisterNetworkManagerMessagesAndHandlers();
        messages.RegisterNetworkManagerWorldMessages();
        messages.RegisterMessageHandler(typeof(ClientQuestCompleteHandler));
        messages.RegisterMessageHandler(typeof(ClientQuestAcceptHandler));
        messages.RegisterMessageHandler(typeof(ClientQuestShareResultHandler));
        messages.RegisterMessageHandler(typeof(ClientConvertResourceHandler));
        using var services = new ServiceCollection()
            .AddKeyedTransient<IReadable, ClientEncrypted>(GameMessageOpcode.ClientEncrypted)
            .AddKeyedTransient<IReadable, ClientQuestComplete>(GameMessageOpcode.ClientQuestComplete)
            .AddKeyedTransient<IReadable, ClientQuestAccept>(GameMessageOpcode.ClientQuestAccept)
            .AddKeyedTransient<IReadable, ClientQuestShareResult>(GameMessageOpcode.ClientQuestShareResult)
            .AddKeyedTransient<IReadable, ClientConvertResource>(GameMessageOpcode.ClientConvertResource)
            .AddSingleton(f.Source.Tables)
            .AddSingleton<ILogger<ClientConvertResourceHandler>>(NullLogger<ClientConvertResourceHandler>.Instance)
            .AddTransient<ClientEncryptedHandler>().AddTransient<ClientQuestCompleteHandler>()
            .AddTransient<ClientQuestAcceptHandler>().AddTransient<ClientQuestShareResultHandler>()
            .AddTransient<ClientConvertResourceHandler>().BuildServiceProvider();

        if (conversionDatabase != null)
        {
            await RunSession(async (world, client, actor) =>
            {
                int authSaves = 0;
                actor.OnAuth = () => authSaves++;
                if (itemConversion == "missing-faction")
                {
                    var request = new ClientConvertResource();
                    typeof(ClientConvertResource).GetProperty(nameof(ClientConvertResource.ConversionId))!.SetValue(request, 10u);
                    new ClientConvertResourceHandler(NullLogger<ClientConvertResourceHandler>.Instance, f.Source.Tables)
                        .HandleMessage(world, request);
                    if (spendsCurrency) Assert.Equal(40ul, Assert.Single(actor.Player.CurrencyManager).Amount);
                    else Assert.Equal(1u, actor.Player.Inventory.GetItemCount(starterDefinition));
                    Assert.False(actor.Player.IsNormalSaveInProgress);
                    Assert.Equal(0, authSaves);
                    Assert.Equal(0, conversionDatabase.Attempts);
                    await AssertQuestPacketsWithheld(world, client);
                    return;
                }
                await client.SendEncrypted(GameMessageOpcode.ClientConvertResource, writer =>
                {
                    writer.Write(10u, 14);
                    writer.Write(0u);
                    writer.Write(0ul);
                });
                await PumpUntil(world, () => world.IsDisconnecting || (reputationConversion
                    ? actor.Player.ReputationManager.GetReputation(reputationFaction)?.Amount == reputationReward
                    : actor.Player.Inventory.GetItemCount(PushedItem) == 1));
                Assert.False(world.IsDisconnecting);
                using (var held = f.Context())
                {
                    Assert.Empty(held.CharacterReputation);
                    if (spendsCurrency) Assert.Equal(40ul, Assert.Single(held.CharacterCurrency).Amount);
                    else Assert.Equal(starterId, Assert.Single(held.Item).Id);
                }
                await AssertQuestPacketsWithheld(world, client);
                Assert.True(actor.Player.IsNormalSaveInProgress);
                Assert.False(world.CanProcessIncomingPackets);

                bool rolledBack = itemConversion == "rollback";
                await PumpUntil(world, () => world.CanProcessIncomingPackets || world.IsDisconnecting);
                Assert.Equal(rolledBack, world.IsDisconnecting);
                Assert.Equal(itemConversion is "retry" or "rollback" ? 2 : 1, conversionDatabase.Attempts);
                Assert.Equal(1, authSaves);
                using var read = f.Context();
                if (spendsCurrency)
                    Assert.Equal(rolledBack ? 40ul : 39ul, Assert.Single(read.CharacterCurrency).Amount);
                if (rolledBack)
                {
                    if (!spendsCurrency) Assert.Equal(starterId, Assert.Single(read.Item).Id);
                    Assert.Empty(read.CharacterReputation);
                    Assert.Empty(read.BankReconciliationReceipt);
                    return;
                }

                Assert.Single(read.BankReconciliationReceipt);
                if (spendsCurrency)
                {
                    using var balance = await ReadPacket(client, GameMessageOpcode.ServerCombatReward);
                    Assert.Equal(0u, balance.ReadUInt(5));
                    Assert.Equal(39ul, balance.ReadULong());
                }
                else
                {
                    using var removed = await ReadPacket(client, GameMessageOpcode.ServerItemDelete);
                    Assert.Equal(starterId, removed.ReadULong());
                }
                if (reputationConversion)
                {
                    Assert.Empty(read.Item);
                    var standing = Assert.Single(read.CharacterReputation);
                    Assert.Equal((uint)reputationFaction, standing.FactionId);
                    Assert.Equal((float)reputationReward, standing.Amount);
                    var notice = ReadPacket(client, GameMessageOpcode.ServerReputationUpdate);
                    await PumpUntil(world, () => notice.IsCompleted);
                    using var delta = await notice;
                    Assert.Equal((uint)reputationFaction, delta.ReadUInt(14));
                    Assert.Equal((float)reputationReward, delta.ReadSingle());
                }
                else
                {
                    var row = Assert.Single(read.Item);
                    Assert.Equal(PushedItem, row.ItemId);
                    Assert.NotEqual(starterId, row.Id);
                    using var added = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
                    Assert.Equal((row.Id, PushedItem, 1u), ReadFixtureInventoryItem(added));
                }
            });
            return;
        }

        if (acceptOnly)
        {
            if (rejectFirstSave)
            {
                using var fault = f.Context();
                await fault.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER reject_quest_accept BEFORE INSERT ON character_quest
                    WHEN NEW.id = 42 AND NEW.state = 2
                    BEGIN SELECT RAISE(ABORT, 'Loopback test rejected quest acceptance'); END;
                    """);
                await RunSession(async (world, client, actor) =>
                {
                    await SendAcceptanceWithSaveHeld(world, client, actor);
                    await PumpUntil(world, () => world.IsDisconnecting);
                    Assert.True(actor.Quests.RequiresAuthoritativeReload);
                    Assert.Empty(ShareResults(originalSharer));
                });
                using var read = f.Context();
                Assert.Empty(read.CharacterQuest);
                Assert.Equal(itemStarter ? new[] { starterId } : [], read.Item.Select(row => row.Id).ToArray());
                await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_quest_accept");
            }

            await RunSession(async (world, client, actor) =>
            {
                await SendAcceptanceWithSaveHeld(world, client, actor);
                if (replaceSharer) sharerPlayer.SetProperty(nameof(IPlayer.Session), replacementSession);
                await PumpUntil(world, () => world.CanProcessIncomingPackets);
                using var read = f.Context();
                var accepted = Assert.Single(read.CharacterQuest);
                Assert.Equal((questId, (byte)QuestState.Achieved), (accepted.QuestId, accepted.State));
                var pushed = Assert.Single(read.Item);
                Assert.Equal(PushedItem, pushed.ItemId);
                if (itemStarter)
                {
                    using var consumed = await ReadPacket(client, GameMessageOpcode.ServerItemDelete);
                    Assert.Equal(starterId, consumed.ReadULong());
                }
                using var grant = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
                Assert.Equal((pushed.Id, PushedItem, 1u), ReadFixtureInventoryItem(grant));
                using var state = await ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
                Assert.Equal(questId, state.ReadUShort(15));
                Assert.Equal(QuestState.Achieved, state.ReadEnum<QuestState>(32));
                if (share)
                {
                    Assert.Empty(ShareResults(replacementSharer));
                    if (replaceSharer) Assert.Empty(ShareResults(originalSharer));
                    else
                    {
                        var result = Assert.Single(ShareResults(originalSharer));
                        Assert.True(result.Accepted);
                        Assert.Equal(questId, result.QuestId);
                        Assert.Equal(actor.Player.Guid, result.SharerUnitId);
                    }
                }
                else if (!itemStarter)
                {
                    using var dialog = await ReadPacket(client, GameMessageOpcode.ServerDialogEnd);
                }
            });
            return;
        }

        if (rejectFirstSave)
        {
            using (var fault = f.Context())
            {
                string trigger = autoAccept ? """
                    CREATE TRIGGER q5597_reject_completion BEFORE INSERT ON character_quest
                    WHEN NEW.id = 42 AND NEW.state = 3
                    BEGIN SELECT RAISE(ABORT, 'Loopback test rejected quest completion'); END;
                    """ : """
                    CREATE TRIGGER q5597_reject_completion BEFORE UPDATE OF state ON character_quest
                    WHEN NEW.id = 42 AND NEW.state = 3
                    BEGIN SELECT RAISE(ABORT, 'Loopback test rejected quest completion'); END;
                    """;
                await fault.Database.ExecuteSqlRawAsync(trigger);
            }
            await RunSession(async (world, client, actor) =>
            {
                await SendWithSaveHeld(world, client, actor);
                await PumpUntil(world, () => world.IsDisconnecting);
                Assert.True(actor.Quests.RequiresAuthoritativeReload);
                if (levelReward)
                {
                    Assert.Empty(LevelPackets(observer));
                    Assert.Empty(LevelPackets(retiredObserver));
                    Assert.Empty(LevelPackets(lateObserverSession));
                    AssertNoVitals(observer, actor);
                    AssertNoVitals(lateObserverSession, actor);
                    Assert.Empty(LevelMessages(actor));
                    Assert.Empty(FanfarePackets(observer));
                    FlushPropertyUpdates(actor);
                    Assert.Empty(PropertyPackets(observer));
                    Assert.Empty(PropertyMessages(actor));
                }
            });

            var rolledBack = Assert.Single(await database.GetCharacters(accountId));
            if (autoAccept) Assert.Empty(rolledBack.Quest);
            else Assert.Equal(QuestState.Achieved, (QuestState)Assert.Single(rolledBack.Quest).State);
            Assert.Empty(rolledBack.Item);
            Assert.Empty(rolledBack.Achievement);
            if (levelReward) Assert.Equal((1, 0u), ((int)rolledBack.Level, rolledBack.TotalXp));
            if (levelReward)
            {
                Assert.Equal(9f, Assert.Single(rolledBack.Stat, stat => stat.Stat == (byte)Stat.Health).Value);
                Assert.Equal(14f, Assert.Single(rolledBack.Stat, stat => stat.Stat == (byte)Stat.Shield).Value);
            }
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
                if (levelReward)
                {
                    var character = read.Character.Single();
                    Assert.Equal((2, 120u), ((int)character.Level, character.TotalXp));
                    Assert.Equal(2f, Assert.Single(LevelPackets(observer)).Stat.Value);
                    Assert.Equal(2f, Assert.Single(LevelPackets(lateObserverSession)).Stat.Value);
                    Assert.Equal(2f, Assert.Single(LevelMessages(actor)).Value);
                    Assert.Empty(LevelPackets(retiredObserver));
                    Assert.Equal(6f, read.CharacterStat.Single(stat => stat.Stat == (byte)Stat.Health).Value);
                    Assert.Equal(7f, read.CharacterStat.Single(stat => stat.Stat == (byte)Stat.Shield).Value);
                    Assert.Equal(6f, Assert.Single(VitalMessages(actor), update => update.Stat == Stat.Health).Value);
                    Assert.Equal(7f, Assert.Single(VitalMessages(actor), update => update.Stat == Stat.Shield).Value);
                    foreach (var recipient in new[] { observer, lateObserverSession })
                    {
                        Assert.Equal(6u, Assert.Single(HealthPackets(recipient)).Health);
                        Assert.Equal(7f, Assert.Single(ShieldPackets(recipient)).Stat.Value);
                    }
                    Assert.Equal(25f, Assert.Single(PropertyMessages(actor), update => update.Property == FixtureLevelProperty).Value);
                    FlushPropertyUpdates(actor);
                    Assert.Equal(25f, Assert.Single(Assert.Single(PropertyPackets(observer)).Properties,
                        property => property.Property == FixtureLevelProperty).Value);
                    Assert.Equal(25f, Assert.Single(Assert.Single(PropertyPackets(lateObserverSession)).Properties,
                        property => property.Property == FixtureLevelProperty).Value);
                    world.FlushPackets();
                    if (replaceMapAdmission)
                        Assert.Empty(FanfarePackets(observer));
                    else
                        Assert.Equal(FixtureFanfareSpellId, Assert.Single(FanfarePackets(observer)).Spell4Id);
                }
            }
            if (autoAccept)
            {
                using var pushed = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
                Assert.Equal(PushedItem, ReadFixtureInventoryItem(pushed).ItemId);
            }
            using var first = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
            using var second = await ReadPacket(client, GameMessageOpcode.ServerItemAdd);
            delivered = new[] { ReadFixtureInventoryItem(first), ReadFixtureInventoryItem(second) }.OrderBy(item => item.ItemId).ToArray();
            using var state = await ReadPacket(client, GameMessageOpcode.ServerQuestStateChange);
            Assert.Equal(questId, state.ReadUShort(15));
            Assert.Equal(QuestState.Completed, state.ReadEnum<QuestState>(32));
            if (levelReward && !replaceMapAdmission)
            {
                using var fanfare = await ReadPacket(client, GameMessageOpcode.ServerSpellStart);
            }
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
            if (levelReward && !replaceMapAdmission)
            {
                using var properties = await ReadPacket(client, GameMessageOpcode.ServerEntityPropertiesUpdate);
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
        if (levelReward) Assert.Equal((2, 120u), ((int)final.Level, final.TotalXp));

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
            if (autoAccept) Assert.Empty(read.CharacterQuest);
            else Assert.Equal(QuestState.Achieved, (QuestState)read.CharacterQuest.Single().State);
            Assert.Empty(read.Item);
            Assert.Empty(read.CharacterAchievement);
            if (levelReward)
            {
                Assert.Equal(2u, actor.Player.Level);
                Assert.Equal(1, read.Character.Single().Level);
                Assert.Empty(LevelPackets(observer));
                Assert.Empty(LevelPackets(retiredObserver));
                Assert.Empty(LevelMessages(actor));
                Assert.Empty(FanfarePackets(observer));
                Assert.Equal(6u, actor.Player.Health);
                Assert.Equal(7u, actor.Player.Shield);
                AssertNoVitals(observer, actor);
                Assert.Equal(20f, actor.Player.GetPropertyValue(FixtureLevelProperty));
                actor.Player.SetBaseProperty(FixtureLevelProperty, 25f);
                FlushPropertyUpdates(actor);
                Assert.Empty(PropertyPackets(observer));
                Assert.Empty(PropertyMessages(actor));
                lateObserverSession = CreateLateObserver(actor.Player);
                var create = Assert.Single(lateObserverSession.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
                    .SelectMany(call => call.Arguments).OfType<ServerEntityCreate>());
                Assert.Equal(1f, Assert.Single(create.Stats, stat => stat.Stat == Stat.Level).Value);
                Assert.Equal(10f, Assert.Single(create.Properties, property => property.Property == FixtureLevelProperty).Value);
                Assert.Equal(9f, Assert.Single(create.Stats, stat => stat.Stat == Stat.Health).Value);
                Assert.Equal(14f, Assert.Single(create.Stats, stat => stat.Stat == Stat.Shield).Value);
                Assert.DoesNotContain(create.Properties, property => property.Property == Property.BaseVigor);
                Assert.Empty(LevelPackets(lateObserverSession));
                retiredObserverAccount.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
                if (replaceMapAdmission)
                    ((IPlayerMapAdmission)actor.Player).BeginMapAdmission();
            }
            await AssertQuestPacketsWithheld(world, client);
        }

        async Task SendAcceptanceWithSaveHeld(WorldSession world, GameProtocolClient client, Actor actor)
        {
            if (share)
                await client.SendEncrypted(GameMessageOpcode.ClientQuestShareResult, writer =>
                {
                    writer.Write(questId, 15);
                    writer.Write(true);
                });
            else await client.SendEncrypted(GameMessageOpcode.ClientQuestAccept, writer =>
            {
                new ItemLocation { Location = itemStarter ? InventoryLocation.Inventory : (InventoryLocation)300 }.Write(writer);
                writer.Write(questId, 15);
            });
            await PumpUntil(world, () => actor.Quests.GetQuestState(questId) == QuestState.Achieved || world.IsDisconnecting);
            Assert.False(world.IsDisconnecting);
            using var read = f.Context();
            Assert.Empty(read.CharacterQuest);
            Assert.Equal(itemStarter ? new[] { starterId } : [], read.Item.Select(row => row.Id).ToArray());
            if (share) Assert.Empty(ShareResults(originalSharer));
            await AssertQuestPacketsWithheld(world, client);
            Assert.True(actor.Player.IsNormalSaveInProgress);
            Assert.False(world.CanProcessIncomingPackets);
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
            using var actor = LoadActor(f, items, 2, model, world, account, auth, questId, conversionDatabase);
            if (reputationConversion)
            {
                var factions = RecordingDispatchProxy<IFactionManager>.Create(out var factionManager);
                var faction = RecordingDispatchProxy<IFactionNode>.Create(out var node);
                node.SetProperty(nameof(IFactionNode.FactionId), reputationFaction);
                factionManager.SetMethodReturn(nameof(IFactionManager.GetFaction), itemConversion == "missing-faction" ? null : faction);
                actor.Set(nameof(Player.ReputationManager), new ReputationManager(actor.Player, model, factions));
                if (spendsCurrency)
                {
                    var wallet = new CurrencyManager(f.Source.Tables);
                    wallet.Initialise(actor.Player, model);
                    actor.Set(nameof(Player.CurrencyManager), wallet);
                }
            }
            if (autoAccept || acceptOnly) actor.Set(nameof(Player.Faction1), Faction.Exile);
            if (levelReward)
            {
                var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
                mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 870 });
                typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(actor.Player, map);
                ConfigureExperience(f, actor, model);
                ConfigureFanfare(actor.Player, f.Scripts);
                observer = AddObserver(actor.Player, map, 70, out _);
                retiredObserver = AddObserver(actor.Player, map, 71, out retiredObserverAccount);
            }
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
            if (share)
            {
                var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity)
                    .GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actor.Player);
                visible.Add(sharerGuid, sharer);
                actor.Quests.QuestShareReceive(questId, sharerGuid);
            }
            await exercise(world, client, actor);

            await client.DisposeAsync();
            await PumpUntil(world, world.CanDispose);
            Assert.Null(world.Player);
            Assert.Empty(actor.Errors);
        }

        static IEnumerable<ServerQuestShareResult> ShareResults(RecordingDispatchProxy<IGameSession> session)
            => session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0])
                .OfType<ServerQuestShareResult>();
    }

    private static void ConfigureExperience(Fixture f, Actor actor, CharacterModel loaded)
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.XpPerLevel), PlayerBindPointTests.Table(
            new XpPerLevelEntry { Id = 1 }, new XpPerLevelEntry { Id = 2, MinXpForLevel = 100 }));
        tables.SetProperty(nameof(IGameTableManager.UnitProperty2), PlayerBindPointTests.Table(
            new UnitProperty2Entry { Id = (uint)Property.BaseHealth, DefaultValue = 6, Flags = UnitPropertyFlags.Static },
            new UnitProperty2Entry { Id = (uint)Property.ShieldCapacityMax, DefaultValue = 7, Flags = UnitPropertyFlags.Static }));
        typeof(Player).GetField("gameTableManager", instance)!.SetValue(actor.Player, f.Source.Tables);
        typeof(WorldEntity).GetField("gameTableManagerResolver", instance)!.SetValue(actor.Player, (Func<IGameTableManager>)(() => f.Source.Tables));
        foreach (var (type, fieldName) in new[] { (typeof(WorldEntity), "properties"), (typeof(WorldEntity), "dirtyProperties"), (typeof(Player), "itemProperties"),
                     (typeof(UnitEntity), "spellProperties"), (typeof(UnitEntity), "recalculatingSpellPropertyConversions") })
        {
            var field = type.GetField(fieldName, instance)!;
            field.SetValue(actor.Player, Activator.CreateInstance(field.FieldType));
        }
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
        var levelProperty = RecordingDispatchProxy<IPropertyModifier>.Create(out var propertyProxy);
        propertyProxy.SetProperty(nameof(IPropertyModifier.Property), FixtureLevelProperty);
        propertyProxy.SetMethodHandler(nameof(IPropertyModifier.GetValue), args => (uint)args[0] * 10f);
        characterProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterBaseProperties), new[] { levelProperty });
        characterProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterClassBaseProperties), Array.Empty<IPropertyModifier>());
        typeof(Player).GetField("characterManager", instance)!.SetValue(actor.Player, characters);
        actor.Player.IsLoading = true;
        actor.Player.Level = loaded.Level;
        actor.Player.SetBaseProperty(FixtureLevelProperty, loaded.Level * 10f);
        actor.Player.SetBaseProperty(Property.ShieldCapacityMax, 15f);
        var stats = (Dictionary<Stat, IStatValue>)typeof(WorldEntity).GetField("stats", instance)!.GetValue(actor.Player)!;
        foreach (CharacterStatModel stat in loaded.Stat.Where(row => (Stat)row.Stat is Stat.Health or Stat.Shield))
            stats.Add((Stat)stat.Stat, new StatValue(stat));
        typeof(WorldEntity).GetMethod("SetDependantStatBalance", instance)!.Invoke(actor.Player, [true]);
        typeof(Player).GetField("attributePointAllocations", instance)!.SetValue(actor.Player, new uint[6]);
        actor.Player.IsLoading = false;
        ((RecordingDispatchProxy<IAccount>)(object)actor.Player.Account).SetProperty(nameof(IAccount.RbacManager),
            RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
        actor.Set(nameof(Player.XpManager), new XpManager(actor.Player, loaded, f.Source.Tables));
        actor.PublisherProxy.Invocations.Clear();
    }

    private static RecordingDispatchProxy<IWorldSession> CreateLateObserver(Player source)
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var observer = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), observer);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(observer, session);
        typeof(Player).GetProperty(nameof(Player.Account))!.SetValue(observer, account);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Guid))!.SetValue(observer, 72u);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(observer, source.Map);
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity).GetField("visibleEntities", instance)!.GetValue(source)!;
        visible.Add(observer.Guid, observer);

        var movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetNetworkEntityCommands), Array.Empty<INetworkEntityCommand>());
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.MovementManager))!.SetValue(source, movement);
        var visuals = typeof(WorldEntity).GetField("itemVisuals", instance)!;
        visuals.SetValue(source, Activator.CreateInstance(visuals.FieldType));
        ((RecordingDispatchProxy<IAppearanceManager>)(object)source.AppearanceManager)
            .SetMethodReturn(nameof(IAppearanceManager.GetBones), Array.Empty<IBone>());
        ((RecordingDispatchProxy<IGuildManager>)(object)source.GuildManager)
            .SetMethodReturnFactory("GetEnumerator", () => Enumerable.Empty<IGuildBase>().GetEnumerator());

        typeof(Player).GetMethod("SendVisibleEntityCreate", instance)!.Invoke(observer, [source, false]);
        return sessionProxy;
    }

    private static RecordingDispatchProxy<IWorldSession> AddObserver(Player source, IBaseMap map, uint guid,
        out RecordingDispatchProxy<IAccount> accountProxy)
    {
        var recipient = RecordingDispatchProxy<IPlayer>.Create(out var recipientProxy);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var account = RecordingDispatchProxy<IAccount>.Create(out accountProxy);
        accountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), recipient);
        recipientProxy.SetProperty(nameof(IPlayer.Guid), guid);
        recipientProxy.SetProperty(nameof(IPlayer.Session), session);
        recipientProxy.SetProperty(nameof(IPlayer.Account), account);
        recipientProxy.SetProperty(nameof(IPlayer.Map), map);
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity)
            .GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source)!;
        visible.Add(guid, recipient);
        return sessionProxy;
    }

    private static void ConfigureFanfare(Player player, IScriptManager scripts)
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        const uint fanfareBaseId = 53378;
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy);
        baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = fanfareBaseId });
        var spell = RecordingDispatchProxy<ISpellInfo>.Create(out var spellProxy);
        spellProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = FixtureFanfareSpellId, Spell4BaseIdBaseSpell = fanfareBaseId, CastTime = 1000 });
        spellProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        baseProxy.SetMethodHandler(nameof(ISpellBaseInfo.GetSpellInfo), args => (byte)args[0] == 1 ? spell : null);
        var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalProxy);
        globalProxy.SetMethodHandler(nameof(IGlobalSpellManager.GetSpellBaseInfo), args => (uint)args[0] == fanfareBaseId ? baseInfo : null);
        globalProxy.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 99u);
        typeof(UnitEntity).GetField("globalSpellManagerResolver", instance)!.SetValue(player, (Func<IGlobalSpellManager>)(() => global));
        typeof(GridEntity).GetField("scriptManagerResolver", instance)!.SetValue(player, (Func<IScriptManager>)(() => scripts));
        foreach (string name in new[] { "procStates", "ccStates" })
        {
            var field = typeof(UnitEntity).GetField(name, instance)!;
            field.SetValue(player, Activator.CreateInstance(field.FieldType));
        }
    }

    private static IEnumerable<ServerSpellStart> FanfarePackets(RecordingDispatchProxy<IWorldSession> observer)
        => observer.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).SelectMany(call => call.Arguments)
            .OfType<ServerSpellStart>().Where(packet => packet.Spell4Id == FixtureFanfareSpellId);

    private static IEnumerable<ServerEntityStatUpdateInteger> LevelPackets(RecordingDispatchProxy<IWorldSession> observer)
        => observer.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).SelectMany(call => call.Arguments)
            .OfType<ServerEntityStatUpdateInteger>().Where(packet => packet.Stat.Stat == Stat.Level);

    private static IEnumerable<PlayerStatUpdatedMessage> LevelMessages(Actor actor)
        => actor.PublisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)).SelectMany(call => call.Arguments)
            .OfType<PlayerStatUpdatedMessage>().Where(message => message.Stat == Stat.Level);

    private static IEnumerable<PlayerStatUpdatedMessage> VitalMessages(Actor actor)
        => actor.PublisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)).SelectMany(call => call.Arguments)
            .OfType<PlayerStatUpdatedMessage>().Where(message => message.Stat is Stat.Health or Stat.Shield);

    private static IEnumerable<ServerEntityHealthUpdate> HealthPackets(RecordingDispatchProxy<IWorldSession> observer)
        => observer.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).SelectMany(call => call.Arguments)
            .OfType<ServerEntityHealthUpdate>();

    private static IEnumerable<ServerEntityStatUpdateInteger> ShieldPackets(RecordingDispatchProxy<IWorldSession> observer)
        => observer.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).SelectMany(call => call.Arguments)
            .OfType<ServerEntityStatUpdateInteger>().Where(packet => packet.Stat.Stat == Stat.Shield);

    private static void AssertNoVitals(RecordingDispatchProxy<IWorldSession> observer, Actor actor)
    {
        Assert.Empty(HealthPackets(observer));
        Assert.Empty(ShieldPackets(observer));
        Assert.Empty(VitalMessages(actor));
    }

    private static IEnumerable<ServerEntityPropertiesUpdate> PropertyPackets(RecordingDispatchProxy<IWorldSession> observer)
        => observer.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).SelectMany(call => call.Arguments)
            .OfType<ServerEntityPropertiesUpdate>();

    private static IEnumerable<PlayerPropertyUpdatedMessage> PropertyMessages(Actor actor)
        => actor.PublisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)).SelectMany(call => call.Arguments)
            .OfType<PlayerPropertyUpdatedMessage>();

    private static void FlushPropertyUpdates(Actor actor)
        => typeof(WorldEntity).GetMethod("FlushPropertyUpdates", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(actor.Player, null);

    private async Task AssertQuestPacketsWithheld(WorldSession world, GameProtocolClient client)
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
                or GameMessageOpcode.ServerItemDelete or GameMessageOpcode.ServerItemStackCountUpdate
                or GameMessageOpcode.ServerCombatReward or GameMessageOpcode.ServerReputationUpdate
                or GameMessageOpcode.ServerAchievementUpdate or GameMessageOpcode.ServerDialogEnd
                or GameMessageOpcode.ServerExperienceGained or GameMessageOpcode.ServerEntityStatUpdateInteger
                or GameMessageOpcode.ServerSpellStart or GameMessageOpcode.ServerEntityPropertiesUpdate
                or GameMessageOpcode.ServerEntityHealthUpdate, $"Premature {frame.Opcode}");
        }
        output.WriteLine("No item, quest-state, achievement or dialog-end packets while the quest save was held.");
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
