using System.Numerics;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.GameTable;
using PlayerSaveMask = NexusForever.Game.Entity.Player.PlayerSaveMask;

namespace NexusForever.Game.Tests.Transport;

public partial class PlayerTaxiDiscoveryTests
{
    [Theory]
    [InlineData("rollback")]
    [InlineData("dispose")]
    [InlineData("reject")]
    [InlineData("unflushed")]
    [InlineData("savepoint")]
    public void LocationAndTaxiDiscoveryRetryAfterUnconfirmedSave(string outcome)
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        f.Player.DiscoverTaxiNode(10);
        using (var failed = Context(connection))
        {
            using var transaction = failed.Database.BeginTransaction();
            if (outcome == "savepoint") transaction.CreateSavepoint("before_location");
            f.Player.Save(failed);
            if (outcome == "reject")
            {
                failed.RejectSave(new IOException("Rejected location flush."));
                Assert.ThrowsAny<Exception>(() => failed.SaveChanges());
            }
            else if (outcome != "unflushed") failed.SaveChanges();
            if (outcome == "rollback") transaction.Rollback();
            if (outcome == "unflushed") transaction.Commit();
            if (outcome == "savepoint")
            {
                transaction.RollbackToSavepoint("before_location");
                transaction.Commit();
            }
        }
        using (var beforeRetry = Context(connection))
        {
            Assert.Equal(0f, beforeRetry.Character.Single().LocationX);
            Assert.Empty(beforeRetry.CharacterTaxiNode);
        }
        using (var retry = Context(connection)) { f.Player.Save(retry); retry.SaveChanges(); }
        using var read = Context(connection);
        AssertLocation(read, new Vector3(11, 12, 13));
        Assert.Single(read.CharacterTaxiNode);
        Assert.Empty(read.CharacterEntitlement);
    }

    [Fact]
    public void LocationCommitAcknowledgesOnlyTheFlushedPosition()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        using (var save = Context(connection))
        {
            using var transaction = save.Database.BeginTransaction();
            f.Player.Save(save); save.SaveChanges();
            typeof(GridEntity).GetProperty(nameof(Player.Position))!.SetValue(f.Player, new Vector3(21, 22, 23));
            // A later admitted location is dirty while the old transaction is outstanding.
            Set(typeof(Player), f.Player, "saveMask", PlayerSaveMask.Location);
            transaction.Commit();
        }
        using (var first = Context(connection)) AssertLocation(first, new Vector3(11, 12, 13));
        using (var next = Context(connection)) { f.Player.Save(next); next.SaveChanges(); }
        using var read = Context(connection); AssertLocation(read, new Vector3(21, 22, 23));
    }

    [Fact]
    public void SuccessfulLocationCommitDoesNotStageAnUnchangedLocationAgain()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        using (var save = Context(connection)) { f.Player.Save(save); save.SaveChanges(); }
        using var next = Context(connection); f.Player.Save(next);
        Assert.False(next.Entry(next.Character.Local.Single()).Property(c => c.LocationX).IsModified);
    }

    [Fact]
    public void SuppressedLocationWriteRemainsPendingForFreshSave()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        using (var save = Context(connection))
        {
            f.Player.Save(save);
            save.Entry(save.Character.Local.Single()).Property(c => c.LocationX).IsModified = false;
            save.SaveChanges();
        }
        using (var retry = Context(connection)) { f.Player.Save(retry); retry.SaveChanges(); }
        using var read = Context(connection); AssertLocation(read, new Vector3(11, 12, 13));
    }

    [Fact]
    public void SettledTaxiFareNodeAndDestinationSurviveRollbackAndFreshSave()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        var tables = (IGameTableManager)typeof(Player).GetField("gameTableManager",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(f.Player)!;
        ((RecordingDispatchProxy<IGameTableManager>)(object)tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var model = new CharacterModel { Id = 42 };
        model.Currency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 100 });
        using (var seed = Context(connection)) { seed.CharacterCurrency.Add(model.Currency.Single()); seed.SaveChanges(); }
        var wallet = new CurrencyManager(tables); wallet.Initialise(f.Player, model);
        Set(typeof(Player), f.Player, "<CurrencyManager>k__BackingField", wallet);
        var destination = new MapPosition { Info = new MapInfo { Entry = f.Player.Map.Entry }, Position = f.Player.Position };
        Set(typeof(Player), f.Player, "pendingTeleport", new PendingTeleport
        {
            MapPosition = destination, TaxiFlight = new(f.Player.Session, 10, 12, Vector3.Zero)
        });
        var admission = (IPlayerMapAdmission)f.Player;
        ulong generation = admission.BeginMapAdmission();
        var state = (PlayerMapAdmissionState)typeof(Player).GetField("mapAdmissionState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(f.Player)!;
        Assert.Equal(generation, state.BeginBootstrap());
        Assert.True(f.Player.TrySettleTaxiFare(destination, generation));
        admission.CompleteMapAdmission(generation, true);
        using (var failed = Context(connection))
        {
            using var transaction = failed.Database.BeginTransaction();
            f.Player.Save(failed); failed.SaveChanges(); transaction.Rollback();
        }
        using (var before = Context(connection))
        {
            Assert.Equal(100ul, before.CharacterCurrency.Single().Amount);
            Assert.Equal(0f, before.Character.Single().LocationX);
            Assert.Empty(before.CharacterTaxiNode);
        }
        using (var retry = Context(connection)) { f.Player.Save(retry); retry.SaveChanges(); }
        using var read = Context(connection);
        AssertLocation(read, new Vector3(11, 12, 13));
        Assert.Equal(88ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal((ushort)10, read.CharacterTaxiNode.Single().TaxiNodeId);
    }

    [Fact]
    public void LaterMovementBackToTheSamePositionRetainsItsOwnDirtyIntent()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        using (var save = Context(connection))
        {
            using var transaction = save.Database.BeginTransaction();
            f.Player.Save(save); save.SaveChanges();
            f.Invoke("MarkMapLocationDirty");
            transaction.Commit();
        }
        using var next = Context(connection); f.Player.Save(next);
        Assert.True(next.Entry(next.Character.Local.Single()).Property(c => c.LocationX).IsModified);
    }

    [Fact]
    public void ChangedSqlAfterFlushCannotAcknowledgeTheLocation()
    {
        using var connection = TaxiPersistenceDatabase();
        var f = LocationFixture();
        using (var save = Context(connection))
        {
            using var transaction = save.Database.BeginTransaction();
            f.Player.Save(save); save.SaveChanges();
            save.Database.ExecuteSqlRaw("UPDATE character SET locationX = 99 WHERE id = 42");
            transaction.Commit();
        }
        using (var next = Context(connection)) { f.Player.Save(next); next.SaveChanges(); }
        using var read = Context(connection); AssertLocation(read, new Vector3(11, 12, 13));
    }

    private static Fixture LocationFixture()
    {
        var f = new Fixture(); PrepareNormalSave(f.Player);
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 51 });
        typeof(GridEntity).GetProperty(nameof(Player.Map))!.SetValue(f.Player, map);
        typeof(GridEntity).GetProperty(nameof(Player.Position))!.SetValue(f.Player, new Vector3(11, 12, 13));
        typeof(WorldEntity).GetProperty(nameof(Player.Zone))!.SetValue(f.Player, new WorldZoneEntry { Id = 52 });
        Set(typeof(Player), f.Player, "saveMask", PlayerSaveMask.Location);
        return f;
    }

    private static void AssertLocation(CharacterContext context, Vector3 position)
    {
        var row = context.Character.AsNoTracking().Single();
        Assert.Equal(position, new Vector3(row.LocationX, row.LocationY, row.LocationZ));
        Assert.Equal((ushort)51, row.WorldId);
        Assert.Equal((ushort)52, row.WorldZoneId);
        Assert.Equal(43u, row.TimePlayedTotal);
    }
}
