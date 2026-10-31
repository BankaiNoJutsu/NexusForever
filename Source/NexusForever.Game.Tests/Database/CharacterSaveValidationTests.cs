using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterSaveValidationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedEnrollmentCannotBeCaughtAndPartiallySaved(bool asynchronous)
    {
        await using var context = await Context();
        AddPair(context);
        var completions = new List<bool>();
        context.OnSaveCompleted(completions.Add);
        var cause = new InvalidOperationException("Second delivery item failed enrollment.");
        context.RejectSave(cause);

        var failure = await SaveFailure(context, asynchronous);

        Assert.Same(cause, failure.InnerException);
        Assert.True(context.IsSaveRejected);
        Assert.Equal(new[] { false }, completions);
        Assert.Equal(0, await context.Character.CountAsync());
        context.ChangeTracker.Clear();
        Assert.Throws<InvalidOperationException>(() => context.OnSaveValidation(() => { }));
        await SaveFailure(context, !asynchronous);
        Assert.Equal(new[] { false }, completions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectionAfterFlushPreventsExplicitCommitAndStillAllowsRollback(bool asynchronous)
    {
        await using var context = await Context();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var completions = new List<bool>();
        context.OnSaveCompleted(completions.Add);
        AddPair(context);
        await context.SaveChangesAsync();
        context.RejectSave(new InvalidOperationException("Receipt enrollment failed after an earlier flush."));

        await CommitFailure(transaction, asynchronous);

        Assert.Empty(completions);
        Assert.Equal(2, await context.Character.CountAsync()); // visible only inside the uncommitted unit
        await transaction.RollbackAsync();
        Assert.Equal(new[] { false }, completions);
        Assert.Equal(0, await context.Character.CountAsync());
        await SaveFailure(context, asynchronous);
        Assert.Equal(new[] { false }, completions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedInvariantAfterFlushRejectsCommitPermanently(bool asynchronous)
    {
        await using var context = await Context();
        await using var transaction = await context.Database.BeginTransactionAsync();
        bool owned = true;
        int validations = 0;
        context.OnSaveValidation(() =>
        {
            ++validations;
            if (!owned) throw new InvalidOperationException("Owned receipt no longer matches its delivery.");
        });
        AddPair(context);
        await context.SaveChangesAsync();
        owned = false;

        var error = await CommitFailure(transaction, asynchronous);

        Assert.Equal("Owned receipt no longer matches its delivery.", error.Message);
        Assert.Equal(2, validations);
        Assert.True(context.IsSaveRejected);
        owned = true;
        await CommitFailure(transaction, !asynchronous);
        await transaction.RollbackAsync();
        Assert.Equal(0, await context.Character.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImplicitCommitRevalidatesAndRollsBackAlreadyIssuedSql(bool asynchronous)
    {
        await using var context = await Context();
        int validations = 0;
        context.OnSaveValidation(() =>
        {
            if (++validations == 2) throw new InvalidOperationException("Commit invariant failed.");
        });
        var completions = new List<bool>();
        context.OnSaveCompleted(completions.Add);
        AddPair(context); // two commands force the implicit transaction

        var error = await SaveFailure(context, asynchronous);

        Assert.Equal("Commit invariant failed.", error.Message);
        Assert.Equal(2, validations);
        Assert.True(context.IsSaveRejected);
        Assert.Equal(0, await context.Character.CountAsync());
        Assert.Equal(new[] { false }, completions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavingChangesEventCanRejectSingleStatementBeforeAnySql(bool asynchronous)
    {
        await using var context = await Context();
        context.Character.Add(new CharacterModel { Id = 1, Name = "Must not persist" });
        context.SavingChanges += (_, _) => context.RejectSave(new InvalidOperationException("Late enrollment failure."));

        await SaveFailure(context, asynchronous);

        Assert.Equal(0, await context.Character.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitRejectionInsideValidatorCannotBeIgnored(bool asynchronous)
    {
        await using var context = await Context();
        context.OnSaveValidation(() => context.RejectSave(new InvalidOperationException("Owned failure.")));
        AddPair(context);
        await SaveFailure(context, asynchronous);
        Assert.True(context.IsSaveRejected);
        Assert.Equal(0, await context.Character.CountAsync());
    }

    [Fact]
    public async Task ValidationSpansMultipleFlushesAndClearsAfterTerminalCommit()
    {
        await using var context = await Context();
        int calls = 0;
        context.OnSaveValidation(() => ++calls);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Character.Add(new CharacterModel { Id = 1, Name = "First" });
            await context.SaveChangesAsync();
            context.Character.Add(new CharacterModel { Id = 2, Name = "Second" });
            await context.SaveChangesAsync();
            Assert.Equal(2, calls);
            await transaction.CommitAsync();
            Assert.Equal(3, calls);
        }
        context.Character.Add(new CharacterModel { Id = 3, Name = "Later unit" });
        await context.SaveChangesAsync();
        Assert.Equal(3, calls);
        Assert.False(context.IsSaveRejected);
        Assert.Equal(3, await context.Character.CountAsync());
    }

    [Fact]
    public async Task RollbackClearsOldValidationWithoutRejectingReusableHealthyContext()
    {
        await using var context = await Context();
        int calls = 0;
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.OnSaveValidation(() => ++calls);
            AddPair(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        context.ChangeTracker.Clear();
        context.Character.Add(new CharacterModel { Id = 3, Name = "Fresh unit" });
        await context.SaveChangesAsync();
        Assert.Equal(1, calls);
        Assert.False(context.IsSaveRejected);
        Assert.Equal(1, await context.Character.CountAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ValidationPhaseDistinguishesFlushAndActualCommit(bool asynchronous, bool explicitTransaction)
    {
        await using var context = await Context();
        var phases = new List<CharacterSaveValidationPhase>();
        context.OnSaveValidation(phases.Add);
        await using var transaction = explicitTransaction ? await context.Database.BeginTransactionAsync() : null;
        context.Database.AutoTransactionBehavior = Microsoft.EntityFrameworkCore.AutoTransactionBehavior.Always;
        AddPair(context);
        if (asynchronous) await context.SaveChangesAsync(); else context.SaveChanges();
        if (explicitTransaction)
        {
            Assert.Equal(new[] { CharacterSaveValidationPhase.BeforeFlush }, phases);
            if (asynchronous) await transaction.CommitAsync(); else transaction.Commit();
        }
        Assert.Equal(new[] { CharacterSaveValidationPhase.BeforeFlush, explicitTransaction
            ? CharacterSaveValidationPhase.BeforeExplicitCommit : CharacterSaveValidationPhase.BeforeImplicitCommit }, phases);
    }

    [Fact]
    public async Task CatchingAReentrantRegistrationFailureCannotMakeTheUnitCommitReady()
    {
        await using var context = await Context();
        context.OnSaveValidation(() =>
        {
            Assert.Throws<InvalidOperationException>(() => context.OnSaveValidation(() => { }));
        });
        AddPair(context);
        await SaveFailure(context, true);
        Assert.True(context.IsSaveRejected);
        Assert.Equal(0, await context.Character.CountAsync());
    }

    private static async Task<CharacterContext> Context()
    {
        var context = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:").Options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static void AddPair(CharacterContext context)
    {
        context.Character.Add(new CharacterModel { Id = 1, Name = "First half" });
        context.Character.Add(new CharacterModel { Id = 2, Name = "Second half" });
    }

    private static async Task<InvalidOperationException> SaveFailure(CharacterContext context, bool asynchronous) => asynchronous
        ? await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync())
        : Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

    private static async Task<InvalidOperationException> CommitFailure(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, bool asynchronous) => asynchronous
        ? await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync())
        : Assert.Throws<InvalidOperationException>(() => transaction.Commit());
}
