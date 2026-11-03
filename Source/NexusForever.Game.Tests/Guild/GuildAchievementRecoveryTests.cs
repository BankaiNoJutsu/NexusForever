using NexusForever.Game.Achievement;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Network.World.Message.Model.Achievement;
using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Network.World.Message.Model.Guild;
using GameGuild = NexusForever.Game.Guild.Guild;

namespace NexusForever.Game.Tests.Guild;

public sealed class GuildAchievementRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-guild-achievements-{Guid.NewGuid():N}.sqlite");
    private readonly List<string> extraFiles = [];
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task DirectAchievementSaveCanPersistAcceptedProgress()
    {
        using Fixture f = await Create(); Progress(f, 2);
        using (var direct = Context()) { f.Guild.Save(direct); await direct.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
    }

    [Fact]
    public async Task AchievementOnlyPublicProgressReachesThePeriodicOwner()
    {
        using Fixture f = await Create();
        f.Guild.AchievementManager.CheckAchievements(f.Leader, AchievementType.KillCreatureEntry, 500, count: 2);
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
    }

    [Fact]
    public async Task FailedDirectSaveRetainsTheAcceptedAchievementCreate()
    {
        using Fixture f = await Create(); Progress(f, 2);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_achievement BEFORE INSERT ON guild_achievement BEGIN SELECT RAISE(ABORT, 'owned guild achievement failure'); END");
        using (var failed = Context())
        {
            f.Guild.Save(failed); await Assert.ThrowsAsync<DbUpdateException>(() => failed.SaveChangesAsync());
        }
        using (var setup = Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_achievement");
        using (var retry = Context()) { f.Guild.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
    }

    [Fact]
    public async Task RollbackRetainsCreationAndLaterProgressWithSiblingGuildData()
    {
        using Fixture f = await Create(); Progress(f, 2);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                Progress(f, 5); f.Guild.MessageOfTheDay = "Later MOTD";
                await transaction.RollbackAsync();
            });
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(5u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
        Assert.Equal("Later MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
    }

    [Fact]
    public async Task RealmFirstAnnouncementWaitsForTheCompletedGuildRow()
    {
        using Fixture f = await Create(); f.Guild.AchievementManager.GrantAchievement(74);
        Assert.Empty(SessionMessages(f).OfType<ServerRealmFirstAchievement>());
        await Flush(f.Manager);
        using var verify = Context(); GuildAchievementModel row = await verify.GuildAchievement.SingleAsync();
        Assert.Equal((ushort)74, row.AchievementId); Assert.NotNull(row.DateCompleted);
        ServerRealmFirstAchievement notice = Assert.Single(SessionMessages(f).OfType<ServerRealmFirstAchievement>());
        Assert.Equal((ushort)74, notice.AchievementId); Assert.Equal("Guild", notice.Name); Assert.True(notice.IsGuildAchievement);
    }

    [Fact]
    public async Task AFailedNewParentDoesNotConsumeItsInitialAchievement()
    {
        using Fixture f = await Create();
        var created = (GameGuild)f.Manager.RegisterGuild(GuildType.Guild, "New parent", "Leader", "Council", "Member",
            new NexusForever.Game.Guild.GuildStandard(1, 2, 3, f.Tables));
        created.AchievementManager.SetAchievementProgress(null, AchievementType.KillCreatureEntry, 500, 0, 2);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_initial_achievement BEFORE INSERT ON guild_achievement BEGIN SELECT RAISE(ABORT, 'owned initial achievement failure'); END");
        await Flush(f.Manager);
        Assert.True(created.PendingCreate);
        using (var setup = Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_initial_achievement");
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync(row => row.Id == created.Id)).ProgressState);
        Assert.False(created.PendingCreate);
    }

    [Fact]
    public async Task ProgressIsPublishedOnlyAfterCommitAndInitialPacketsNeverExposeLaterIntent()
    {
        using Fixture f = await Create(); Progress(f, 2);
        Assert.Empty(SessionMessages(f).OfType<ServerAchievementUpdate>());
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync(); Progress(f, 5);
                f.Guild.AchievementManager.SendInitialPackets(f.Leader);
                Assert.Empty(Assert.Single(SessionMessages(f).OfType<ServerAchievementInit>()).Achievements);
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                Assert.Equal(2u, (await staged.GuildAchievement.AsNoTracking().SingleAsync()).ProgressState);
                await transaction.CommitAsync();
            });
        Assert.True(f.Guild.NeedsSave);
        await Flush(f.Manager);
        var updates = SessionMessages(f).OfType<ServerAchievementUpdate>().SelectMany(packet => packet.Achievements).ToArray();
        Assert.Equal(new uint[] { 2, 5 }, updates.Select(row => row.ProgressState));
        f.Guild.AchievementManager.SendInitialPackets(f.Leader);
        Assert.Equal(5u, Assert.Single(SessionMessages(f).OfType<ServerAchievementInit>().Last().Achievements).ProgressState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectBoundRowOrManagerSavesTheirCompleteParent(bool rowSave)
    {
        using Fixture f = await Create(); Progress(f, 2); f.Guild.MessageOfTheDay = "Bound sibling";
        using (var direct = Context())
        {
            if (rowSave) Row(f, 73).Save(direct); else f.Guild.AchievementManager.Save(direct);
            await direct.SaveChangesAsync();
        }
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
        Assert.Equal("Bound sibling", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.True(f.Guild.NeedsSave); // SQL is clean; committed publication is still pending.
        await Flush(f.Manager); Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task BoundAchievementSaveRejectsAnIdenticalForeignDatabase()
    {
        using Fixture f = await Create(); Progress(f, 2);
        string foreignFile = file + ".foreign"; extraFiles.Add(foreignFile);
        var config = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" };
        var original = new CharacterDatabase(); original.Initialise(Config);
        using (var foreign = new CharacterContext(config))
        {
            await foreign.Database.EnsureCreatedAsync();
            foreign.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
            foreign.Guild.Add(original.GetGuilds().Single()); await foreign.SaveChangesAsync();
            Assert.Throws<InvalidOperationException>(() => Row(f, 73).Save(foreign)); Assert.True(foreign.IsSaveRejected);
            Assert.Empty(await foreign.GuildAchievement.AsNoTracking().ToListAsync());
        }
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
    }

    [Fact]
    public async Task LostAcknowledgementRetainsCompletionAndPublishesTheOriginalWinningName()
    {
        using Fixture f = await Create(); f.Guild.AchievementManager.GrantAchievement(74);
        f.Guild.MessageOfTheDay = "Owned mixed acknowledgement loss";
        using (var fault = new FaultContext(Config, new LostAcknowledgement()))
        {
            f.Guild.Save(fault); await Assert.ThrowsAsync<IOException>(() => fault.SaveChangesAsync());
        }
        Assert.Empty(SessionMessages(f).OfType<ServerRealmFirstAchievement>());
        f.Guild.Name = "Later guild"; Progress(f, 5);
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2, await verify.GuildAchievement.CountAsync());
        Assert.Equal("Later guild", (await verify.Guild.SingleAsync()).Name);
        Assert.Equal("Guild", Assert.Single(SessionMessages(f).OfType<ServerRealmFirstAchievement>()).Name);
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task FailedAnnouncementRetriesAnImmutablePayloadWithoutRepeatingAcknowledgedUpdates()
    {
        using Fixture f = await Create(); bool fail = true;
        f.Session.SetMethodHandler(nameof(NexusForever.WorldServer.Network.IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerRealmFirstAchievement packet && fail)
            {
                packet.Name = "Receiver mutated"; packet.AchievementId = 3;
                throw new IOException("owned announcement failure");
            }
            return null;
        });
        f.Guild.AchievementManager.GrantAchievement(74); await Flush(f.Manager);
        Assert.True(f.Guild.NeedsSave);
        Assert.Single(SessionMessages(f).OfType<ServerAchievementUpdate>());
        f.Guild.Name = "Renamed after announcement"; fail = false; await Flush(f.Manager);
        var notices = SessionMessages(f).OfType<ServerRealmFirstAchievement>().ToArray();
        Assert.Equal(2, notices.Length); Assert.Equal("Guild", notices[1].Name); Assert.Equal((ushort)74, notices[1].AchievementId);
        Assert.Single(SessionMessages(f).OfType<ServerAchievementUpdate>()); Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task PublicationFailureRetainsTheExactEarlierProgressBeforeLaterProgress()
    {
        using Fixture f = await Create(); bool fail = true;
        f.Session.SetMethodHandler(nameof(NexusForever.WorldServer.Network.IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAchievementUpdate packet && fail)
            {
                packet.Achievements[0].ProgressState = 999;
                throw new IOException("owned progress publication failure");
            }
            return null;
        });
        Progress(f, 2); await Flush(f.Manager); Progress(f, 5); fail = false;
        f.Guild.AchievementManager.SendInitialPackets(f.Leader);
        Assert.Empty(Assert.Single(SessionMessages(f).OfType<ServerAchievementInit>()).Achievements);
        await Flush(f.Manager);
        var updates = SessionMessages(f).OfType<ServerAchievementUpdate>().SelectMany(packet => packet.Achievements).ToArray();
        Assert.Equal(new uint[] { 999, 2, 5 }, updates.Select(row => row.ProgressState));
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task LateForeignSqlAndTrackedAchievementRowsRejectTheWholeGuildUnit()
    {
        using Fixture f = await Create(); Progress(f, 2);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                await staged.Database.ExecuteSqlRawAsync("UPDATE guild_achievement SET data0=8 WHERE id=77 AND achievementId=73");
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        using (var staged = Context())
        {
            staged.GuildAchievement.Add(new GuildAchievementModel { Id = 77, AchievementId = 74, ProgressState = 1 });
            f.Guild.Save(staged); await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
        }
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
    }

    [Fact]
    public async Task AcceptedAchievementsCommitBeforeDisbandCleanupAndCleanupRejectsFurtherProgress()
    {
        using Fixture f = await Create(); Progress(f, 2); f.Guild.DisbandGuild();
        f.Guild.AchievementManager.GrantAchievement(74);
        bool sawCleanup = false;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            if (args[0] is GuildDisbandedMessage)
            {
                sawCleanup = true;
                Assert.Throws<InvalidOperationException>(() => Progress(f, 5));
                Assert.Single(SessionMessages(f).OfType<ServerRealmFirstAchievement>());
            }
            return Task.CompletedTask;
        });
        await Flush(f.Manager);
        Assert.True(sawCleanup); Assert.Null(f.Manager.GetGuild(77));
        using var verify = Context(); Assert.Equal(2, await verify.GuildAchievement.CountAsync());
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
        Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync(row => row.AchievementId == 73)).ProgressState);
    }

    [Fact]
    public async Task LoadedRawProgressAndCompletionRemainUnchangedAcrossSiblingSaves()
    {
        DateTime completed = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567);
        using Fixture f = await Create(new GuildAchievementModel { Id = 77, AchievementId = 73, ProgressState = 0x80000005, CreditedChecklistMask = 0x80000001, DateCompleted = completed });
        f.Guild.MessageOfTheDay = "Sibling only"; await Flush(f.Manager);
        using var verify = Context(); GuildAchievementModel stored = await verify.GuildAchievement.SingleAsync();
        Assert.Equal(0x80000005u, stored.ProgressState); Assert.Equal(0x80000001u, stored.CreditedChecklistMask);
        Assert.Equal(completed, stored.DateCompleted);
        Assert.Empty(SessionMessages(f).OfType<ServerAchievementUpdate>());
    }

    [Fact]
    public async Task ADetachedRowOrReplacedManagerCannotSaveTheOriginalGuild()
    {
        using Fixture f = await Create(); Progress(f, 2); IAchievement row = Row(f, 73);
        var values = Rows(f); values.Remove(73);
        using (var orphan = Context()) Assert.Throws<InvalidOperationException>(() => row.Save(orphan));
        values.Add(73, row);
        IGuildAchievementManager original = f.Guild.AchievementManager;
        typeof(GameGuild).GetProperty(nameof(GameGuild.AchievementManager))!.SetValue(f.Guild, new GuildAchievementManager(f.Guild, globalAchievementManager: f.Global));
        using (var replaced = Context()) Assert.Throws<InvalidOperationException>(() => original.Save(replaced));
        using (var replaced = Context()) Assert.Throws<InvalidOperationException>(() => f.Guild.AchievementManager.Save(replaced));
        using (var replaced = Context()) Assert.Throws<InvalidOperationException>(() => f.Guild.Save(replaced));
        typeof(GameGuild).GetProperty(nameof(GameGuild.AchievementManager))!.SetValue(f.Guild, original);
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReservedRealmFirstSurvivesAcceptedCompletionTimestampEdits(bool clearBeforeSave)
    {
        using Fixture f = await Create(); f.Guild.AchievementManager.GrantAchievement(74);
        IAchievement row = Row(f, 74);
        DateTime later = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        row.DateCompleted = clearBeforeSave ? null : later;
        await Flush(f.Manager);
        if (clearBeforeSave)
        {
            Assert.Empty(SessionMessages(f).OfType<ServerRealmFirstAchievement>());
            row.DateCompleted = later; await Flush(f.Manager);
        }
        using var verify = Context(); Assert.Equal(later, (await verify.GuildAchievement.SingleAsync()).DateCompleted);
        Assert.Equal("Guild", Assert.Single(SessionMessages(f).OfType<ServerRealmFirstAchievement>()).Name);
    }

    [Fact]
    public async Task LateManagerReplacementCannotCommitAnUnacknowledgeableGuildUnit()
    {
        using Fixture f = await Create(); Progress(f, 2);
        IGuildAchievementManager original = f.Guild.AchievementManager;
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                typeof(GameGuild).GetProperty(nameof(GameGuild.AchievementManager))!.SetValue(f.Guild,
                    new GuildAchievementManager(f.Guild, globalAchievementManager: f.Global));
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        typeof(GameGuild).GetProperty(nameof(GameGuild.AchievementManager))!.SetValue(f.Guild, original);
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2u, (await verify.GuildAchievement.SingleAsync()).ProgressState);
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public void CharacterAchievementDelegationAllowsConcurrentOwnerReads()
    {
        var row = new Achievement<CharacterAchievementModel>(42, new AchievementInfo(new AchievementEntry { Id = 73, RequiredProgress = 10 }));
        row.ProgressCount = 2;
        typeof(Achievement<CharacterAchievementModel>).GetProperty("CharacterSaveOwner", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(row, new Func<CharacterContext, bool>(_ =>
            {
                // The owner's save may capture achievements while another caller
                // invokes a bound row save. Its callback must not hold a row lock.
                uint progress = Task.Run(() => row.ProgressCount).WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                Assert.Equal(2u, progress); return true;
            }));
        using var context = Context(); row.Save(context); Assert.Empty(context.ChangeTracker.Entries());
    }

    private static Dictionary<ushort, IAchievement> Rows(Fixture f) => (Dictionary<ushort, IAchievement>)typeof(BaseAchievementManager<GuildAchievementModel>)
        .GetField("achievements", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Guild.AchievementManager);
    private static IAchievement Row(Fixture f, ushort id) => Rows(f)[id];
    private sealed class FaultContext(DatabaseConnectionString config, IInterceptor interceptor) : CharacterContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { base.OnConfiguring(builder); builder.AddInterceptors(interceptor); }
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("owned guild achievement acknowledgement loss"));
    }

    private static void Progress(Fixture f, uint value) => f.Guild.AchievementManager.SetAchievementProgress(f.Leader, AchievementType.KillCreatureEntry, 500, 0, value);
    private static object[] SessionMessages(Fixture f) => f.Session.GetInvocations(nameof(NexusForever.WorldServer.Network.IWorldSession.EnqueueMessageEncrypted))
        .Select(call => call.Arguments[0]).ToArray();

    private static Task WithConfiguredStrategy(CharacterContext context, Func<Task> action)
    {
        bool entered = false;
        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (entered) throw new InvalidOperationException("The guild fixture cannot replay gameplay callbacks.");
            entered = true; await action();
        });
    }

    private async Task<Fixture> Create(params GuildAchievementModel[] loaded)
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
            seed.Guild.Add(new GuildModel
            {
                Id = 77, Type = (byte)GuildType.Guild, Name = "Guild", LeaderId = 42,
                Achievement = loaded.ToList(),
                GuildData = new GuildDataModel { Id = 77, MessageOfTheDay = "Old MOTD", AdditionalInfo = "Old info", BackgroundIconPartId = 1, ForegroundIconPartId = 2, ScanLinesPartId = 3 },
                GuildRank = [new GuildRankModel { Id = 77, Index = 0, Name = "Leader", Permission = (uint)GuildRankPermission.Leader },
                    new GuildRankModel { Id = 77, Index = 2, Name = "Unused" }, new GuildRankModel { Id = 77, Index = 9, Name = "Member" }],
                GuildMember = [new GuildMemberModel { Id = 77, CharacterId = 42, Rank = 0, Note = "Leader note" },
                    new GuildMemberModel { Id = 77, CharacterId = 43, Rank = 9, Note = "Other note" }]
            });
            await seed.SaveChangesAsync();
        }
        var database = new CharacterDatabase(); database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages); messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GuildStandardPart), Parts());
        var achievementTable = (GameTable<AchievementEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<AchievementEntry>));
        typeof(GameTable<AchievementEntry>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(achievementTable,
            new[] { new AchievementEntry { Id = 73, Flags = (uint)AchievementFlags.Guild, AchievementTypeId = (uint)AchievementType.KillCreatureEntry, ObjectId = 500, RequiredProgress = 10, AchievementPointEnum = 1 },
                new AchievementEntry { Id = 74, Flags = (uint)AchievementFlags.Guild, AchievementTypeId = (uint)AchievementType.RealmFirst, RequiredProgress = 1, AchievementPointEnum = 1 } });
        tableProxy.SetProperty(nameof(IGameTableManager.Achievement), achievementTable);
        var globalAchievements = new GlobalAchievementManager(databases, tables); globalAchievements.Initialise();
        IPlayer[] online = [];
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] is ulong id ? online.SingleOrDefault(player => player.CharacterId == id) : null);
        playerManagerProxy.SetMethodHandler(nameof(IEnumerable<IPlayer>.GetEnumerator), _ => ((IEnumerable<IPlayer>)online).GetEnumerator());
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
        characterProxy.SetMethodHandler(nameof(ICharacterManager.GetCharacterIdByName), args => (string)args[0] == "Other" ? (ulong?)43 : 42ul);
        characterProxy.SetMethodHandler(nameof(ICharacterManager.GetCharacter), args =>
        {
            var character = RecordingDispatchProxy<ICharacter>.Create(out var value);
            value.SetProperty(nameof(ICharacter.Name), (ulong)args[0] == 42 ? "Leader" : "Other");
            return character;
        });
        var registrations = new ServiceCollection();
        registrations.AddSingleton<IGuildFactory, GuildFactory>();
        registrations.AddSingleton<IGlobalGuildManager>(provider => new GlobalGuildManager(provider.GetRequiredService<IGuildFactory>(), databases));
        registrations.AddKeyedTransient<IGuildBase>(GuildType.Guild, (provider, _) => new GameGuild(realm, publisher, filter,
            characterManager: characters, gameTableManager: tables, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>(), playerManager: players, globalAchievementManager: globalAchievements));
        var services = registrations.BuildServiceProvider();
        var manager = (GlobalGuildManager)services.GetRequiredService<IGlobalGuildManager>(); manager.Initialise();
        IPlayer Player(ulong id)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
            proxy.SetProperty(nameof(IPlayer.CharacterId), id); proxy.SetProperty(nameof(IPlayer.Name), id == 42 ? "Leader" : "Other");
            proxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = id, RealmId = 1 });
            var guilds = RecordingDispatchProxy<IGuildManager>.Create(out var guildProxy);
            guildProxy.SetProperty(nameof(IGuildManager.GuildAffiliation), manager.GetGuild(77));
            proxy.SetProperty(nameof(IPlayer.GuildManager), guilds);
            proxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession>.Create(out _));
            proxy.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
            return player;
        }
        IPlayer leader = Player(42), other = Player(43); online = [leader, other];
        var guild = Assert.IsType<GameGuild>(manager.GetGuild(77)); guild.OnPlayerLogin(leader);
        return new(services, manager, guild, leader, other, messages, (RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession>)(object)leader.Session, globalAchievements, tables);
    }

    private static GameTable<GuildStandardPartEntry> Parts()
    {
        var table = (GameTable<GuildStandardPartEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<GuildStandardPartEntry>));
        var rows = new[] { new GuildStandardPartEntry { Id = 1 }, new GuildStandardPartEntry { Id = 2 }, new GuildStandardPartEntry { Id = 3 } };
        typeof(GameTable<GuildStandardPartEntry>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, rows);
        typeof(GameTable<GuildStandardPartEntry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = 4 });
        typeof(GameTable<GuildStandardPartEntry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new[] { -1, 0, 1, 2 });
        return table;
    }

    private static async Task Flush(GlobalGuildManager manager)
    {
        manager.Update(61);
        object gate = typeof(GlobalGuildManager).GetField("cacheLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var active = (HashSet<ulong>)typeof(GlobalGuildManager).GetField("guildSavesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++) { lock (gate) if (active.Count == 0) return; await Task.Delay(10); }
        Assert.Fail("Periodic guild save did not settle.");
    }
    private sealed record Fixture(ServiceProvider Services, GlobalGuildManager Manager, GameGuild Guild, IPlayer Leader, IPlayer Other, RecordingDispatchProxy<IInternalMessagePublisher> Messages, RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession> Session, GlobalAchievementManager Global, IGameTableManager Tables) : IDisposable
    { public void Dispose() => Services.Dispose(); }
    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
        foreach (string extra in extraFiles)
        {
            using var foreignPool = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={extra};Foreign Keys=True");
            Microsoft.Data.Sqlite.SqliteConnection.ClearPool(foreignPool);
            if (File.Exists(extra)) File.Delete(extra);
        }
    }
}
