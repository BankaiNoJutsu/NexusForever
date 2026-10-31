using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Tests.Transport;

public class PlayerTaxiDiscoveryTests
{
    [Theory]
    [InlineData(0u, false)]
    [InlineData(10u, true)]
    [InlineData(20u, true)]
    [InlineData(30u, false)]
    [InlineData(99u, false)]
    [InlineData(16383u, true)]
    [InlineData(16384u, false)]
    public void DiscoveryValidatesNodeFactionAndWireWidth(uint id, bool expected)
    {
        var f = new Fixture();
        f.Player.DiscoverTaxiNode(id);
        f.Player.DiscoverTaxiNode(id);
        Assert.Equal(expected ? 1 : 0, f.Session.Invocations.Count);
        if (expected)
            Assert.Equal(id, Assert.IsType<ServerEntitySelectAuxUInt14>(f.Session.Invocations[0].Arguments[0]).Value);
    }

    [Fact]
    public void UnknownPlayerFactionCannotDiscoverFactionSpecificNodes()
    {
        var f = new Fixture();
        f.Player.Faction1 = 0;
        f.Player.DiscoverTaxiNode(20);
        Assert.Empty(f.Session.Invocations);
        f.Player.DiscoverTaxiNode(10);
        Assert.Single(f.Session.Invocations);
    }

    [Fact]
    public void NearbyDiscoveryUsesTimerAndIgnoresFarOrOtherMapKiosks()
    {
        var f = new Fixture();
        f.Kiosk(10, new Vector3(20, 0, 0));
        f.Kiosk(20, new Vector3(31, 0, 0));
        f.Kiosk(30, Vector3.Zero);
        f.Kiosk(16383, Vector3.Zero, RecordingDispatchProxy<IBaseMap>.Create(out _));
        f.Invoke("UpdateTaxiDiscovery", 1d);
        Assert.Empty(f.Session.Invocations);
        f.Invoke("UpdateTaxiDiscovery", 1.1d);
        Assert.Equal(10u, Assert.IsType<ServerEntitySelectAuxUInt14>(Assert.Single(f.Session.Invocations).Arguments[0]).Value);
        Assert.True(f.Player.IsNearTaxiNode(10, 30));
        Assert.False(f.Player.IsNearTaxiNode(20, 30));
        Assert.False(f.Player.IsNearTaxiNode(10, float.NaN));
        Assert.False(f.Player.IsNearTaxiNode(10, -1));
        f.Invoke("UpdateTaxiDiscovery", 2.1d);
        Assert.Single(f.Session.Invocations);
    }

    [Fact]
    public void DiscoverySavesReloadsAndSendsSortedNodesWithoutAutomaticGrants()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = Context(connection);
        db.Database.EnsureCreated();
        db.Add(new CharacterModel { Id = 42, Name = "Taxi" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var f = new Fixture();
        f.Invoke("SendTaxiNodes");
        Assert.Empty(Assert.IsType<ServerFlightPathUpdate>(f.Session.Invocations[0].Arguments[0]).FlightPathIds);
        f.Player.DiscoverTaxiNode(20);
        f.Player.DiscoverTaxiNode(10);
        f.Invoke("SaveTaxiNodes", db);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Equal(2, db.CharacterTaxiNode.Count());
        var loaded = new Fixture();
        loaded.Invoke("LoadTaxiNodes", db.Character.Include(c => c.TaxiNode).Single().TaxiNode);
        loaded.Invoke("SendTaxiNodes");
        Assert.Equal(new uint[] { 10, 20 }, Assert.IsType<ServerFlightPathUpdate>(Assert.Single(loaded.Session.Invocations).Arguments[0]).FlightPathIds);
        db.ChangeTracker.Clear();
        loaded.Invoke("SaveTaxiNodes", db);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.CharacterEntitlement);
    }

