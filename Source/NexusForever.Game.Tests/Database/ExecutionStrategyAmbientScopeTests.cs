using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Tests.Database;

/// <summary>
/// Documents a framework assumption that production code relies on: <c>CharacterAtomicSave</c>, <c>Residence.Association</c> and
/// <c>Residence.Donation</c> call the synchronous <c>strategy.Execute</c> with a delegate that returns a Task and then
/// <c>SaveChangesAsync</c>, in one case after a real <c>await</c> (an asynchronous receipt authorization). A retrying strategy rejects
/// strategy-executed work inside a transaction that is outside its scope, so this only works if EF's ambient scope flows through
/// the await. These cases use a strategy that enforces that rule but never retries.
/// </summary>
public sealed class ExecutionStrategyAmbientScopeTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "nf-ambient-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<CharacterContext> options;

    public ExecutionStrategyAmbientScopeTests()
    {
        Directory.CreateDirectory(directory);
        options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "ambient.sqlite")};Pooling=False")
            .ReplaceService<IExecutionStrategyFactory, EnforcingStrategyFactory>().Options;
        using var context = new CharacterContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, true); } catch (IOException) { }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SynchronousExecuteReturningATaskKeepsTheScopeAcrossAnAwait(bool yieldBeforeSave)
    {
        await using var context = new CharacterContext(options);
        await context.Database.CreateExecutionStrategy().Execute(() => Complete(context, yieldBeforeSave));
        await using var read = new CharacterContext(options);
        Assert.Equal("ambient", (await read.Character.AsNoTracking().SingleAsync()).Name);
    }

    [Fact]
    public async Task WorkOutsideAnyStrategyScopeIsRejectedSoTheScopeCheckIsReal()
    {
        await using var context = new CharacterContext(options);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(context, false));
        Assert.Contains("does not support user-initiated transactions", error.Message);
    }

    private static async Task Complete(CharacterContext context, bool yieldBeforeSave)
    {
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        context.Character.Add(new CharacterModel { Id = 1, Name = "ambient" });
        if (yieldBeforeSave) await Task.Delay(20); // a real asynchronous gap, like an Auth receipt authorization
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private sealed class EnforcingStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new EnforcingStrategy(dependencies);
    }

    private sealed class EnforcingStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => false;
    }
}
