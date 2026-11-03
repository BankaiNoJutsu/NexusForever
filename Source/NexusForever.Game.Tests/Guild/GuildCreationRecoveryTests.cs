using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Game.Abstract.Housing;
using System.Reflection;
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

public sealed class GuildCreationRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-guild-create-{Guid.NewGuid():N}.sqlite");
    private readonly List<string> extraFiles = [];
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task DirectFirstCreateWithoutEditsCanCommitACompleteGraph()
    {
        using Fixture f = await Create();
        using (var direct = Context()) { f.Guild.Save(direct); await direct.SaveChangesAsync(); }
        using var verify = Context();
        Assert.Equal(f.Guild.Id, (await verify.Guild.SingleAsync()).Id);
        Assert.Equal(3, await verify.GuildRank.CountAsync());
        Assert.Equal(42ul, (await verify.GuildMember.SingleAsync()).CharacterId);
        Assert.Single(await verify.GuildData.ToListAsync());
        Assert.False(f.Guild.PendingCreate);
    }

    [Fact]
    public async Task RegistrationPublishesOnlyAfterTheCompleteCreateCommits()
    {
        using Fixture f = await Create();
        Assert.Empty(Publications(f));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal(f.Guild.Id, (await verify.Guild.SingleAsync()).Id);
        Assert.Equal(3, await verify.GuildRank.CountAsync());
        Assert.Equal(42ul, (await verify.GuildMember.SingleAsync()).CharacterId);
        Assert.Single(await verify.GuildData.ToListAsync());
        Assert.Collection(Publications(f), value => Assert.IsType<GuildCreatedMessage>(value), value => Assert.IsType<GuildMemberAddedMessage>(value));
        Assert.False(f.Guild.PendingCreate); Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task FailedPeriodicFirstCreateRetainsTheOriginalCompleteGraph()
    {
        using Fixture f = await Create();
        var guild = Assert.IsType<GameGuild>(f.Guild);
        guild.SetTaxes(true); guild.MessageOfTheDay = "Accepted MOTD";
        IGuildMember original = guild.GetMember(42); original.Note = "Accepted note";
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_create BEFORE INSERT ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned creation failure'); END");
        await Flush(f.Manager);
        Assert.True(guild.PendingCreate); Assert.True(guild.NeedsSave);
        using (var failed = Context())
        {
            Assert.Empty(await failed.Guild.ToListAsync());
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_create");
        }
        Assert.Same(guild, f.Manager.GetGuild(GuildType.Guild, "Created guild"));
        Assert.Same(original, guild.GetMember(42));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Accepted MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Accepted note", (await verify.GuildMember.SingleAsync()).Note);
        Assert.True(((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Assert.Equal(3, await verify.GuildRank.CountAsync()); Assert.False(guild.PendingCreate);
    }

    [Fact]
    public async Task CallerRollbackRetainsCreationAndItsLaterAcceptedEdits()
    {
        using Fixture f = await Create();
        var guild = Assert.IsType<GameGuild>(f.Guild);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                guild.Save(staged); await staged.SaveChangesAsync();
                guild.MessageOfTheDay = "Later MOTD"; guild.GetMember(42).Note = "Later note";
                await transaction.RollbackAsync();
            });
        Assert.True(guild.PendingCreate); Assert.True(guild.NeedsSave);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Later MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Later note", (await verify.GuildMember.SingleAsync()).Note);
        Assert.Equal(3, await verify.GuildRank.CountAsync());
        Assert.Single(Publications(f).OfType<GuildCreatedMessage>());
    }

    [Fact]
    public async Task BoundInitialMemberSaveCreatesItsCompleteOriginalParent()
    {
        using Fixture f = await Create();
        var guild = Assert.IsType<GameGuild>(f.Guild); guild.MessageOfTheDay = "Direct parent data";
        using (var direct = Context()) { guild.GetMember(42).Save(direct); await direct.SaveChangesAsync(); }
        using var verify = Context();
        Assert.Equal(guild.Id, (await verify.Guild.SingleAsync()).Id);
        Assert.Equal(3, await verify.GuildRank.CountAsync());
        Assert.Equal("Direct parent data", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        await Flush(f.Manager);
        Assert.Single(Publications(f).OfType<GuildCreatedMessage>());
    }

    [Fact]
    public async Task RegisteredCreationRejectsAForeignDatabaseBeforeAnySave()
    {
        using Fixture f = await Create();
        string foreignFile = file + ".foreign"; extraFiles.Add(foreignFile);
        var config = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" };
        using var foreign = new CharacterContext(config); await foreign.Database.EnsureCreatedAsync();
        foreign.Character.Add(new CharacterModel { Id = 42, Name = "Leader" }); await foreign.SaveChangesAsync();
        Exception error = Record.Exception(() => f.Guild.Save(foreign));
        if (error == null) error = await Record.ExceptionAsync(() => foreign.SaveChangesAsync());
        Assert.IsType<InvalidOperationException>(error);
        Assert.Empty(await foreign.Guild.AsNoTracking().ToListAsync());
        Assert.True(f.Guild.PendingCreate);
        await Flush(f.Manager);
        using var verify = Context(); Assert.Single(await verify.Guild.ToListAsync());
    }

    [Theory]
    [InlineData(GuildType.Guild)]
    [InlineData(GuildType.Circle)]
    [InlineData(GuildType.WarParty)]
    [InlineData(GuildType.ArenaTeam2v2)]
    [InlineData(GuildType.ArenaTeam3v3)]
    [InlineData(GuildType.ArenaTeam5v5)]
    [InlineData(GuildType.Community)]
    public async Task RegisteredRootGraphPreservesEveryGuildTypeOnReload(GuildType type)
    {
        using Fixture f = await Create(type);
        await Flush(f.Manager);
        var database = new CharacterDatabase(); database.Initialise(Config);
        GuildModel stored = Assert.Single(database.GetGuilds());
        Assert.Equal((byte)type, stored.Type); Assert.Equal(f.Guild.CreateTime, stored.CreateTime);
        using ServiceProvider reloadServices = f.Registrations.BuildServiceProvider();
        var reloadedManager = (GlobalGuildManager)reloadServices.GetRequiredService<IGlobalGuildManager>();
        reloadedManager.Initialise();
        IGuildBase loaded = reloadedManager.GetGuild(f.Guild.Id);
        Assert.Equal(type, loaded.Type); Assert.Equal(f.Guild.MaxMembers, loaded.MaxMembers);
        Assert.Equal(42ul, loaded.GetMember(42).CharacterId);
        Assert.Equal(3, stored.GuildRank.Count); Assert.Single(stored.GuildMember);
        Assert.Equal(type == GuildType.Guild, stored.GuildData != null);
        Assert.Equal(type, Assert.Single(Publications(f).OfType<GuildCreatedMessage>()).Type);
    }

    [Fact]
    public async Task LaterAcceptedIntentSurvivesTheFirstCommitAndFrozenResave()
    {
        using Fixture f = await Create(); var guild = Assert.IsType<GameGuild>(f.Guild);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                guild.Save(staged);
                guild.Name = "Later guild"; guild.MessageOfTheDay = "Later MOTD";
                guild.GetMember(42).Note = "Later note"; guild.GetMember(42).Rank.Name = "Later leader";
                await staged.SaveChangesAsync(); guild.Save(staged); await staged.SaveChangesAsync();
                Assert.Equal("Created guild", (await staged.Guild.AsNoTracking().SingleAsync()).Name);
                Assert.Equal("", (await staged.GuildData.AsNoTracking().SingleAsync()).MessageOfTheDay);
                await transaction.CommitAsync();
            });
        Assert.False(guild.PendingCreate); Assert.True(guild.NeedsSave);
        Assert.Empty(Publications(f));
        Assert.Same(guild, f.Manager.GetGuild(GuildType.Guild, "Created guild"));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Later guild", (await verify.Guild.SingleAsync()).Name);
        Assert.Equal("Later MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Later note", (await verify.GuildMember.SingleAsync()).Note);
        Assert.Equal("Later leader", (await verify.GuildRank.SingleAsync(row => row.Index == 0)).Name);
        Assert.Null(f.Manager.GetGuild(GuildType.Guild, "Created guild"));
        Assert.Same(guild, f.Manager.GetGuild(GuildType.Guild, "Later guild"));
        Assert.Single(Publications(f).OfType<GuildCreatedMessage>());
    }

    [Fact]
    public async Task LostCreateAcknowledgementResolvesTheOriginalGraphAndLaterEdits()
    {
        using Fixture f = await Create();
        using (var fault = new FaultContext(Config, new LostAcknowledgement()))
        {
            f.Guild.Save(fault); await Assert.ThrowsAsync<IOException>(() => fault.SaveChangesAsync());
        }
        Assert.True(f.Guild.PendingCreate); Assert.Empty(Publications(f));
        using (var committed = Context()) Assert.Single(await committed.Guild.ToListAsync());
        var guild = Assert.IsType<GameGuild>(f.Guild); guild.MessageOfTheDay = "Resolved later"; guild.GetMember(42).Note = "Resolved note";
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Single(await verify.Guild.ToListAsync()); Assert.Equal(3, await verify.GuildRank.CountAsync());
        Assert.Equal("Resolved later", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Resolved note", (await verify.GuildMember.SingleAsync()).Note);
        Assert.Single(Publications(f).OfType<GuildCreatedMessage>()); Assert.Single(Publications(f).OfType<GuildMemberAddedMessage>());
        Assert.False(guild.PendingCreate); Assert.False(guild.NeedsSave);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AttemptedCreationDisbandResolvesSqlBeforeCleanup(bool lostAcknowledgement)
    {
        using Fixture f = await Create(); var guild = Assert.IsType<GameGuild>(f.Guild);
        guild.MessageOfTheDay = "Archive create"; guild.GetMember(42).Note = "Archive note";
        if (lostAcknowledgement)
        {
            using var fault = new FaultContext(Config, new LostAcknowledgement());
            guild.Save(fault); await Assert.ThrowsAsync<IOException>(() => fault.SaveChangesAsync());
        }
        else
        {
            using (var setup = Context())
                await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_attempt BEFORE INSERT ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned attempted create failure'); END");
            await Flush(f.Manager);
            using var retrySetup = Context(); await retrySetup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_attempt");
        }
        Assert.True(guild.PendingCreate); Assert.Empty(Publications(f));
        guild.DisbandGuild();
        await Flush(f.Manager);
        using var verify = Context(); GuildModel row = await verify.Guild.SingleAsync();
        Assert.NotNull(row.DeleteTime); Assert.Null(row.Name); Assert.Null(row.LeaderId);
        Assert.Equal("Created guild", row.OriginalName); Assert.Equal(42ul, row.OriginalLeaderId);
        Assert.Equal("Archive create", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Archive note", (await verify.GuildMember.SingleAsync()).Note);
        Assert.Equal(3, await verify.GuildRank.CountAsync());
        Assert.Null(f.Manager.GetGuild(guild.Id)); Assert.Null(f.Manager.GetGuild(GuildType.Guild, "Created guild"));
        Assert.Single(Publications(f).OfType<GuildMemberRemovedMessage>());
        Assert.Single(Publications(f).OfType<GuildDisbandedMessage>());
    }

    [Fact]
    public async Task AnUnacceptedTrackedMemberCannotJoinTheCreateTransaction()
    {
        using Fixture f = await Create();
        using (var staged = Context())
        {
            staged.GuildMember.Add(new GuildMemberModel { Id = f.Guild.Id, CharacterId = 43, Rank = 9, Note = "Unaccepted" });
            f.Guild.Save(staged);
            await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
        }
        using (var rejected = Context()) Assert.Empty(await rejected.Guild.ToListAsync());
        Assert.Null(f.Guild.GetMember(43)); Assert.Empty(Publications(f));
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(42ul, (await verify.GuildMember.SingleAsync()).CharacterId);
    }

    [Fact]
    public async Task NonGuildCreationCannotAdmitAnUnacceptedGuildDataRow()
    {
        using Fixture f = await Create(GuildType.Circle);
        using (var staged = Context())
        {
            staged.GuildData.Add(new GuildDataModel { Id = f.Guild.Id, MessageOfTheDay = "Unaccepted non-guild data" });
            f.Guild.Save(staged);
            await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
        }
        Assert.True(f.Guild.PendingCreate); Assert.Empty(Publications(f));
        using (var rejected = Context()) { Assert.Empty(await rejected.Guild.ToListAsync()); Assert.Empty(await rejected.GuildData.ToListAsync()); }
        await Flush(f.Manager);
        using var verify = Context(); Assert.Single(await verify.Guild.ToListAsync()); Assert.Empty(await verify.GuildData.ToListAsync());
    }

    [Fact]
    public async Task NonGuildUnknownCreateOutcomeMustProveItsDataAbsence()
    {
        using Fixture f = await Create(GuildType.Circle);
        using (var fault = new FaultContext(Config, new LostAcknowledgement()))
        {
            f.Guild.Save(fault); await Assert.ThrowsAsync<IOException>(() => fault.SaveChangesAsync());
        }
        using (var foreign = Context())
        {
            foreign.GuildData.Add(new GuildDataModel { Id = f.Guild.Id, MessageOfTheDay = "Foreign outcome data" });
            await foreign.SaveChangesAsync();
        }
        using (var retry = Context()) Assert.Throws<InvalidOperationException>(() => f.Guild.Save(retry));
        Assert.True(f.Guild.PendingCreate); Assert.Empty(Publications(f));
        using (var repair = Context())
        {
            Assert.Equal("Foreign outcome data", (await repair.GuildData.SingleAsync()).MessageOfTheDay);
            repair.GuildData.Remove(await repair.GuildData.SingleAsync()); await repair.SaveChangesAsync();
        }
        await Flush(f.Manager);
        Assert.False(f.Guild.PendingCreate); Assert.Single(Publications(f).OfType<GuildCreatedMessage>());
        using var verify = Context(); Assert.Single(await verify.Guild.ToListAsync()); Assert.Empty(await verify.GuildData.ToListAsync());
    }

    [Fact]
    public async Task CreationPublicationRetriesItsImmutableParentBeforeTheInitialMember()
    {
        using Fixture f = await Create(); bool fail = true;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            if (fail && args[0] is GuildCreatedMessage message)
            {
                message.GuildId = 999;
                return Task.FromException(new IOException("owned creation publication failure"));
            }
            return Task.CompletedTask;
        });
        await Flush(f.Manager);
        Assert.False(f.Guild.PendingCreate); Assert.True(f.Guild.NeedsSave);
        Assert.Empty(Publications(f).OfType<GuildMemberAddedMessage>());
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_repeat_parent BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'must not repeat committed parent'); END");
        fail = false; await Flush(f.Manager);
        GuildCreatedMessage[] roots = Publications(f).OfType<GuildCreatedMessage>().ToArray();
        Assert.Equal(2, roots.Length); Assert.Equal(f.Guild.Id, roots[1].GuildId);
        Assert.Single(Publications(f).OfType<GuildMemberAddedMessage>());
        Assert.False(f.Guild.NeedsSave);
        using var verify = Context(); Assert.Single(await verify.Guild.ToListAsync());
    }

    private sealed class FaultContext(DatabaseConnectionString config, IInterceptor interceptor) : CharacterContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder); builder.AddInterceptors(interceptor);
        }
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("owned guild creation commit acknowledgement loss"));
    }

    private static object[] Publications(Fixture f) => f.Messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))
        .Select(call => call.Arguments[0]).Where(value => value is GuildCreatedMessage or GuildMemberAddedMessage or GuildMemberRemovedMessage or GuildDisbandedMessage).ToArray();

    private static Task WithConfiguredStrategy(CharacterContext context, Func<Task> action)
    {
        bool entered = false;
        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (entered) throw new InvalidOperationException("The guild fixture cannot replay gameplay callbacks.");
            entered = true; await action();
        });
    }

    private async Task<Fixture> Create(GuildType type = GuildType.Guild)
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
            await seed.SaveChangesAsync();
        }
        var database = new CharacterDatabase(); database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages); messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GuildStandardPart), Parts());
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
            characterManager: characters, gameTableManager: tables, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        registrations.AddKeyedTransient<IGuildBase>(GuildType.Circle, (provider, _) => new Circle(realm, publisher, filter,
            characterManager: characters, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        registrations.AddKeyedTransient<IGuildBase>(GuildType.WarParty, (provider, _) => new WarParty(realm, publisher, filter,
            characterManager: characters, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        foreach (GuildType arena in new[] { GuildType.ArenaTeam2v2, GuildType.ArenaTeam3v3, GuildType.ArenaTeam5v5 })
            registrations.AddKeyedTransient<IGuildBase>(arena, (provider, _) => new ArenaTeam(realm, publisher, filter,
                characterManager: characters, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        // Root graph control only: real plot/table-backed residence creation is a separate gate.
        var residences = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out _);
        registrations.AddKeyedTransient<IGuildBase>(GuildType.Community, (provider, _) => new Community(realm, publisher, residences, filter, null,
            characterManager: characters, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        var services = registrations.BuildServiceProvider();
        var manager = (GlobalGuildManager)services.GetRequiredService<IGlobalGuildManager>(); manager.Initialise();
        var guild = (GuildBase)manager.RegisterGuild(type, "Created guild", "Leader", "Council", "Member",
            type == GuildType.Guild ? new NexusForever.Game.Guild.GuildStandard(1, 2, 3, tables) : null);
        IPlayer Player(ulong id)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
            proxy.SetProperty(nameof(IPlayer.CharacterId), id); proxy.SetProperty(nameof(IPlayer.Name), id == 42 ? "Leader" : "Other");
            proxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = id, RealmId = 1 });
            var guilds = RecordingDispatchProxy<IGuildManager>.Create(out var guildProxy);
            guildProxy.SetProperty(nameof(IGuildManager.GuildAffiliation), guild);
            proxy.SetProperty(nameof(IPlayer.GuildManager), guilds);
            proxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession>.Create(out _));
            return player;
        }
        IPlayer leader = Player(42); guild.JoinGuild(leader);
        return new(services, manager, guild, leader, Player(43), messages, registrations);
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
    private sealed record Fixture(ServiceProvider Services, GlobalGuildManager Manager, GuildBase Guild, IPlayer Leader, IPlayer Other, RecordingDispatchProxy<IInternalMessagePublisher> Messages, ServiceCollection Registrations) : IDisposable
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
