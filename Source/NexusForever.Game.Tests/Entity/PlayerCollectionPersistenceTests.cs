using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class PlayerCollectionPersistenceTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public PlayerCollectionPersistenceTests() { }
    internal PlayerCollectionPersistenceTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [Theory]
    [InlineData("datacube-new")] [InlineData("datacube-progress")] [InlineData("archive-new")] [InlineData("archive-view")]
    public async Task AcceptedPublicCollectionChangesSurviveRejectedPlayerFlushAndRetry(string action)
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context())
        {
            if (action == "datacube-progress") seed.CharacterDatacube.Add(new() { Id = 42, Type = 0, Datacube = 123, Progress = 1 });
            if (action == "archive-view") seed.CharacterGalacticArchive.Add(new() { Id = 42, ArchiveArticleId = 123, UnlockedFlags = 0x80000001 });
            seed.SaveChanges();
        }
        using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        if (action.StartsWith("datacube", StringComparison.Ordinal)) cubes.AddDatacube(123, action == "datacube-new" ? 1u : 2u);
        else { if (action == "archive-new") Assert.True(archive.UnlockArticle(123, grantRewards: false)); Assert.True(archive.MarkArticleViewed(123)); }
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor);
        using var stored = f.Context();
        if (action.StartsWith("datacube", StringComparison.Ordinal)) Assert.Equal(action == "datacube-new" ? 1u : 3u, Assert.Single(stored.CharacterDatacube).Progress);
        else { var row = Assert.Single(stored.CharacterGalacticArchive); Assert.Equal(0x80000001u, row.UnlockedFlags); Assert.Equal(1u, row.ViewedFlags); }
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task CombinedCubeJournalScanAndArchiveProgressRecoversAfterUncertainOutcome(string fault)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        cubes.AddDatacube(123, 1); cubes.AddDatacubeVolume(123, 2); cubes.AddScientistCreatureScan(123);
        Assert.True(archive.UnlockArticle(123)); Assert.True(archive.MarkArticleViewed(123));
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed"));
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        await Persist(f, actor);
        using var stored = f.Context(); Assert.Equal(3, stored.CharacterDatacube.Count());
        Assert.Equal(1u, stored.CharacterDatacube.Single(row => row.Type == (byte)DatacubeType.Datacube).Progress);
        Assert.Equal(2u, stored.CharacterDatacube.Single(row => row.Type == (byte)DatacubeType.Journal).Progress);
        Assert.Equal(1u, stored.CharacterDatacube.Single(row => row.Type == (byte)DatacubeType.ScientistCreatureScan).Progress);
        var article = Assert.Single(stored.CharacterGalacticArchive); Assert.Equal(0x80000001u, article.UnlockedFlags); Assert.Equal(1u, article.ViewedFlags);
        Assert.Equal(22, Assert.Single(stored.CharacterTitle).Title);
        using var reload = new Actor(f); await reload.Load(); var (_, replay) = Configure(f, reload);
        Assert.True(replay.UnlockArticle(123)); await Persist(f, reload); using var final = f.Context(); Assert.Single(final.CharacterTitle);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NormalSaveFreezesCollectionValuesMembershipAndArchiveTitleBeforeAuth(bool existing)
    {
        using var f = new Fixture(options); f.Seed();
        if (existing)
        {
            using var seed = f.Context(); seed.CharacterDatacube.Add(new() { Id = 42, Type = 0, Datacube = 123, Progress = 1 });
            seed.CharacterGalacticArchive.Add(new() { Id = 42, ArchiveArticleId = 123, UnlockedFlags = 0x80000001 }); seed.SaveChanges();
        }
        using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        actor.OnAuth = () => { cubes.AddDatacube(123, 2); cubes.AddDatacubeVolume(123, 4); Assert.True(archive.UnlockArticle(124)); Assert.Equal(existing, archive.MarkArticleViewed(123)); };
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var first = f.Context())
        {
            Assert.Equal(existing ? 1 : 0, first.CharacterDatacube.Count());
            Assert.Equal(existing ? 1 : 0, first.CharacterGalacticArchive.Count());
            if (existing) { Assert.Equal(1u, first.CharacterDatacube.Single().Progress); Assert.Equal(0u, first.CharacterGalacticArchive.Single().ViewedFlags); }
            Assert.Empty(first.CharacterTitle);
        }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        using var stored = f.Context(); Assert.Equal(existing ? 3u : 2u, stored.CharacterDatacube.Single(row => row.Type == 0).Progress);
        Assert.Equal(4u, stored.CharacterDatacube.Single(row => row.Type == (byte)DatacubeType.Journal).Progress);
        Assert.Equal(existing ? 2 : 1, stored.CharacterGalacticArchive.Count()); Assert.Equal(23, Assert.Single(stored.CharacterTitle).Title);
    }

    [Fact]
    public async Task FlushedValuesAreAcknowledgedWhileLaterPublicEditsRemainPending()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        cubes.AddDatacube(123, 1); Assert.True(archive.UnlockArticle(123));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            cubes.AddDatacube(123, 2); Assert.True(archive.MarkArticleViewed(123)); await transaction.CommitAsync();
        }
        using (var first = f.Context()) { Assert.Equal(1u, first.CharacterDatacube.Single().Progress); Assert.Equal(0u, first.CharacterGalacticArchive.Single().ViewedFlags); }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal(3u, stored.CharacterDatacube.Single().Progress); Assert.Equal(1u, stored.CharacterGalacticArchive.Single().ViewedFlags);
    }

    [Fact]
    public async Task RecoveryRefusesAMixtureOfSeparatelyMatchingCollectionCandidates()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        cubes.AddDatacube(123, 1); Assert.True(archive.UnlockArticle(123, grantRewards: false));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using (var foreign = f.Context()) { foreign.CharacterDatacube.Add(new() { Id = 42, Type = 0, Datacube = 123, Progress = 1 }); foreign.SaveChanges(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(f, actor)); using var stored = f.Context(); Assert.Single(stored.CharacterDatacube); Assert.Empty(stored.CharacterGalacticArchive);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedArchiveIntentCannotBeOverwritten(bool tracked)
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context()) { seed.CharacterGalacticArchive.Add(new() { Id = 42, ArchiveArticleId = 123, UnlockedFlags = 0x80000001 }); seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var (_, archive) = Configure(f, actor); Assert.True(archive.MarkArticleViewed(123));
        if (!tracked) { using var foreign = f.Context(); await foreign.CharacterGalacticArchive.ExecuteUpdateAsync(set => set.SetProperty(row => row.ViewedFlags, 9u)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { actor.Player.Save(context); if (tracked) context.CharacterGalacticArchive.Local.Single().ViewedFlags = 9; }));
        using var stored = f.Context(); Assert.Equal(tracked ? 0u : 9u, stored.CharacterGalacticArchive.Single().ViewedFlags);
    }

    [Fact]
    public async Task UnknownLoadedTypesArticlesAndBitsSurviveAnUnrelatedCollectionWrite()
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context())
        {
            seed.CharacterDatacube.Add(new() { Id = 42, Type = 209, Datacube = 999, Progress = 0xfeed });
            seed.CharacterGalacticArchive.AddRange(new CharacterGalacticArchiveModel { Id = 42, ArchiveArticleId = 999, UnlockedFlags = 0x87654321, ViewedFlags = 0x12345678 },
                new CharacterGalacticArchiveModel { Id = 42, ArchiveArticleId = 123, UnlockedFlags = 0xc0000001, ViewedFlags = 0x40000000 }); seed.SaveChanges();
        }
        using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor); cubes.AddDatacube(123, 1); Assert.True(archive.MarkArticleViewed(123)); await Persist(f, actor);
        using var stored = f.Context(); Assert.Equal(0xfeedu, stored.CharacterDatacube.Single(row => row.Type == 209).Progress);
        var unknown = stored.CharacterGalacticArchive.Single(row => row.ArchiveArticleId == 999); Assert.Equal(0x87654321u, unknown.UnlockedFlags); Assert.Equal(0x12345678u, unknown.ViewedFlags);
        var known = stored.CharacterGalacticArchive.Single(row => row.ArchiveArticleId == 123); Assert.Equal(0xc0000001u, known.UnlockedFlags); Assert.Equal(0x40000001u, known.ViewedFlags);
    }

    [Fact]
    public async Task DirectManagerAndDatacubeSaveApisShareThePlayersAcknowledgedGraph()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        cubes.AddDatacube(123, 1); Assert.True(archive.UnlockArticle(123, grantRewards: false)); await Persist(f, actor);
        cubes.AddDatacube(123, 2); Assert.True(archive.MarkArticleViewed(123));
        await using (var context = f.Context()) { cubes.GetDatacube(123, DatacubeType.Datacube).Save(context); cubes.Save(context); archive.Save(context); await context.SaveChangesAsync(); }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal(3u, Assert.Single(stored.CharacterDatacube).Progress); Assert.Equal(1u, Assert.Single(stored.CharacterGalacticArchive).ViewedFlags);
    }

    [Fact]
    public async Task FailedCollectionSaveCannotMoveItsAcceptedGraphToAnotherDatabase()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (cubes, _) = Configure(f, actor); cubes.AddDatacube(123, 1);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using var foreign = new Fixture(null); foreign.Seed(); await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(foreign, actor));
        await Persist(f, actor); using var read = f.Context(); using var other = foreign.Context(); Assert.Single(read.CharacterDatacube); Assert.Empty(other.CharacterDatacube);
    }

    [Theory]
    [InlineData("cube-manager")] [InlineData("cube-member")] [InlineData("archive-manager")]
    public async Task ReplacedRealPlayerOwnersCannotFallBackToLegacyWrites(string entryPoint)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (cubes, archive) = Configure(f, actor);
        cubes.AddDatacube(123, 1); Assert.True(archive.UnlockArticle(123, grantRewards: false)); await Persist(f, actor);
        Configure(f, actor);
        using var context = f.Context();
        Assert.Throws<InvalidOperationException>(() =>
        {
            if (entryPoint == "cube-manager") cubes.Save(context);
            else if (entryPoint == "cube-member") cubes.GetDatacube(123, DatacubeType.Datacube).Save(context);
            else archive.Save(context);
        });
        Assert.True(context.IsSaveRejected); using var stored = f.Context(); Assert.Equal(1u, Assert.Single(stored.CharacterDatacube).Progress);
        Assert.Equal(0x80000001u, Assert.Single(stored.CharacterGalacticArchive).UnlockedFlags);
    }

    private static (DatacubeManager, GalacticArchiveManager) Configure(Fixture f, Actor actor)
    {
        CharacterModel loaded; using (var read = f.Context()) loaded = read.Character.AsNoTracking().Include(row => row.Datacube).Include(row => row.GalacticArchive).Include(row => row.CharacterTitle).Single();
        var tables = new GameTableManager(Options.Create(new GameTableConfig { GameTablePath = string.Empty }));
        void Table<T>(string name, params T[] entries) where T : class, new()
            => typeof(GameTableManager).GetField("<" + name + ">k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(entries));
        Table(nameof(GameTableManager.Datacube), new DatacubeEntry { Id = 123 });
        Table(nameof(GameTableManager.DatacubeVolume), new DatacubeVolumeEntry { Id = 123 });
        Table(nameof(GameTableManager.PathScientistCreatureInfo), new PathScientistCreatureInfoEntry { Id = 123 });
        Table(nameof(GameTableManager.ArchiveArticle), new ArchiveArticleEntry { Id = 123, ArchiveEntryId00 = 1, CharacterTitleIdReward = 22 }, new ArchiveArticleEntry { Id = 124, CharacterTitleIdReward = 23 });
        Table<ArchiveEntryEntry>(nameof(GameTableManager.ArchiveEntry));
        Table<ArchiveEntryUnlockRuleEntry>(nameof(GameTableManager.ArchiveEntryUnlockRule)); Table<ArchiveLinkEntry>(nameof(GameTableManager.ArchiveLink));
        Table(nameof(GameTableManager.CharacterTitle), new CharacterTitleEntry { Id = 22 }, new CharacterTitleEntry { Id = 23 });
        var cubes = new DatacubeManager(actor.Player, loaded, tables); var archive = new GalacticArchiveManager(actor.Player, loaded, tables);
        actor.Set(nameof(Player.DatacubeManager), cubes); actor.Set(nameof(Player.GalacticArchiveManager), archive); actor.Set(nameof(Player.TitleManager), new TitleManager(actor.Player, loaded, tables));
        return (cubes, archive);
    }
    private static Task Persist(Fixture f, Actor actor, bool fail = false)
        => CharacterAtomicSave.Execute(() => { var context = f.Context(); if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned collection flush fault."); return context; }, 42, actor.Player.Save);
    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned collection acknowledgement loss."));
    }
}