    [Fact]
    public void FailedInsertRetriesAndInFlightDiscoveryRemainsPending()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        connection.Open();
        using (var db = Context(connection)) db.Database.EnsureCreated();
        var f = new Fixture();
        f.Player.DiscoverTaxiNode(10);
        using (var failure = Context(connection))
        {
            f.Invoke("SaveTaxiNodes", failure);
            Assert.Throws<DbUpdateException>(() => failure.SaveChanges());
        }
        using var retry = Context(connection);
        retry.Add(new CharacterModel { Id = 42, Name = "Retry" });
        retry.SaveChanges();
        retry.ChangeTracker.Clear();
        f.Invoke("SaveTaxiNodes", retry);
        f.Player.DiscoverTaxiNode(20);
        retry.SaveChanges();
        retry.ChangeTracker.Clear();
        Assert.Equal((ushort)10, retry.CharacterTaxiNode.Single().TaxiNodeId);
        retry.ChangeTracker.Clear();
        f.Invoke("SaveTaxiNodes", retry);
        Assert.Equal((ushort)20, Assert.Single(retry.ChangeTracker.Entries<CharacterTaxiNodeModel>()).Entity.TaxiNodeId);
        retry.SaveChanges();
        Assert.Equal(2, retry.CharacterTaxiNode.Count());
    }

    [Fact]
    public void TaxiMigrationUpgradesAndRollsBackPreservingBindAndEntitlements()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection,
            o => o.MigrationsAssembly(NexusForever.Database.Extensions.SqliteMigrationsAssembly)).Options);
        var migrations = db.Database.GetMigrations().ToArray();
        string taxiMigration = Assert.Single(migrations, migration => migration.EndsWith("_CharacterTaxiNode", StringComparison.Ordinal));
        int taxiIndex = Array.IndexOf(migrations, taxiMigration);
        Assert.True(taxiIndex > 0);
        string previousMigration = migrations[taxiIndex - 1];
        var migrator = db.GetService<IMigrator>();
        migrator.Migrate(previousMigration);
        db.Add(new CharacterModel { Id = 42, Name = "Existing" });
        db.Add(new CharacterBindModel { Id = 42, BindPointId = 7, WorldId = 51 });
        db.Add(new CharacterEntitlementModel { Id = 42, EntitlementId = 1, Amount = 1 });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        migrator.Migrate(taxiMigration);
        db.Add(new CharacterTaxiNodeModel { Id = 42, TaxiNodeId = 10 });
        db.SaveChanges();
        Assert.Single(db.CharacterTaxiNode);
        migrator.Migrate(previousMigration);
        Assert.Equal("Existing", db.Character.Single().Name);
        Assert.Equal((ushort)7, db.CharacterBind.Single().BindPointId);
        Assert.Equal(1u, db.CharacterEntitlement.Single().Amount);
    }

    private static CharacterContext Context(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);

    private sealed class Fixture
    {
        public Player Player { get; } = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        public RecordingDispatchProxy<IGameSession> Session { get; }
        public IBaseMap Map { get; } = RecordingDispatchProxy<IBaseMap>.Create(out _);
        private readonly Dictionary<uint, IGridEntity> visible = [];

        public Fixture()
        {
            PlayerTaxiDiscoveryTests.Set(typeof(Player), Player, "taxiStateLock", new object());
            Set(typeof(Player), Player, "taxiNodes", new HashSet<ushort>());
            Set(typeof(Player), Player, "unsavedTaxiNodes", new HashSet<ushort>());
            Set(typeof(Player), Player, "taxiDiscoveryTimer", new UpdateTimer(TimeSpan.FromSeconds(2)));
            Set(typeof(GridEntity), Player, "visibleEntities", visible);
            typeof(GridEntity).GetProperty(nameof(IPlayer.Map))!.SetValue(Player, Map);
            typeof(WorldEntity).GetProperty(nameof(IPlayer.MovementManager))!.SetValue(Player, RecordingDispatchProxy<IMovementManager>.Create(out _));
            typeof(Player).GetProperty(nameof(IPlayer.Identity))!.SetValue(Player, new Identity { RealmId = 1, Id = 42 });
            var session = RecordingDispatchProxy<IGameSession>.Create(out var proxy);
            Session = proxy;
            typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(Player, session);
            Player.Faction1 = Faction.Exile;
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            tp.SetProperty(nameof(IGameTableManager.TaxiNode), PlayerBindPointTests.Table(
                new TaxiNodeEntry { Id = 10 }, new TaxiNodeEntry { Id = 20, TaxiNodeFactionEnum = 1 },
                new TaxiNodeEntry { Id = 30, TaxiNodeFactionEnum = 2 },
                new TaxiNodeEntry { Id = 16383 }, new TaxiNodeEntry { Id = 16384 }));
            Set(typeof(Player), Player, "gameTableManager", tables);
        }

        public void Kiosk(uint id, Vector3 position, IBaseMap map = null)
        {
            var kiosk = RecordingDispatchProxy<IWorldEntity>.Create(out var kp);
            kp.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { TaxiNodeId = id });
            kp.SetProperty(nameof(IWorldEntity.Position), position);
            kp.SetProperty(nameof(IWorldEntity.Map), map ?? Map);
            visible[id] = kiosk;
        }

        public void Invoke(string name, params object[] args) =>
            typeof(Player).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Player, args);
    }

    private static void Set(Type type, object target, string name, object value) =>
        type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
