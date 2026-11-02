using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Marketplace;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class MarketplaceCommittedSaveTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ConfirmedCommitRetainsSuccessAfterAnActualLateAcknowledgementError(bool asynchronous)
    {
        using var connection = Database(); bool observed = false; int enrollments = 0;
        var error = Record.Exception(() => MarketplaceCommittedSave.Save(Persist, context =>
        {
            enrollments++; context.Character.Add(Row()); context.OnSaveSettled(success => observed |= success);
        }));
        using var read = Context(connection); Assert.Single(read.Character); Assert.True(observed);
        Assert.Equal(1, enrollments); Assert.Null(error);

        void Persist(Action<CharacterContext> enroll)
        {
            using var context = Context(connection, late: true); enroll(context);
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always; Flush(context, asynchronous);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnobservedPhysicalCommitRemainsAnExplicitUnknownOutcome(bool asynchronous)
    {
        using var connection = Database(); bool observed = false; int enrollments = 0;
        var error = Record.Exception(() => MarketplaceCommittedSave.Save(Persist, context =>
        {
            enrollments++; context.Character.Add(Row()); context.OnSaveSettled(success => observed |= success);
        }));
        using var read = Context(connection); Assert.Single(read.Character); Assert.False(observed);
        Assert.Equal(1, enrollments); Assert.IsType<IOException>(error);

        void Persist(Action<CharacterContext> enroll)
        {
            using var context = Context(connection, early: true); enroll(context);
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always; Flush(context, asynchronous);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GenuineSqlRejectionDoesNotBecomeConfirmedSuccess(bool asynchronous)
    {
        using var connection = Database();
        using (var setup = Context(connection))
            setup.Database.ExecuteSqlRaw("CREATE TRIGGER reject_marketplace_save BEFORE INSERT ON character BEGIN SELECT RAISE(ABORT,'Rejected staged Character'); END;");
        int enrollments = 0;
        var error = Record.Exception(() => MarketplaceCommittedSave.Save(Persist, context =>
        { enrollments++; context.Character.Add(Row()); }));
        using var read = Context(connection); Assert.Empty(read.Character);
        Assert.Equal(1, enrollments); Assert.IsType<DbUpdateException>(error);

        void Persist(Action<CharacterContext> enroll)
        {
            using var context = Context(connection); enroll(context);
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always; Flush(context, asynchronous);
        }
    }

    [Fact]
    public void EnrollmentFailureDoesNotFlushOrReplayTheCallback()
    {
        using var connection = Database(); int enrollments = 0;
        var error = Record.Exception(() => MarketplaceCommittedSave.Save(Persist, context =>
        { enrollments++; context.Character.Add(Row()); throw new IOException("Enrollment rejected."); }));
        using var read = Context(connection); Assert.Empty(read.Character);
        Assert.Equal(1, enrollments); Assert.IsType<IOException>(error);

        void Persist(Action<CharacterContext> enroll)
        { using var context = Context(connection); enroll(context); context.SaveChanges(); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NormalConfirmedSaveStillPersistsExactlyOnce(bool asynchronous)
    {
        using var connection = Database(); int enrollments = 0;
        MarketplaceCommittedSave.Save(Persist, context => { enrollments++; context.Character.Add(Row()); });
        using var read = Context(connection); Assert.Single(read.Character); Assert.Equal(1, enrollments);

        void Persist(Action<CharacterContext> enroll)
        { using var context = Context(connection); enroll(context); Flush(context, asynchronous); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NormalReturnWithoutOuterCommitCannotReportSuccess(bool asynchronous)
    {
        using var connection = Database(); int enrollments = 0;
        var error = Record.Exception(() => MarketplaceCommittedSave.Save(Persist, context =>
        { enrollments++; context.Character.Add(Row()); }));
        using var read = Context(connection); Assert.Empty(read.Character);
        Assert.Equal(1, enrollments); Assert.IsType<InvalidOperationException>(error);

        void Persist(Action<CharacterContext> enroll)
        {
            using var context = Context(connection); using var transaction = context.Database.BeginTransaction();
            enroll(context); Flush(context, asynchronous);
        }
    }

    [Fact]
    public void AnEarlyEnrollmentCommitCannotAcknowledgeAnIncompleteLaterGraph()
    {
        using var connection = Database(); bool observed = false; int enrollments = 0;
        var error = Record.Exception(() => MarketplaceCommittedSave.Save(Persist, context =>
        {
            enrollments++; context.OnSaveSettled(success => observed |= success);
            context.Character.Add(Row()); context.SaveChanges();
            context.Character.Add(new CharacterModel { Id = 43, Name = "UnfinishedEnrollment" });
            throw new IOException("Enrollment failed after an early partial commit.");
        }));
        using var read = Context(connection); Assert.Equal(42ul, Assert.Single(read.Character).Id);
        Assert.True(observed); Assert.Equal(1, enrollments); Assert.IsType<IOException>(error);

        void Persist(Action<CharacterContext> enroll)
        { using var context = Context(connection); enroll(context); context.SaveChanges(); }
    }

    [Fact]
    public void ANoChangeSaveStillConfirmsItsCompletedUnit()
    {
        using var connection = Database(); int enrollments = 0;
        MarketplaceCommittedSave.Save(Persist, _ => enrollments++);
        using var read = Context(connection); Assert.Empty(read.Character); Assert.Equal(1, enrollments);

        void Persist(Action<CharacterContext> enroll)
        { using var context = Context(connection); enroll(context); Assert.Equal(0, context.SaveChanges()); }
    }

    private static CharacterModel Row() => new() { Id = 42, Name = "MarketplaceCommittedSave" };
    private static void Flush(CharacterContext context, bool asynchronous)
    { if (asynchronous) context.SaveChangesAsync().GetAwaiter().GetResult(); else context.SaveChanges(); }
    private static SqliteConnection Database()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True"); connection.Open();
        using var create = Context(connection); create.Database.EnsureCreated(); return connection;
    }
    private static CommitContext Context(SqliteConnection connection, bool early = false, bool late = false)
    {
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection);
        if (early) options.AddInterceptors(new LoseCommitAcknowledgement());
        return new(options.Options, late);
    }
    private sealed class CommitContext(DbContextOptions<CharacterContext> options, bool late) : CharacterContext(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);
            if (late) builder.AddInterceptors(new LoseCommitAcknowledgement());
        }
    }
    private sealed class LoseCommitAcknowledgement : DbTransactionInterceptor
    {
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
            => throw new IOException("Actual commit acknowledgement lost.");
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Actual commit acknowledgement lost."));
    }
}
