using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterSaveCompletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletionFollowsOuterTransactionCommitOrRollback(bool commit)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var results = new List<bool>();
        context.OnSaveCompleted(results.Add);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await context.SaveChangesAsync();
        Assert.Empty(results);
        if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        Assert.Equal(new[] { commit }, results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavepointRollbackDefersUncertainResultUntilOuterCompletion(bool savepointAfterWrites)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        if (!savepointAfterWrites) await transaction.CreateSavepointAsync("owned");
        var results = new List<bool>();
        context.OnSaveCompleted(results.Add);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await context.SaveChangesAsync();
        if (savepointAfterWrites) await transaction.CreateSavepointAsync("owned");
        await transaction.RollbackToSavepointAsync("owned");
        Assert.Empty(results);
        await transaction.CommitAsync();
        Assert.Equal(new[] { false }, results);
        Assert.Equal(savepointAfterWrites ? 1 : 0, await context.Character.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransactionOnlyDisposalReportsFailureBeforeLaterEmptySave(bool asynchronous)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var transaction = await context.Database.BeginTransactionAsync();
        var results = new List<bool>();
        context.OnSaveCompleted(results.Add);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await context.SaveChangesAsync();
        Assert.Empty(results);
        if (asynchronous) await transaction.DisposeAsync(); else transaction.Dispose();
        Assert.Equal(new[] { false }, results);
        await context.SaveChangesAsync();
        Assert.Equal(new[] { false }, results);
        Assert.Equal(0, await context.Character.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyncImplicitSaveAndDisposeCompleteOnce(bool save)
    {
        var results = new List<bool>();
        using (var context = Context())
        {
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            context.OnSaveCompleted(results.Add);
            context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
            if (save) context.SaveChanges();
        }
        Assert.Equal(new[] { save }, results);
    }

    [Fact]
    public async Task FailedImplicitSaveReportsFailureAndPreservesException()
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_character BEFORE INSERT ON character BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        var results = new List<bool>();
        context.OnSaveCompleted(results.Add);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(new[] { false }, results);
    }

    [Fact]
    public async Task AsyncDisposeWithoutSaveReleasesUncommittedUnit()
    {
        var results = new List<bool>();
        await using (var context = Context()) context.OnSaveCompleted(results.Add);
        Assert.Equal(new[] { false }, results);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThrowingObserverDoesNotFailCommittedImplicitSave(bool asynchronous, bool throwFirst)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var results = RegisterThrowingObserver(context, throwFirst);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });

        int written = asynchronous ? await context.SaveChangesAsync() : context.SaveChanges();

        Assert.Equal(1, written);
        Assert.Equal(1, await context.Character.CountAsync());
        AssertCompletions(results, throwFirst, true);
        Assert.Equal(0, await context.SaveChangesAsync());
        await context.DisposeAsync();
        AssertCompletions(results, throwFirst, true);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ImplicitTransactionCompletionObservesAcceptedTrackedChanges(bool asynchronous, bool throwFirst)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var results = RegisterThrowingObserver(context, throwFirst);
        var observedStates = new List<EntityState>();
        context.OnSaveCompleted(_ => observedStates.AddRange(context.ChangeTracker.Entries<CharacterModel>()
            .OrderBy(entry => entry.Entity.Id).Select(entry => entry.State)));
        // Two SQL inserts force EF's implicit transaction; one insert may not open one.
        context.Character.Add(new CharacterModel { Id = 1, Name = "First" });
        context.Character.Add(new CharacterModel { Id = 2, Name = "Second" });

        int written = asynchronous ? await context.SaveChangesAsync() : context.SaveChanges();

        Assert.Equal(2, written);
        Assert.Equal(2, await context.Character.CountAsync());
        AssertCompletions(results, throwFirst, true);
        Assert.Equal(new[] { EntityState.Unchanged, EntityState.Unchanged }, observedStates);
        Assert.Equal(0, await context.SaveChangesAsync());
        await context.DisposeAsync();
        AssertCompletions(results, throwFirst, true);
        Assert.Equal(2, observedStates.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostCommitSaveObserverFailureRetainsActualCommitOutcome(bool asynchronous)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var results = new List<bool>();
        context.OnSaveCompleted(results.Add);
        context.Character.Add(new CharacterModel { Id = 1, Name = "First" });
        context.Character.Add(new CharacterModel { Id = 2, Name = "Second" });
        EventHandler<SavedChangesEventArgs> failureObserver = (_, _) =>
            throw new InvalidOperationException("Owned post-commit observer failure.");
        context.SavedChanges += failureObserver;

        InvalidOperationException failure = asynchronous
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync())
            : Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        Assert.Equal("Owned post-commit observer failure.", failure.Message);
        Assert.Equal(new[] { true }, results);
        Assert.Equal(2, await context.Character.CountAsync());
        context.SavedChanges -= failureObserver;
        Assert.Equal(0, await context.SaveChangesAsync());
        await context.DisposeAsync();
        Assert.Equal(new[] { true }, results);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task ThrowingObserverDoesNotChangeOuterTransactionOutcome(bool commit, bool asynchronous, bool throwFirst)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var results = RegisterThrowingObserver(context, throwFirst);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await context.SaveChangesAsync();
        Assert.Empty(results);

        if (asynchronous)
        {
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        else
        {
            if (commit) transaction.Commit(); else transaction.Rollback();
        }

        AssertCompletions(results, throwFirst, commit);
        Assert.Equal(commit ? 1 : 0, await context.Character.CountAsync());
        await transaction.DisposeAsync();
        await context.SaveChangesAsync();
        AssertCompletions(results, throwFirst, commit);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThrowingObserverPreservesSavepointUncertainty(bool savepointAfterWrites, bool throwFirst)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        if (!savepointAfterWrites) await transaction.CreateSavepointAsync("owned");
        var results = RegisterThrowingObserver(context, throwFirst);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await context.SaveChangesAsync();
        if (savepointAfterWrites) await transaction.CreateSavepointAsync("owned");
        await transaction.RollbackToSavepointAsync("owned");
        Assert.Empty(results);

        await transaction.CommitAsync();

        AssertCompletions(results, throwFirst, false);
        Assert.Equal(savepointAfterWrites ? 1 : 0, await context.Character.CountAsync());
        await transaction.DisposeAsync();
        await context.SaveChangesAsync();
        AssertCompletions(results, throwFirst, false);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThrowingObserverDoesNotStrandTransactionDisposal(bool asynchronous, bool throwFirst)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var transaction = await context.Database.BeginTransactionAsync();
        var results = RegisterThrowingObserver(context, throwFirst);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });
        await context.SaveChangesAsync();
        Assert.Empty(results);

        if (asynchronous) await transaction.DisposeAsync(); else transaction.Dispose();

        AssertCompletions(results, throwFirst, false);
        Assert.Equal(0, await context.Character.CountAsync());
        await context.SaveChangesAsync();
        await context.DisposeAsync();
        AssertCompletions(results, throwFirst, false);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThrowingObserverPreservesOriginalSqlFailure(bool asynchronous, bool throwFirst)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_character BEFORE INSERT ON character BEGIN SELECT RAISE(ABORT, 'owned failure'); END");
        var results = RegisterThrowingObserver(context, throwFirst);
        context.Character.Add(new CharacterModel { Id = 1, Name = "Owner" });

        DbUpdateException failure = asynchronous
            ? await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync())
            : Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.Contains("owned failure", failure.ToString());
        AssertCompletions(results, throwFirst, false);
        Assert.Equal(0, await context.Character.CountAsync());
        await context.DisposeAsync();
        AssertCompletions(results, throwFirst, false);
    }

    private static List<(string Observer, bool Committed)> RegisterThrowingObserver(CharacterContext context, bool throwFirst)
    {
        var results = new List<(string, bool)>();
        Action<bool> throwing = committed =>
        {
            results.Add(("throwing", committed));
            throw new InvalidOperationException("Owned completion observer failure.");
        };
        Action<bool> owner = committed => results.Add(("owner", committed));
        context.OnSaveCompleted(throwFirst ? throwing : owner);
        context.OnSaveCompleted(throwFirst ? owner : throwing);
        return results;
    }

    private static void AssertCompletions(List<(string Observer, bool Committed)> results, bool throwFirst, bool committed)
    {
        Assert.Equal(throwFirst
            ? new[] { ("throwing", committed), ("owner", committed) }
            : new[] { ("owner", committed), ("throwing", committed) }, results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AggregateSettlementFollowsEveryNormalOwnerEvenWhenRegisteredFirstAndAnOwnerThrows(bool asynchronous)
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var order = new List<string>();
        context.OnSaveSettled(committed => { Assert.True(committed); order.Add("aggregate"); });
        context.OnSaveCompleted(committed => { Assert.True(committed); order.Add("owner1"); throw new InvalidOperationException("Owned observer failure."); });
        context.OnSaveCompleted(committed => { Assert.True(committed); order.Add("owner2"); });
        context.Character.Add(new CharacterModel { Id = 1, Name = "First" });
        context.Character.Add(new CharacterModel { Id = 2, Name = "Second" });
        if (asynchronous) await context.SaveChangesAsync(); else context.SaveChanges();
        Assert.Equal(new[] { "owner1", "owner2", "aggregate" }, order);
        await context.DisposeAsync();
        Assert.Equal(3, order.Count);
    }

    [Fact]
    public async Task SettlementOnlyOwnerRetainsSavepointUncertaintyUntilTerminalCompletion()
    {
        await using var context = Context();
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var results = new List<bool>();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.OnSaveSettled(results.Add);
            await transaction.CreateSavepointAsync("before");
            context.Character.Add(new CharacterModel { Id = 1, Name = "Rolled back" });
            await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before");
            Assert.Empty(results);
            await transaction.CommitAsync();
        }
        Assert.Equal(new[] { false }, results);
        Assert.Equal(0, await context.Character.CountAsync());
    }

    private static CharacterContext Context() => new(new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:").Options);
}
