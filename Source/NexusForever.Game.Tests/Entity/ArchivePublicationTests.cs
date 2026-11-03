using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.GalacticArchive;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class ArchivePublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ArchivePublicationTests() { }
    internal ArchivePublicationTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const uint Flags = 0x80000001u;

    [Fact]
    public async Task AchievementArchiveWaitsForTheOriginalGraphAndWorldUpdate()
    {
        using var f = await Create();
        using (var setup = f.Context()) await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
            ? "CREATE TRIGGER reject_archive BEFORE INSERT ON character_galactic_archive BEGIN SELECT RAISE(ABORT, 'owned archive rejection'); END"
            : "CREATE TRIGGER reject_archive BEFORE INSERT ON character_galactic_archive FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned archive rejection'");
        f.Actor.Player.AchievementManager.GrantAchievement(500);
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>()); Assert.Empty(f.Packets<ServerTitleUpdate>());
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Save()); f.Pump();
        using (var read = f.Context()) { Assert.Empty(read.CharacterGalacticArchive); Assert.Empty(read.CharacterAchievement); Assert.Empty(read.CharacterTitle); Assert.Empty(read.RealmFirstAchievement); }
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        using (var setup = f.Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_archive");
        await f.Save(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>()); f.Pump();
        Assert.Equal(Flags, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).UnlockedFlags);
        using var final = f.Context(); Assert.Single(final.CharacterGalacticArchive); Assert.Single(final.CharacterAchievement); Assert.Single(final.CharacterTitle); Assert.Single(final.RealmFirstAchievement);
        Assert.Empty(final.CharacterEntitlement);
    }

    [Fact]
    public async Task ViewedFlagsStayOutOfUpdatesAndInitialListsUntilConfirmed()
    {
        using var f = await Create(existing: true); Assert.True(f.Archive.MarkArticleViewed(900));
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        f.Archive.SendInitialPackets(); Assert.Equal(0u, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).ViewedFlags);
        await f.Save(); f.Pump(); Assert.Equal(1u, f.Packets<ServerGalacticArchiveUpdate>().Last().ViewedFlags);
        Assert.Equal(new uint[] { 0, 1 }, f.Packets<ServerGalacticArchiveUpdate>().Select(row => row.ViewedFlags));
        using var read = f.Context(); Assert.Equal(1u, Assert.Single(read.CharacterGalacticArchive).ViewedFlags);
    }

    [Fact]
    public async Task InitialRuleUnlocksAppearOnlyAfterTheirNewRowsCommit()
    {
        using var f = await Create(completedAchievement: true); f.Archive.SendInitialPackets();
        Assert.Single(f.Packets<ServerGalacticArchiveRefresh>()); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        using (var read = f.Context()) Assert.Empty(read.CharacterGalacticArchive);
        await f.Save(); f.Pump(); Assert.Equal(900u, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).ArchiveArticleId);
    }

    [Fact]
    public async Task LinkedUnlocksUseTheOriginalCompleteProgressionSave()
    {
        using var f = await Create(); Assert.True(f.Archive.UnlockArticle(900, grantRewards: false)); Assert.True(f.Archive.UnlockLinkedArticle(910));
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        using (var context = f.Context()) { f.Archive.Save(context); await context.SaveChangesAsync(); }
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>()); f.Pump();
        Assert.Equal(new uint[] { 900, 910 }, f.Packets<ServerGalacticArchiveUpdate>().Select(row => row.ArchiveArticleId));
        using var read = f.Context(); Assert.Equal(2, read.CharacterGalacticArchive.Count()); Assert.Empty(read.CharacterTitle);
    }

    [Fact]
    public async Task RewardCallbackCanSaveWithoutPublishingOrLosingTheArchiveNotice()
    {
        using var f = await Create();
        f.Tables.ArchiveArticle.GetEntry(900).CharacterTitleIdReward = 22;
        var titles = RecordingDispatchProxy<ITitleManager>.Create(out var callback); int saves = 0;
        callback.SetMethodHandler(nameof(ITitleManager.AddTitle), _ =>
        {
            Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
            using var context = f.Context(); f.Actor.Player.Save(context); context.SaveChanges(); saves++; return null;
        });
        f.Actor.Set(nameof(Player.TitleManager), titles); Assert.True(f.Archive.UnlockArticle(900)); Assert.Equal(1, saves);
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>()); f.Pump(); Assert.Single(f.Packets<ServerGalacticArchiveUpdate>());
    }

    private async Task<Fixture> Create(bool existing = false, bool completedAchievement = false)
    {
        var source = new SourceFixture(options); source.Seed();
        using (var seed = source.Context())
        {
            if (existing) seed.CharacterGalacticArchive.Add(new() { Id = 42, ArchiveArticleId = 900, UnlockedFlags = Flags });
            if (completedAchievement) seed.CharacterAchievement.Add(new() { Id = 42, AchievementId = 500, ProgressState = 1, DateCompleted = new(2026, 10, 1) });
            await seed.SaveChangesAsync();
        }
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages); var events = new EventQueue(); messages.SetProperty("Events", events);
        var actor = new Actor(source, actorSession: session, actorEvents: events,
            inventoryFactory: (player, _) => BankDeferredCurrencyRecoveryTests.Inventory(player, (IItemManager)(object)source.Source.ManagerProxy, source.Source.Tables, null));
        messages.SetProperty("Player", actor.Player); actor.Set(nameof(Player.Name), "Owner42"); await actor.Load();
        typeof(GridEntity).GetField("visibleEntities", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.ArchiveArticle), PlayerBindPointTests.Table(new ArchiveArticleEntry { Id = 900, ArchiveEntryId00 = 901 }, new ArchiveArticleEntry { Id = 910, ArchiveEntryId00 = 911 }));
        table.SetProperty(nameof(IGameTableManager.ArchiveEntry), PlayerBindPointTests.Table(new ArchiveEntryEntry { Id = 901 }, new ArchiveEntryEntry { Id = 911 }));
        table.SetProperty(nameof(IGameTableManager.ArchiveLink), PlayerBindPointTests.Table(new ArchiveLinkEntry { Id = 903, ArchiveArticleIdParent = 900, ArchiveArticleIdChild = 910 }));
        table.SetProperty(nameof(IGameTableManager.ArchiveEntryUnlockRule), PlayerBindPointTests.Table(new ArchiveEntryUnlockRuleEntry { Id = 902, ArchiveEntryId = 901, ArchiveEntryUnlockRuleEnum = 0, Object00 = 500 }));
        table.SetProperty(nameof(IGameTableManager.CharacterTitle), PlayerBindPointTests.Table(new CharacterTitleEntry { Id = 21 }, new CharacterTitleEntry { Id = 22 }));
        table.SetProperty(nameof(IGameTableManager.Achievement), PlayerBindPointTests.Table(new AchievementEntry { Id = 500, AchievementTypeId = (uint)AchievementType.RealmFirst, RequiredProgress = 1, CharacterTitleId = 21 }));
        var databases = (IDatabaseManager)typeof(Player).GetField("databaseManager", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(actor.Player)!;
        var global = new GlobalAchievementManager(databases, tables); global.Initialise();
        using var read = source.Context(); var model = read.Character.AsNoTracking().Include(row => row.Achievement).Include(row => row.GalacticArchive).Include(row => row.CharacterTitle).Single();
        actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player, model, globalAchievementManager: global));
        actor.Set(nameof(Player.TitleManager), new TitleManager(actor.Player, model, tables));
        var archive = new GalacticArchiveManager(actor.Player, model, tables); actor.Set(nameof(Player.GalacticArchiveManager), archive);
        return new(source, actor, archive, messages, tables);
    }

    private sealed record Fixture(SourceFixture Source, Actor Actor, GalacticArchiveManager Archive, RecordingDispatchProxy<IWorldSession> Messages, IGameTableManager Tables) : IDisposable
    {
        internal CharacterContext Context(params IInterceptor[] interceptors) => Source.Context(interceptors);
        internal Task Save(params IInterceptor[] interceptors) => CharacterAtomicSave.Execute(() => Context(interceptors), 42, Actor.Player.Save);
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations("EnqueueMessageEncrypted").Select(call => call.Arguments[0]).OfType<T>();
        internal void Pump() { Actor.Events.Update(.01); Actor.Events.Update(.01); }
        public void Dispose() { Actor.Dispose(); Source.Dispose(); }
    }
}
