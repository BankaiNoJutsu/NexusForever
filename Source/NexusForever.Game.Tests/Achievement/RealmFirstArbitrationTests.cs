using System.Collections;
using System.Data.Common;
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
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Achievement;

public sealed partial class RealmFirstArbitrationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public RealmFirstArbitrationTests() { }
    internal RealmFirstArbitrationTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const ushort AchievementId = 500;

    [Fact]
    public async Task StalePeerCannotAnnounceASecondCommittedCharacterRealmFirst()
    {
        using var f = await CreateCharacters();
        f.First.Grant(); await f.First.Save(); f.Pump();
        f.Second.Grant(); await f.Second.Save(); f.Pump();
        AssertWinner(f, f.First);
        using var sql = f.Source.Context();
        Assert.Equal(new ulong[] { 42, 43 }, sql.CharacterAchievement.Where(row => row.DateCompleted != null).Select(row => row.Id).AsEnumerable().Order());
    }

    [Fact]
    public async Task RejectedCharacterSaveCannotReserveTheWinnerAheadOfACommittedPeer()
    {
        using var f = await CreateCharacters(sharedRegistry: true);
        using (var setup = f.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
                ? "CREATE TRIGGER reject_first_claim BEFORE INSERT ON character_achievement WHEN NEW.id=42 BEGIN SELECT RAISE(ABORT, 'owned realm-first failure'); END"
                : "CREATE TRIGGER reject_first_claim BEFORE INSERT ON character_achievement FOR EACH ROW BEGIN IF NEW.id=42 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned realm-first failure'; END IF; END");
        f.First.Grant(); await Assert.ThrowsAsync<DbUpdateException>(() => f.First.Save()); f.Pump();
        Assert.All(f.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        f.Second.Grant(); await f.Second.Save(); f.Pump();
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_first_claim");
        await f.First.Save(); f.Pump();
        AssertWinner(f, f.Second);
    }

    [Fact]
    public async Task LostAcknowledgementCannotLetAStalePeerReassignTheCommittedWinner()
    {
        using var f = await CreateCharacters();
        f.First.Grant(); await Assert.ThrowsAsync<IOException>(() => f.First.Save(new LostAcknowledgement())); f.Pump();
        using (var sql = f.Source.Context()) Assert.NotNull(Assert.Single(sql.CharacterAchievement).DateCompleted);
        Assert.All(f.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        f.Second.Grant(); await f.Second.Save(); f.Pump();
        Assert.All(f.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        await f.First.Save(); f.Pump();
        AssertWinner(f, f.First);
    }

    [Fact]
    public async Task HistoricalCompletionsRemainClosedWithoutInventingANewWinner()
    {
        using var f = await CreateCharacters(historical: true);
        f.Second.Grant(); await f.Second.Save(); f.Pump();
        Assert.All(f.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        using var sql = f.Source.Context();
        Assert.Equal(2, sql.CharacterAchievement.Count(row => row.DateCompleted != null));
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0), sql.CharacterAchievement.Single(row => row.Id == 42).DateCompleted);
    }

    [Fact]
    public async Task SavepointRollbackCannotLeaveAWinningClaim()
    {
        using var f = await CreateCharacters(sharedRegistry: true); f.First.Grant();
        await using (var context = f.Source.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await transaction.CreateSavepointAsync("before_realm_first");
            f.First.Actor.Player.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_realm_first"); await transaction.CommitAsync();
        }
        f.Pump();
        using (var sql = f.Source.Context()) { Assert.Empty(sql.RealmFirstAchievement); Assert.Empty(sql.CharacterAchievement); }
        f.Second.Grant(); await f.Second.Save(); f.Pump(); await f.First.Save(); f.Pump();
        AssertWinner(f, f.Second);
    }

    [Fact]
    public async Task DeletingTheOriginalWinnerCannotReopenItsDurableClaim()
    {
        using var f = await CreateCharacters(); f.First.Grant(); await f.First.Save(); f.Pump();
        using (var sql = f.Source.Context())
        {
            await sql.CharacterAchievement.Where(row => row.Id == 42).ExecuteDeleteAsync();
            await sql.Character.Where(row => row.Id == 42).ExecuteDeleteAsync();
        }
        f.Second.Grant(); await f.Second.Save(); f.Pump(); AssertWinner(f, f.First);
        using var after = f.Source.Context();
        Assert.Equal(42ul, Assert.Single(after.RealmFirstAchievement).OwnerId);
        Assert.Equal(43ul, Assert.Single(after.CharacterAchievement).Id);
    }

    private static void AssertWinner(CharacterPair f, Endpoint winner)
    {
        Assert.All(f.Endpoints, endpoint =>
        {
            var announcement = Assert.Single(endpoint.Announcements);
            Assert.Equal(winner.Actor.Player.Name, announcement.Name);
            Assert.Equal(AchievementId, announcement.AchievementId);
        });
        Assert.All(f.Endpoints, endpoint => Assert.Empty(endpoint.Actor.Errors));
    }

    private async Task<CharacterPair> CreateCharacters(bool sharedRegistry = false, bool historical = false)
    {
        var source = new Fixture(options); source.Seed();
        using (var seed = source.Context())
        {
            seed.Character.Add(new CharacterModel { Id = 43, Name = "Second" });
            if (historical) seed.CharacterAchievement.Add(new CharacterAchievementModel
                { Id = 42, AchievementId = AchievementId, ProgressState = 1, DateCompleted = new DateTime(2026, 10, 1) });
            await seed.SaveChangesAsync();
        }
        var endpoints = new List<Endpoint>();
        foreach (ulong id in new ulong[] { 42, 43 })
        {
            var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
            var events = new EventQueue(); messages.SetProperty("Events", events);
            var actor = new Actor(source, actorModel: new CharacterModel { Id = id }, actorSession: session, actorEvents: events,
                inventoryFactory: (player, _) => BankDeferredCurrencyRecoveryTests.Inventory(player,
                    (IItemManager)(object)source.Source.ManagerProxy, source.Source.Tables, null));
            messages.SetProperty("Player", actor.Player);
            actor.Set(nameof(Player.Name), "Owner" + id);
            await actor.Load(); endpoints.Add(new(source, actor, messages));
        }
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IEnumerable.GetEnumerator), _ => endpoints.Select(endpoint => (IPlayer)endpoint.Actor.Player).ToArray().AsEnumerable().GetEnumerator());
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => args[0] is Identity id
            ? endpoints.SingleOrDefault(endpoint => endpoint.Actor.Player.Identity.Id == id.Id && endpoint.Actor.Player.Identity.RealmId == id.RealmId)?.Actor.Player : null);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.Achievement), PlayerBindPointTests.Table(new AchievementEntry
            { Id = AchievementId, AchievementTypeId = (uint)AchievementType.RealmFirst, RequiredProgress = 1 }));
        var databases = (IDatabaseManager)typeof(Player).GetField("databaseManager", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(endpoints[0].Actor.Player)!;
        var first = new GlobalAchievementManager(databases, tables); first.Initialise();
        var second = sharedRegistry ? first : new GlobalAchievementManager(databases, tables);
        if (!sharedRegistry) second.Initialise();
        for (int i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            using var context = source.Context();
            var model = context.Character.AsNoTracking().Include(row => row.Achievement).Single(row => row.Id == endpoint.Actor.Player.CharacterId);
            endpoint.Actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(endpoint.Actor.Player, model,
                globalAchievementManager: i == 0 ? first : second, playerManager: players));
        }
        return new(source, endpoints);
    }

    private sealed record Endpoint(Fixture Source, Actor Actor, RecordingDispatchProxy<IWorldSession> Messages)
    {
        internal IEnumerable<ServerRealmFirstAchievement> Announcements => Messages.GetInvocations("EnqueueMessageEncrypted")
            .Select(call => call.Arguments[0]).OfType<ServerRealmFirstAchievement>();
        internal void Grant() => Actor.Player.AchievementManager.GrantAchievement(AchievementId);
        internal Task Save(params IInterceptor[] interceptors) => CharacterAtomicSave.Execute(() => Source.Context(interceptors), Actor.Player.CharacterId, Actor.Player.Save);
    }
    private sealed record CharacterPair(Fixture Source, List<Endpoint> Endpoints) : IDisposable
    {
        internal Endpoint First => Endpoints[0];
        internal Endpoint Second => Endpoints[1];
        internal void Pump() { foreach (var endpoint in Endpoints) { endpoint.Actor.Events.Update(.01); endpoint.Actor.Events.Update(.01); } }
        public void Dispose() { foreach (var endpoint in Endpoints) endpoint.Actor.Dispose(); Source.Dispose(); }
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken token = default)
            => Task.FromException(new IOException("owned realm-first acknowledgement loss"));
    }
}
