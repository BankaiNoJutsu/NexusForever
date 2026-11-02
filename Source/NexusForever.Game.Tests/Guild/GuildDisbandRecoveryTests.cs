using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.Game.Tests.Guild;

public sealed class GuildDisbandRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-disband-{Guid.NewGuid():N}.sqlite");
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task PublicDisbandKeepsChatAndNameReservedUntilPeriodicCommit()
    {
        using Fixture f = await Create();
        Disband(f);
        Assert.Empty(DisbandPublications(f));
        Assert.Same(f.Guild, f.Manager.GetGuild(GuildType.Circle, "circle"));
        using (var before = Context()) Assert.Null((await before.Guild.SingleAsync()).DeleteTime);

        await Flush(f.Manager);

        using var after = Context();
        GuildModel row = await after.Guild.SingleAsync();
        Assert.NotNull(row.DeleteTime);
        Assert.Equal("Circle", row.OriginalName);
        Assert.Equal(42ul, row.OriginalLeaderId);
        Assert.Null(row.Name);
        Assert.Null(row.LeaderId);
        Assert.Single(await after.GuildMember.ToListAsync()); // Existing soft-delete archive semantics.
        Assert.Null(f.Manager.GetGuild(77));
        Assert.Null(f.Manager.GetGuild(GuildType.Circle, "CIRCLE"));
        Assert.Equal(2, DisbandPublications(f).Length);
        Assert.IsType<GuildMemberRemovedMessage>(DisbandPublications(f)[0]);
        Assert.IsType<GuildDisbandedMessage>(DisbandPublications(f)[1]);
    }

    [Fact]
    public async Task FailedPeriodicDisbandRetainsTombstoneIntentAndRetriesBeforeReleasingName()
    {
        using Fixture f = await Create();
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_disband BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'owned disband failure'); END");
        Disband(f);
        await Flush(f.Manager);
        using (var failed = Context())
        {
            Assert.Null((await failed.Guild.SingleAsync()).DeleteTime);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_disband");
        }
        Assert.True(f.Guild.PendingDelete);
        Assert.True(f.Guild.NeedsSave);
        Assert.Same(f.Guild, f.Manager.GetGuild(GuildType.Circle, "Circle"));
        Assert.Empty(DisbandPublications(f));

        await Flush(f.Manager);

        using var verify = Context();
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
        Assert.Null(f.Manager.GetGuild(GuildType.Circle, "Circle"));
        Assert.Equal(2, DisbandPublications(f).Length);
    }

    [Fact]
    public async Task CallerRollbackDoesNotConsumeDisbandIntent()
    {
        using Fixture f = await Create();
        Disband(f);
        using (var staged = Context())
        {
            await using var transaction = await staged.Database.BeginTransactionAsync();
            f.Guild.Save(staged);
            await staged.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.True(f.Guild.PendingDelete);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
        Assert.Null(f.Manager.GetGuild(77));
    }

    [Fact]
    public async Task PublicationFailureRetainsOriginalActorAndRetriesOnlyUnacknowledgedMessages()
    {
        using Fixture f = await Create();
        bool fail = true;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
            args[0] is GuildDisbandedMessage && fail ? Task.FromException(new IOException("owned broker failure")) : Task.CompletedTask);
        Disband(f);
        await Flush(f.Manager);
        DateTime deletedAt;
        using (var committed = Context())
        {
            deletedAt = (await committed.Guild.SingleAsync()).DeleteTime!.Value;
            await committed.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_repeat BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'must not repeat committed tombstone'); END");
        }
        Assert.Same(f.Guild, f.Manager.GetGuild(77));
        Assert.Same(f.Guild, f.Manager.GetGuild(GuildType.Circle, "circle"));
        Assert.True(f.Guild.NeedsSave);
        Assert.Single(DisbandPublications(f).OfType<GuildMemberRemovedMessage>());
        fail = false;

        await Flush(f.Manager);

        Assert.Null(f.Manager.GetGuild(77));
        Assert.Single(DisbandPublications(f).OfType<GuildMemberRemovedMessage>());
        Assert.Equal(2, DisbandPublications(f).OfType<GuildDisbandedMessage>().Count());
        Assert.False(f.Guild.NeedsSave);
        using var verify = Context();
        Assert.Equal(deletedAt, (await verify.Guild.SingleAsync()).DeleteTime);
    }

    [Fact]
    public async Task ConfirmedDirectSaveDefersPublicationToTimerAndPreservesAcceptedTombstone()
    {
        using Fixture f = await Create();
        Disband(f);
        DateTime deletedAt;
        using (var direct = Context())
        {
            f.Guild.Save(direct);
            await direct.SaveChangesAsync();
            deletedAt = (await direct.Guild.SingleAsync()).DeleteTime!.Value;
        }
        f.Guild.Name = "Later raw edit";
        f.Guild.LeaderId = null;
        Assert.Empty(DisbandPublications(f));
        await Flush(f.Manager);
        using var verify = Context();
        GuildModel row = await verify.Guild.SingleAsync();
        Assert.Equal("Circle", row.OriginalName);
        Assert.Equal(42ul, row.OriginalLeaderId);
        Assert.Equal(deletedAt, row.DeleteTime);
        Assert.Null(f.Manager.GetGuild(GuildType.Circle, "Circle"));
        Assert.Equal(2, DisbandPublications(f).Length);
    }

    [Fact]
    public async Task DisbandFreesTheOriginallyIndexedNameAfterAnAcceptedRename()
    {
        using Fixture f = await Create();
        f.Guild.RenameGuild("Renamed");
        Disband(f);
        await Flush(f.Manager);
        Circle replacement = Assert.IsType<Circle>(f.Manager.RegisterGuild(GuildType.Circle, "Circle", "Leader", "Council", "Member"));
        Assert.NotEqual(f.Guild.Id, replacement.Id);
        Assert.Same(replacement, f.Manager.GetGuild(GuildType.Circle, "circle"));
        using var verify = Context();
        Assert.Equal("Renamed", (await verify.Guild.SingleAsync()).OriginalName);
    }

    [Fact]
    public async Task CreateThenDisbandCancelsWithoutCreatingAnArchivedDatabaseGuild()
    {
        using Fixture f = await Create();
        var temporary = Assert.IsType<Circle>(f.Manager.RegisterGuild(GuildType.Circle, "Temporary", "Leader", "Council", "Member"));
        temporary.DisbandGuild();
        temporary.DisbandGuild();
        Assert.Empty(DisbandPublications(f));
        await Flush(f.Manager);
        Assert.Null(f.Manager.GetGuild(temporary.Id));
        Assert.Null(f.Manager.GetGuild(GuildType.Circle, "Temporary"));
        Assert.Same(f.Guild, f.Manager.GetGuild(77));
        Assert.Single(DisbandPublications(f).OfType<GuildDisbandedMessage>());
        using var verify = Context();
        Assert.Single(await verify.Guild.ToListAsync());
    }

    [Fact]
    public async Task RepeatedDisbandDoesNotRepeatMemberSideEffectsOrAdmitNewMembers()
    {
        using Fixture f = await Create();
        Disband(f);
        f.Guild.DisbandGuild();
        Assert.Empty(f.Manager.GetCharacterGuilds(42));
        Assert.Equal(GuildResult.NotAGuild, f.Guild.CanJoinGuild(f.Player).Result);
        await Flush(f.Manager);
        await Flush(f.Manager);
        Assert.Equal(2, DisbandPublications(f).Length);
    }

    [Fact]
    public async Task CommitWithoutFlushCannotQualifyTheDisbandTombstone()
    {
        using Fixture f = await Create();
        Disband(f);
        using (var staged = Context())
        {
            await using var transaction = await staged.Database.BeginTransactionAsync();
            f.Guild.Save(staged);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }
        Assert.Empty(DisbandPublications(f));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
    }

    [Fact]
    public async Task SuppressedTombstoneWriteCannotQualifyDisbandPublication()
    {
        using Fixture f = await Create();
        Disband(f);
        using (var staged = Context())
        {
            f.Guild.Save(staged);
            staged.ChangeTracker.Clear();
            await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
        }
        Assert.Empty(DisbandPublications(f));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
    }

    [Fact]
    public async Task DirectSaveOfNeverPersistedDisbandDoesNotInsertAnActiveGuild()
    {
        using Fixture f = await Create();
        var temporary = Assert.IsType<Circle>(f.Manager.RegisterGuild(GuildType.Circle, "Temporary", "Leader", "Council", "Member"));
        temporary.DisbandGuild();
        using (var staged = Context())
        {
            temporary.Save(staged);
            await staged.SaveChangesAsync();
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.False(await verify.Guild.AnyAsync(row => row.Id == temporary.Id));
        Assert.Null(f.Manager.GetGuild(temporary.Id));
    }

    [Fact]
    public async Task ChangedSqlTombstoneBeforeCallerCommitCannotPublishCleanup()
    {
        using Fixture f = await Create();
        Disband(f);
        using (var staged = Context())
        {
            await using var transaction = await staged.Database.BeginTransactionAsync();
            f.Guild.Save(staged);
            await staged.SaveChangesAsync();
            await staged.Database.ExecuteSqlRawAsync("UPDATE guild SET originalName = 'foreign edit' WHERE id = 77");
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }
        Assert.Empty(DisbandPublications(f));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Circle", (await verify.Guild.SingleAsync()).OriginalName);
        Assert.Equal(2, DisbandPublications(f).Length);
    }

    [Fact]
    public async Task OutstandingPublicationKeepsNameAndPreventsConcurrentTimerUnits()
    {
        using Fixture f = await Create();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            if (args[0] is not GuildDisbandedMessage) return Task.CompletedTask;
            Interlocked.Increment(ref attempts);
            entered.TrySetResult();
            return release.Task;
        });
        Disband(f);
        f.Manager.Update(61);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Same(f.Guild, f.Manager.GetGuild(GuildType.Circle, "Circle"));
            f.Manager.Update(61);
            Assert.Equal(1, Volatile.Read(ref attempts));
        }
        finally { release.TrySetResult(); }
        await Flush(f.Manager);
        Assert.Null(f.Manager.GetGuild(77));
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task RepeatedSaveInTheSameCallerTransactionPreservesEquivalentTombstoneIntent()
    {
        using Fixture f = await Create();
        Disband(f);
        using (var staged = Context())
        {
            await using var transaction = await staged.Database.BeginTransactionAsync();
            f.Guild.Save(staged);
            await staged.SaveChangesAsync();
            f.Guild.Save(staged);
            await staged.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        Assert.Empty(DisbandPublications(f));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
        Assert.Equal(2, DisbandPublications(f).Length);
    }

    private async Task<Fixture> Create()
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.Add(new CharacterModel { Id = 42, Name = "Leader" });
            seed.Guild.Add(new GuildModel
            {
                Id = 77, Type = (byte)GuildType.Circle, Name = "Circle", LeaderId = 42,
                CreateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                GuildRank = [new GuildRankModel { Id = 77, Index = 0, Name = "Leader" }],
                GuildMember = [new GuildMemberModel { Id = 77, CharacterId = 42, Rank = 0, Note = "Archived" }]
            });
            await seed.SaveChangesAsync();
        }
        var database = new CharacterDatabase();
        database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dbProxy);
        dbProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton<IGuildFactory, GuildFactory>();
        services.AddSingleton<IGlobalGuildManager>(p => new GlobalGuildManager(p.GetRequiredService<IGuildFactory>(), databases));
        services.AddKeyedTransient<IGuildBase>(GuildType.Circle, (p, _) => new Circle(realm, publisher, null,
            globalGuildManager: p.GetRequiredService<IGlobalGuildManager>()));
        ServiceProvider provider = services.BuildServiceProvider();
        var manager = (GlobalGuildManager)provider.GetRequiredService<IGlobalGuildManager>();
        manager.Initialise();
        var guild = Assert.IsType<Circle>(manager.GetGuild(77));
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        return new(provider, manager, guild, player, messages);
    }

    private static void Disband(Fixture f)
    {
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes))
        {
            new NexusForever.Network.World.Message.Model.Shared.Identity { Id = 77, RealmId = 1 }.Write(writer);
            writer.Write(0u);
            writer.Write(0ul);
            writer.WriteStringWide("");
            writer.Write(GuildOperation.Disband, 6u);
            writer.FlushBits();
        }
        using var input = new MemoryStream(bytes.ToArray());
        using var reader = new GamePacketReader(input);
        var operation = new ClientGuildOperation();
        operation.Read(reader);
        f.Manager.HandleGuildOperation(f.Player, operation);
    }

    private static object[] DisbandPublications(Fixture f) => f.Messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))
        .Select(call => call.Arguments[0]).Where(message => message is GuildDisbandedMessage or GuildMemberRemovedMessage).ToArray();

    private static async Task Flush(GlobalGuildManager manager)
    {
        manager.Update(61);
        object gate = typeof(GlobalGuildManager).GetField("cacheLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var active = (HashSet<ulong>)typeof(GlobalGuildManager).GetField("guildSavesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++)
        {
            lock (gate) if (active.Count == 0) return;
            await Task.Delay(10);
        }
        Assert.Fail("Periodic guild save did not settle.");
    }

    private sealed record Fixture(ServiceProvider Provider, GlobalGuildManager Manager, Circle Guild, IPlayer Player,
        RecordingDispatchProxy<IInternalMessagePublisher> Messages) : IDisposable
    {
        public void Dispose() => Provider.Dispose();
    }

    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString);
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
    }
}
