using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Transport;

// Private SQLite transaction outcomes through the real Player taxi owner.
public partial class PlayerTaxiDiscoveryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TaxiDiscoveryAfterAFlushedDisposalOrRollbackRemainsPersistable(bool rollback)
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var failed = Context(connection))
        {
            using var transaction = failed.Database.BeginTransaction();
            f.Invoke("SaveTaxiNodes", failed); failed.SaveChanges();
            if (rollback) transaction.Rollback();
        }
        using var retry = Context(connection); Assert.Empty(retry.CharacterTaxiNode);
        f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Equal((ushort)10, Assert.Single(retry.CharacterTaxiNode).TaxiNodeId);
    }

    [Fact]
    public void TaxiDiscoverySavepointRollbackCannotBeAcknowledgedByOuterCommit()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var failed = Context(connection))
        {
            using var transaction = failed.Database.BeginTransaction(); transaction.CreateSavepoint("before_taxi");
            f.Invoke("SaveTaxiNodes", failed); failed.SaveChanges(); transaction.RollbackToSavepoint("before_taxi"); transaction.Commit();
        }
        using var retry = Context(connection); Assert.Empty(retry.CharacterTaxiNode);
        f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Single(retry.CharacterTaxiNode);
    }

    [Fact]
    public void TaxiDiscoveryRepeatedEnrollmentUsesOneTrackedRowPerKey()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using var save = Context(connection); f.Invoke("SaveTaxiNodes", save); f.Invoke("SaveTaxiNodes", save);
        Assert.Single(save.ChangeTracker.Entries<CharacterTaxiNodeModel>());
        save.SaveChanges(); Assert.Single(save.CharacterTaxiNode);
    }

    [Fact]
    public async Task TaxiDiscoveryActualLostAcknowledgementCanRetryWithoutDuplicateRows()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).AddInterceptors(new LoseTaxiAcknowledgement());
        using (var uncertain = new CharacterContext(options.Options))
        {
            await using var transaction = await uncertain.Database.BeginTransactionAsync();
            f.Invoke("SaveTaxiNodes", uncertain); await uncertain.SaveChangesAsync();
            await Assert.ThrowsAsync<IOException>(() => transaction.CommitAsync());
        }
        using var retry = Context(connection); Assert.Single(retry.CharacterTaxiNode);
        f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Single(retry.CharacterTaxiNode);
        f.Player.DiscoverTaxiNode(20); f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Equal(2, retry.CharacterTaxiNode.Count());
    }

    [Fact]
    public void TaxiDiscoveryRejectsAnotherContextUntilTheOriginalScopeEnds()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var original = Context(connection))
        using (var foreign = Context(connection))
        {
            f.Invoke("SaveTaxiNodes", original);
            var error = Assert.Throws<TargetInvocationException>(() => f.Invoke("SaveTaxiNodes", foreign));
            Assert.IsType<InvalidOperationException>(error.InnerException); Assert.True(foreign.IsSaveRejected);
            Assert.Empty(foreign.ChangeTracker.Entries<CharacterTaxiNodeModel>());
        }
        using var retry = Context(connection); f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Single(retry.CharacterTaxiNode);
    }

    [Theory]
    [InlineData("suppress")] [InlineData("character")] [InlineData("node")]
    public void TaxiDiscoveryRejectsSuppressedOrForeignTrackedKeysAndRetainsIntent(string fault)
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var rejected = Context(connection))
        {
            using var transaction = rejected.Database.BeginTransaction(); f.Invoke("SaveTaxiNodes", rejected);
            var entry = Assert.Single(rejected.ChangeTracker.Entries<CharacterTaxiNodeModel>());
            if (fault == "suppress") entry.State = EntityState.Unchanged;
            if (fault == "character") entry.Entity.Id = 43;
            if (fault == "node") entry.Entity.TaxiNodeId = 20;
            Assert.Throws<InvalidOperationException>(() => rejected.SaveChanges()); Assert.True(rejected.IsSaveRejected);
        }
        using var retry = Context(connection); Assert.Empty(retry.CharacterTaxiNode);
        f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Equal((ushort)10, Assert.Single(retry.CharacterTaxiNode).TaxiNodeId);
    }

    [Fact]
    public void TaxiDiscoveryCannotCommitAfterItsFlushedRowIsRemoved()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var rejected = Context(connection))
        {
            using var transaction = rejected.Database.BeginTransaction(); f.Invoke("SaveTaxiNodes", rejected); rejected.SaveChanges();
            Assert.Equal(1, rejected.CharacterTaxiNode.Where(row => row.Id == 42 && row.TaxiNodeId == 10).ExecuteDelete());
            Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        }
        using var retry = Context(connection); f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Single(retry.CharacterTaxiNode);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TaxiDiscoveryAfterTheLastFlushRemainsPendingAcrossAnEarlierCommit(bool enrollLater)
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var first = Context(connection))
        {
            using var transaction = first.Database.BeginTransaction(); f.Invoke("SaveTaxiNodes", first); first.SaveChanges();
            f.Player.DiscoverTaxiNode(20); if (enrollLater) f.Invoke("SaveTaxiNodes", first);
            transaction.Commit();
        }
        using var retry = Context(connection); Assert.Equal((ushort)10, Assert.Single(retry.CharacterTaxiNode).TaxiNodeId);
        f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Equal(2, retry.CharacterTaxiNode.Count());
    }

    [Fact]
    public void TaxiDiscoveryOuterCommitWithoutAFlushCannotClearIntent()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        using (var unflushed = Context(connection))
        {
            using var transaction = unflushed.Database.BeginTransaction(); f.Invoke("SaveTaxiNodes", unflushed); transaction.Commit();
        }
        using var retry = Context(connection); Assert.Empty(retry.CharacterTaxiNode);
        f.Invoke("SaveTaxiNodes", retry); retry.SaveChanges(); Assert.Single(retry.CharacterTaxiNode);
    }

    [Fact]
    public async Task TaxiDiscoveryEnrollsUnderTheAtomicCharacterValidator()
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture(); f.Player.DiscoverTaxiNode(10);
        await CharacterAtomicSave.Execute(() => Context(connection), 42, context => f.Invoke("SaveTaxiNodes", context));
        using var read = Context(connection); Assert.Single(read.CharacterTaxiNode);
        f.Invoke("SaveTaxiNodes", read); Assert.DoesNotContain(read.ChangeTracker.Entries(), entry => entry.State == EntityState.Added);
        Assert.Empty(read.CharacterEntitlement);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NormalPlayerSaveSettlesTaxiAndCharacterMutationTogether(bool commit)
    {
        using var connection = TaxiPersistenceDatabase(); var f = new Fixture();
        PrepareNormalSave(f.Player); f.Player.DiscoverTaxiNode(10);
        using (var save = Context(connection))
        {
            using var transaction = save.Database.BeginTransaction();
            f.Player.Save(save); save.SaveChanges();
            if (commit) transaction.Commit(); else transaction.Rollback();
        }
        using (var read = Context(connection))
        {
            Assert.Equal(commit ? 1 : 0, read.CharacterTaxiNode.Count());
            Assert.Equal(commit ? 43u : 0u, read.Character.Single().TimePlayedTotal);
            Assert.Empty(read.CharacterEntitlement);
        }
        if (!commit)
        {
            using var retry = Context(connection); f.Player.Save(retry); retry.SaveChanges();
            Assert.Single(retry.CharacterTaxiNode); Assert.Equal(43u, retry.Character.Single().TimePlayedTotal);
        }
    }

    private static void PrepareNormalSave(Player player)
    {
        // Keep the real Player.Save entry point; unrelated managers have no dirty work.
        foreach (var property in typeof(Player).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsInterface && (p.Name.EndsWith("Manager", StringComparison.Ordinal) || p.Name == nameof(Player.Inventory))))
        {
            var args = new object[] { null };
            object value = typeof(RecordingDispatchProxy<>).MakeGenericType(property.PropertyType).GetMethod("Create")!.Invoke(null, args);
            if (property.SetMethod != null) property.SetValue(player, value);
            else property.DeclaringType!.GetField("<" + property.Name + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, value);
        }
        Set(typeof(Player), player, "bindStateLock", new object());
        foreach (string name in new[] { "tradeskills", "schematics" })
        {
            var field = typeof(Player).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
            field.SetValue(player, Activator.CreateInstance(field.FieldType));
        }
        var stats = typeof(WorldEntity).GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance)!;
        stats.SetValue(player, Activator.CreateInstance(stats.FieldType));
        typeof(Player).GetProperty(nameof(Player.TimePlayedTotal))!.SetValue(player, 43d);
    }

    private static SqliteConnection TaxiPersistenceDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True"); connection.Open();
        using var seed = Context(connection); seed.Database.EnsureCreated(); seed.Character.Add(new CharacterModel { Id = 42, Name = "TaxiPersistence" }); seed.SaveChanges();
        return connection;
    }
    private sealed class LoseTaxiAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Lost taxi commit acknowledgement."));
    }
}
