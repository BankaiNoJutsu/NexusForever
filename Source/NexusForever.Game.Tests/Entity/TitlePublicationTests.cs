using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class TitlePublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public TitlePublicationTests() { }
    internal TitlePublicationTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task AchievementTitleWaitsForItsOriginalGraphAndWorldUpdate()
    {
        using var f = await Create();
        using (var setup = f.Context()) await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
            ? "CREATE TRIGGER reject_title BEFORE INSERT ON character_title BEGIN SELECT RAISE(ABORT, 'owned title rejection'); END"
            : "CREATE TRIGGER reject_title BEFORE INSERT ON character_title FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned title rejection'");
        f.Actor.Player.AchievementManager.GrantAchievement(500);
        Assert.True(f.Titles.HasTitle(21)); Assert.Empty(f.Packets<ServerTitleUpdate>());
        f.Titles.SendTitles(); Assert.Empty(Assert.Single(f.Packets<ServerTitles>()).Titles);
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Save()); f.Pump();
        using (var read = f.Context()) { Assert.Empty(read.CharacterTitle); Assert.Empty(read.CharacterAchievement); Assert.Empty(read.RealmFirstAchievement); }
        Assert.Empty(f.Packets<ServerTitleUpdate>());
        using (var setup = f.Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_title");
        await f.Save(); Assert.Empty(f.Packets<ServerTitleUpdate>());
        f.Pump(); Assert.Equal(21, Assert.Single(f.Packets<ServerTitleUpdate>()).TitleId);
        using var final = f.Context(); Assert.Single(final.CharacterTitle); Assert.Single(final.CharacterAchievement); Assert.Equal(42ul, Assert.Single(final.RealmFirstAchievement).OwnerId);
        Assert.Empty(final.CharacterEntitlement);
    }

    [Fact]
    public async Task ActiveTitleAndRevocationReachEntityModelsOnlyAfterCommittedPublication()
    {
        using var f = await Create(existing: true);
        f.Titles.AddTitle(22); f.Titles.ActiveTitleId = 22;
        Assert.Equal(22, f.Titles.ActiveTitleId); Assert.Equal(21, f.EntityTitle());
        await f.Save(); Assert.Equal(21, f.EntityTitle()); f.Pump(); Assert.Equal(22, f.EntityTitle());
        f.Titles.RevokeTitle(22); Assert.False(f.Titles.HasTitle(22)); Assert.Equal(0, f.Titles.ActiveTitleId); Assert.Equal(22, f.EntityTitle());
        await f.Save(); f.Pump(); Assert.Equal(0, f.EntityTitle());
        Assert.Equal(new ushort[] { 22, 0 }, f.Packets<ServerTitleSet>().Select(packet => packet.Title));
    }

    [Fact]
    public async Task BulkCommandsWaitForTheirSaveAndKeepIndividualUpdatesSuppressed()
    {
        using var f = await Create(); f.Titles.AddAllTitles();
        Assert.Empty(f.Packets<ServerTitleUpdate>()); Assert.Empty(f.Packets<ServerTitles>());
        await f.Save(); f.Pump(); Assert.Equal(2, Assert.Single(f.Packets<ServerTitles>()).Titles.Count);
        f.Titles.AddAllTitles(); await f.Save(); f.Pump(); Assert.Empty(f.Packets<ServerTitleUpdate>());
        f.Titles.RevokeAllTitles(); Assert.Equal(2, f.Packets<ServerTitles>().Count());
        await f.Save(); f.Pump(); Assert.All(f.Packets<ServerTitles>().Last().Titles, row => Assert.True(row.Revoked));
        Assert.Empty(f.Packets<ServerTitleUpdate>());
    }

    [Fact]
    public async Task OrderedGrantRevokeAndAlreadyOwnedRequestsWaitForTheirCapturedSave()
    {
        using var f = await Create(); f.Titles.AddTitle(21); f.Titles.AddTitle(21); f.Titles.RevokeTitle(21);
        Assert.Empty(f.Packets<ServerTitleUpdate>());
        await f.Save(); f.Pump();
        var packets = f.Packets<ServerTitleUpdate>().ToArray(); Assert.Equal(3, packets.Length);
        Assert.False(packets[0].Alreadyowned); Assert.True(packets[1].Alreadyowned); Assert.True(packets[2].Revoked);
        await f.Save(); f.Pump(); Assert.Equal(3, f.Packets<ServerTitleUpdate>().Count());
    }

    private async Task<Fixture> Create(bool existing = false)
    {
        var source = new SourceFixture(options); source.Seed();
        using (var seed = source.Context())
        {
            seed.Character.Single().Title = existing ? (ushort)21 : (ushort)0;
            if (existing) seed.CharacterTitle.Add(new() { Id = 42, Title = 21, TimeRemaining = 5 });
            await seed.SaveChangesAsync();
        }
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages); var events = new EventQueue(); messages.SetProperty("Events", events);
        var actor = new Actor(source, actorSession: session, actorEvents: events,
            inventoryFactory: (player, _) => BankDeferredCurrencyRecoveryTests.Inventory(player, (IItemManager)(object)source.Source.ManagerProxy, source.Source.Tables, null));
        messages.SetProperty("Player", actor.Player); actor.Set(nameof(Player.Name), "Owner42"); await actor.Load();
        ((RecordingDispatchProxy<IAppearanceManager>)(object)actor.Player.AppearanceManager).SetMethodReturn(nameof(IAppearanceManager.GetBones), Array.Empty<IBone>());
        ((RecordingDispatchProxy<IGuildManager>)(object)actor.Player.GuildManager).SetMethodHandler("GetEnumerator", _ => ((IEnumerable<IGuildBase>)Array.Empty<IGuildBase>()).GetEnumerator());
        typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.CharacterTitle), PlayerBindPointTests.Table(new CharacterTitleEntry { Id = 21, LifeTimeSeconds = 5 }, new CharacterTitleEntry { Id = 22 }));
        table.SetProperty(nameof(IGameTableManager.Achievement), PlayerBindPointTests.Table(new AchievementEntry
            { Id = 500, AchievementTypeId = (uint)AchievementType.RealmFirst, RequiredProgress = 1, CharacterTitleId = 21 }));
        var databases = (IDatabaseManager)typeof(Player).GetField("databaseManager", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actor.Player)!;
        var global = new GlobalAchievementManager(databases, tables); global.Initialise();
        using var read = source.Context(); var model = read.Character.AsNoTracking().Include(row => row.CharacterTitle).Include(row => row.Achievement).Single();
        actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player, model, globalAchievementManager: global));
        var titles = new TitleManager(actor.Player, model, tables); actor.Set(nameof(Player.TitleManager), titles);
        return new(source, actor, titles, messages, tables);
    }

    private sealed record Fixture(SourceFixture Source, Actor Actor, TitleManager Titles, RecordingDispatchProxy<IWorldSession> Messages, IGameTableManager Tables) : IDisposable
    {
        internal CharacterContext Context(params IInterceptor[] interceptors) => Source.Context(interceptors);
        internal Task Save(params IInterceptor[] interceptors) => CharacterAtomicSave.Execute(() => Context(interceptors), 42, Actor.Player.Save);
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations("EnqueueMessageEncrypted").Select(call => call.Arguments[0]).OfType<T>();
        internal void Pump() { Actor.Events.Update(.01); Actor.Events.Update(.01); }
        internal ushort EntityTitle() => ((PlayerEntityModel)typeof(Player).GetMethod("BuildEntityModel", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Actor.Player, null)!).Title;
        public void Dispose() { Actor.Dispose(); Source.Dispose(); }
    }
}
