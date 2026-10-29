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
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Tests.Transport;

public class PlayerBindPointTests
{
    [Fact]
    public void SqliteBindMigrationUpgradesAndRollsBackWithoutChangingExistingCharactersOrEntitlements()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite(connection, o => o.MigrationsAssembly(NexusForever.Database.Extensions.SqliteMigrationsAssembly)).Options);
        string[] migrations = db.Database.GetMigrations().ToArray();
        int bindIndex = Array.FindIndex(migrations, m => m.EndsWith("_CharacterBind", StringComparison.Ordinal));
        Assert.True(bindIndex > 0);
        var migrator = db.GetService<IMigrator>();
        migrator.Migrate(migrations[bindIndex - 1]);
        db.Add(new CharacterModel { Id = 42, Name = "Existing" });
        db.Add(new CharacterEntitlementModel { Id = 42, EntitlementId = 1, Amount = 1 });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        migrator.Migrate(migrations[bindIndex]);
        Assert.Empty(db.CharacterBind);
        db.Add(new CharacterBindModel { Id = 42, BindPointId = 7, WorldId = 51, X = 1, Y = 2, Z = 3 });
        db.SaveChanges();
        Assert.Equal("Existing", db.Character.Single().Name);
        Assert.Equal(1u, db.CharacterEntitlement.Single().Amount);
        migrator.Migrate(migrations[bindIndex - 1]);
        Assert.Equal("Existing", db.Character.Single().Name);
        Assert.Equal(1u, db.CharacterEntitlement.Single().Amount);
    }

    [Fact]
    public void BindPersistsInsertsReloadsAndUpdatesWithoutChangingEntitlements()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = Context(connection);
        db.Database.EnsureCreated();
        db.Add(new CharacterModel { Id = 42, Name = "Bound" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var player = Player(out var session);
        Assert.False(player.TryGetBindPoint(out _, out _));
        player.SetBindPoint(7, 51, new Vector3(1, 2, 3));
        Assert.True(player.TryGetBindPoint(out ushort worldId, out Vector3 position));
        Assert.Equal(51, worldId);
        Assert.Equal(new Vector3(1, 2, 3), position);
        Assert.Equal((ushort)7, Assert.IsType<ServerActionSetUInt16>(Assert.Single(session.Invocations).Arguments[0]).Value);
        Invoke(player, "SaveBindPoint", db);
        Assert.Equal(EntityState.Added, Assert.Single(db.ChangeTracker.Entries<CharacterBindModel>()).State);
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var loaded = Player(out _);
        Invoke(loaded, "LoadBindPoint", db.Character.Include(c => c.Bind).Single().Bind);
        Assert.True(loaded.TryGetBindPoint(out _, out position));
        Assert.Equal(new Vector3(1, 2, 3), position);
        db.ChangeTracker.Clear();
        Invoke(loaded, "SaveBindPoint", db);
        Assert.Empty(db.ChangeTracker.Entries());
        loaded.SetBindPoint(8, 52, new Vector3(4, 5, 6));
        Invoke(loaded, "SaveBindPoint", db);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Equal((ushort)8, db.CharacterBind.Single().BindPointId);
        Assert.Empty(db.CharacterEntitlement);
    }

    [Fact]
    public void FailedBindInsertRemainsRetryableInFreshContext()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        connection.Open();
        using (var db = Context(connection))
            db.Database.EnsureCreated();
        var player = Player(out _);
        player.SetBindPoint(7, 51, Vector3.One);
        using (var failure = Context(connection))
        {
            Invoke(player, "SaveBindPoint", failure);
            Assert.Throws<DbUpdateException>(() => failure.SaveChanges());
        }
        using var retry = Context(connection);
        retry.Add(new CharacterModel { Id = 42, Name = "Retry" });
        retry.SaveChanges();
        retry.ChangeTracker.Clear();
        Invoke(player, "SaveBindPoint", retry);
        Assert.Equal(EntityState.Added, Assert.Single(retry.ChangeTracker.Entries<CharacterBindModel>()).State);
        retry.SaveChanges();
        Assert.Equal((ushort)7, retry.CharacterBind.Single().BindPointId);
    }

    [Fact]
    public void RebindingDuringSaveRemainsDirtyUntilNextSave()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = Context(connection);
        db.Database.EnsureCreated();
        db.Add(new CharacterModel { Id = 42, Name = "Rebind" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var player = Player(out _);
        player.SetBindPoint(7, 51, Vector3.One);
        Invoke(player, "SaveBindPoint", db);
        player.SetBindPoint(8, 52, Vector3.Zero);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Invoke(player, "SaveBindPoint", db);
        Assert.Equal(EntityState.Modified, Assert.Single(db.ChangeTracker.Entries<CharacterBindModel>()).State);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Equal((ushort)8, db.CharacterBind.Single().BindPointId);
    }

    [Theory]
    [InlineData(0, 51, 1f)]
    [InlineData(7, 0, 1f)]
    [InlineData(7, 51, float.NaN)]
    [InlineData(7, 51, float.PositiveInfinity)]
    public void InvalidBindDoesNotChangeStateOrSendPacket(ushort bindId, ushort worldId, float x)
    {
        var player = Player(out var session);
        Assert.Throws<ArgumentException>(() => player.SetBindPoint(bindId, worldId, new Vector3(x, 0, 0)));
        Assert.False(player.TryGetBindPoint(out _, out _));
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void OpenWorldReturnSurvivesNestedInstancesAndOverridesBindFallback()
    {
        var player = Player(out _);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.World), Table(new WorldEntry { Id = 51 }));
        Field(typeof(Game.Entity.Player), player, "gameTableManager", tables);
        player.SetBindPoint(7, 51, Vector3.One);
        Assert.Equal(Vector3.One, player.GetOpenWorldReturnPosition().Position);
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 52 });
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, map);
        Invoke(player, "CaptureOpenWorldReturnPosition");
        var original = player.GetOpenWorldReturnPosition();
        var instance = RecordingDispatchProxy<IMapInstance>.Create(out var instanceProxy);
        instanceProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 53 });
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, instance);
        Invoke(player, "CaptureOpenWorldReturnPosition");
        Assert.Same(original, player.GetOpenWorldReturnPosition());
        Assert.Equal(52u, original.Info.Entry.Id);
    }

    [Fact]
    public void PlayerCreateBindAliasPreservesExistingWireField()
    {
        var packet = new ServerPlayerCreate { BindPointId = 123 };
        Assert.Equal((ushort)123, packet.UnknownBC);
        packet.UnknownBC = 456;
        Assert.Equal((ushort)456, packet.BindPointId);
    }

    private static CharacterContext Context(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);

    private static Game.Entity.Player Player(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        var player = (Game.Entity.Player)RuntimeHelpers.GetUninitializedObject(typeof(Game.Entity.Player));
        Field(typeof(Game.Entity.Player), player, "bindStateLock", new object());
        typeof(Game.Entity.Player).GetProperty(nameof(IPlayer.Identity))!.SetValue(player, new Identity { RealmId = 1, Id = 42 });
        typeof(Game.Entity.Player).GetProperty(nameof(IPlayer.Session))!.SetValue(player, RecordingDispatchProxy<IGameSession>.Create(out sessionProxy));
        return player;
    }

    private static void Invoke(object target, string name, params object[] args) =>
        typeof(Game.Entity.Player).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    internal static void Field(Type type, object target, string name, object value) =>
        type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    internal static GameTable<T> Table<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        Field(typeof(GameTable<T>), table, "<Entries>k__BackingField", entries);
        uint Id(T entry) => (uint)typeof(T).GetField("Id")!.GetValue(entry)!;
        uint max = entries.Length == 0 ? 0 : entries.Max(Id) + 1;
        int[] lookup = Enumerable.Repeat(-1, (int)max).ToArray();
        for (int i = 0; i < entries.Length; i++) lookup[Id(entries[i])] = i;
        Field(typeof(GameTable<T>), table, "header", new GameTableHeader { MaxId = max });
        Field(typeof(GameTable<T>), table, "lookup", lookup);
        return table;
    }
}
