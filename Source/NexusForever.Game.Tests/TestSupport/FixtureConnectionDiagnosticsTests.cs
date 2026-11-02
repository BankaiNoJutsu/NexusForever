using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;

namespace NexusForever.Game.Tests.TestSupport;

public sealed class FixtureConnectionDiagnosticsTests
{
    [Fact]
    public async Task InMemoryTransactionAndCommandEventsExposePhaseWithoutSqlText()
    {
        var diagnostics = new FixtureConnectionDiagnostics();
        var builder = new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:;Pooling=False")
            .AddInterceptors(diagnostics);
        builder.LogTo(FixtureConnectionDiagnostics.ShouldObserve, diagnostics.Observe);
        await using var context = new CharacterContext(builder.Options);
        await context.Database.OpenConnectionAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        Assert.Contains("lastEvent=Microsoft.EntityFrameworkCore.Database.Transaction.TransactionStarted", diagnostics.Describe());
        await context.Database.ExecuteSqlRawAsync("SELECT 1");
        string observation = diagnostics.Describe();
        Assert.Contains("lastEvent=Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted", observation);
        Assert.Contains("sqlShape=", observation); Assert.DoesNotContain("SELECT 1", observation);
    }

    [Fact]
    public async Task FailedReadOnlyOpenIsNotReportedAsAnActiveConnectionAndCreatesNoFile()
    {
        string missing = Path.Combine(Path.GetTempPath(), "NexusForever-observation-never-created-" + Guid.NewGuid().ToString("N") + ".sqlite");
        Assert.False(File.Exists(missing));
        var diagnostics = new FixtureConnectionDiagnostics();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite($"Data Source={missing};Mode=ReadOnly;Pooling=False")
            .AddInterceptors(diagnostics).Options;
        await using var context = new CharacterContext(options);
        await Assert.ThrowsAnyAsync<DbException>(() => context.Database.OpenConnectionAsync());
        string observation = diagnostics.Describe();
        Assert.Contains("opened=0;", observation); Assert.Contains("openingAttempts=1;", observation);
        Assert.Contains("failed=1;", observation); Assert.Contains("activeAttempts=0;", observation);
        Assert.DoesNotContain(missing, observation); Assert.False(File.Exists(missing));
    }

    [Fact]
    public async Task SuccessfulInMemoryOpenAndCloseReportsTheActualEventsWithoutAFile()
    {
        var diagnostics = new FixtureConnectionDiagnostics();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:;Pooling=False")
            .AddInterceptors(diagnostics).Options;
        await using var context = new CharacterContext(options);
        await context.Database.OpenConnectionAsync();
        Assert.Contains("opened=1;", diagnostics.Describe()); Assert.Contains("state=Open;", diagnostics.Describe());
        await context.Database.CloseConnectionAsync();
        string observation = diagnostics.Describe();
        Assert.Contains("closed=1;", observation); Assert.DoesNotContain("state=Open;", observation);
    }
}
