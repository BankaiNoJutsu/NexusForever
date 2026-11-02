using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task OrdinaryItemPreservesASharedOwnersTrackedChangeBeforeEnrollment()
    {
        var item = await OrdinaryItem("update");
        using (var context = new CharacterContext(Configuration))
        {
            // Existing mail/auction owners may have already staged their own
            // columns. Item.Save owns its dirty columns, not those prior changes.
            context.Item.Single().OwnerId = null; item.Save(context); await context.SaveChangesAsync();
        }
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.Item);
        Assert.Null(stored.OwnerId); Assert.Equal(5u, stored.StackCount);
    }

    [Fact]
    public async Task OrdinaryItemNoOpSecondFlushRetainsLaterLiveIntent()
    {
        var item = await OrdinaryItem("update");
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            item.Save(context); await context.SaveChangesAsync(); Assert.Equal(0, await context.SaveChangesAsync());
            item.StackCount = 9; await transaction.CommitAsync();
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(5u, read.Item.Single().StackCount);
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(9u, final.Item.Single().StackCount);
    }

    [Fact]
    public async Task OrdinaryItemUnflushedRestagingDoesNotReplaceTheLastSqlWitness()
    {
        var item = await OrdinaryItem("update");
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            item.Save(context); await context.SaveChangesAsync(); item.StackCount = 9; item.Save(context);
            await transaction.CommitAsync();
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(5u, read.Item.Single().StackCount);
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(9u, final.Item.Single().StackCount);
    }

    [Fact]
    public async Task OrdinaryItemSavepointRollbackAndOuterCommitRetainFreshRetryIntent()
    {
        var item = await OrdinaryItem("update");
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await transaction.CreateSavepointAsync("ordinary_item"); item.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("ordinary_item"); await transaction.CommitAsync();
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(17u, read.Item.Single().StackCount);
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(5u, final.Item.Single().StackCount);
    }

    [Fact]
    public async Task OrdinaryItemSqlTriggerCannotCommitARewrittenTarget()
    {
        var item = await OrdinaryItem("update");
        using (var configure = new CharacterContext(Configuration)) await configure.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
            ? "CREATE TRIGGER ordinary_rewrite AFTER UPDATE ON item WHEN NEW.stackCount=5 BEGIN UPDATE item SET stackCount=9 WHERE id=NEW.id; END;"
            : "CREATE TRIGGER ordinary_rewrite BEFORE UPDATE ON item FOR EACH ROW SET NEW.stackCount=IF(NEW.stackCount=5,9,NEW.stackCount)");
        using (var context = new CharacterContext(Configuration))
        {
            item.Save(context); await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); Assert.True(context.IsSaveRejected);
        }
        using (var read = new CharacterContext(Configuration))
        { Assert.Equal(17u, read.Item.Single().StackCount); await read.Database.ExecuteSqlRawAsync("DROP TRIGGER ordinary_rewrite"); }
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(5u, final.Item.Single().StackCount);
    }

    [Fact]
    public async Task OrdinaryItemForeignSqlAfterFlushRejectsTheWholeCommit()
    {
        var item = await OrdinaryItem("update");
        using (var context = new CharacterContext(Configuration))
        {
            using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            item.Save(context); await context.SaveChangesAsync();
            await context.Item.Where(row => row.Id == item.Guid).ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.OwnerId, (ulong?)null).SetProperty(row => row.Location, (ushort)InventoryLocation.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync()); Assert.True(context.IsSaveRejected);
            await transaction.RollbackAsync();
        }
        using (var read = new CharacterContext(Configuration))
        { Assert.Equal(17u, read.Item.Single().StackCount); Assert.Equal(100ul, read.Item.Single().OwnerId); }
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(5u, final.Item.Single().StackCount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OrdinaryItemTrackedAndOriginalRewritesCannotConsumeOwnedIntent(bool original)
    {
        var item = await OrdinaryItem("update");
        using (var context = new CharacterContext(Configuration))
        {
            item.Save(context); var entry = context.ChangeTracker.Entries<ItemModel>().Single();
            if (original) entry.Property(row => row.StackCount).OriginalValue = 9; else entry.Entity.StackCount = 9;
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); Assert.True(context.IsSaveRejected);
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(17u, read.Item.Single().StackCount);
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(5u, final.Item.Single().StackCount);
    }
}
