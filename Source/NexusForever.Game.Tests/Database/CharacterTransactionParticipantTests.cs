using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterTransactionParticipantTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CharacterTransactionParticipantTests() { }
    internal CharacterTransactionParticipantTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task MultipleFreshTrackersShareOnlyTheParentsConnectionTransactionAndFinalTargets()
    {
        using var f = new Fixture(options);
        await CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            var transaction = parent.Database.CurrentTransaction!.GetDbTransaction();
            using (var first = parent.CreateTransactionParticipant())
            {
                Assert.Same(parent.Database.GetDbConnection(), first.Database.GetDbConnection());
                Assert.Same(transaction, first.Database.CurrentTransaction!.GetDbTransaction());
                first.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); first.SaveChanges();
            }
            Assert.Equal(ConnectionState.Open, parent.Database.GetDbConnection().State);
            using (var second = parent.CreateTransactionParticipant()) { second.CharacterCurrency.Single().Amount = 150; second.SaveChanges(); }
            parent.ProtectTransactionParticipantWrites();
            using var observer = parent.CreateTransactionParticipant(readOnly: true); Assert.Equal(150ul, observer.CharacterCurrency.AsNoTracking().Single().Amount);
            Assert.Same(transaction, parent.Database.CurrentTransaction!.GetDbTransaction());
        });
        using var read = f.Context(); Assert.Equal(150ul, read.CharacterCurrency.Single().Amount);
    }

    [Theory]
    [InlineData("parent-flush")] [InlineData("child-rejection")] [InlineData("child-commit")] [InlineData("suppressed-child")]
    public async Task ChildWritesRemainProvisionalAndCaughtChildFailuresCannotCommitAnIncompleteParent(string fault)
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAnyAsync<Exception>(() => CharacterAtomicSave.Execute(
            () => fault == "parent-flush" ? f.Intercepted(new FailFlush()) : f.Context(), 42, parent =>
            {
                using (var first = parent.CreateTransactionParticipant())
                { first.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); first.SaveChanges(); }
                using var second = parent.CreateTransactionParticipant(); var row = second.CharacterCurrency.Single(); row.Amount = 150;
                if (fault == "child-rejection")
                {
                    second.RejectSave(new IOException("Child rejected by its owner."));
                    Assert.Throws<InvalidOperationException>(() => second.SaveChanges()); Assert.True(parent.IsSaveRejected);
                }
                else
                {
                    if (fault == "suppressed-child") second.SavingChanges += (_, _) => second.Entry(row).State = EntityState.Unchanged;
                    second.SaveChanges();
                    if (fault == "child-commit")
                    {
                        Assert.Throws<InvalidOperationException>(() => second.Database.CurrentTransaction!.Commit()); Assert.True(parent.IsSaveRejected);
                    }
                }
            }));
        using var read = f.Context(); Assert.Empty(read.CharacterCurrency);
    }

    [Theory]
    [InlineData("target")] [InlineData("delete")] [InlineData("original")]
    [InlineData("raw-before-seal")] [InlineData("raw-after-flush")]
    public async Task ProtectedParticipantRowsCannotBeRewrittenOrDeletedByLaterOrdinaryEnrollment(string fault)
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            using (var child = parent.CreateTransactionParticipant())
            { child.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); child.SaveChanges(); }
            parent.ProtectTransactionParticipantWrites();
            if (fault == "raw-before-seal") parent.Database.ExecuteSqlRaw("UPDATE character_currency SET amount=999 WHERE id=42");
            else if (fault == "raw-after-flush") parent.SavedChanges += (_, _) => parent.Database.ExecuteSqlRaw("UPDATE character_currency SET amount=999 WHERE id=42");
            else
            {
                var row = parent.CharacterCurrency.Single();
                if (fault == "target") row.Amount = 999;
                if (fault == "delete") parent.Remove(row);
                if (fault == "original") parent.Entry(row).Property(value => value.Amount).OriginalValue = 999;
            }
        }));
        using var read = f.Context(); Assert.Empty(read.CharacterCurrency);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AReadOnlyParticipantCannotFlushOrCommitEvenIfTheCallerCatchesItsError(bool commit)
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            using (var writer = parent.CreateTransactionParticipant())
            { writer.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); writer.SaveChanges(); }
            parent.ProtectTransactionParticipantWrites();
            using var reader = parent.CreateTransactionParticipant(readOnly: true);
            Assert.Throws<InvalidOperationException>(() => { if (commit) reader.Database.CurrentTransaction!.Commit(); else reader.SaveChanges(); });
            Assert.True(parent.IsSaveRejected);
        }));
        using var read = f.Context(); Assert.Empty(read.CharacterCurrency);
    }

    [Fact]
    public async Task AnExplicitMutableModelCanJoinItsFinalNormalSaveWithoutChangingTheCommittedBaseline()
    {
        using var f = new Fixture(options);
        await CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            using (var child = parent.CreateTransactionParticipant())
            { child.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); child.SaveChanges(); }
            parent.ProtectTransactionParticipantWrites(typeof(CharacterCurrencyModel));
            var row = parent.CharacterCurrency.Single(); row.Amount = 125;
        });
        using var read = f.Context(); Assert.Equal(125ul, read.CharacterCurrency.Single().Amount);
    }

    [Fact]
    public async Task ParticipantWritesCannotEscapeTheParentsCharacterOwnership()
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            using var child = parent.CreateTransactionParticipant(); child.Character.Add(new() { Id = 43, Name = "Foreign owner" }); child.SaveChanges();
        }));
        using var read = f.Context(); Assert.Equal(42ul, Assert.Single(read.Character).Id);
    }

    [Fact]
    public async Task ParticipantTombstonesCannotDeleteTheParentsLockedCharacter()
    {
        using var f = new Fixture(options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            using var child = parent.CreateTransactionParticipant(); child.Character.Remove(child.Character.Single()); child.SaveChanges();
        }));
        using var read = f.Context(); Assert.Equal(42ul, Assert.Single(read.Character).Id);
    }

    [Fact]
    public async Task OwnedParticipantDeletionRemainsAbsentThroughTheParentCommit()
    {
        using var f = new Fixture(options); using (var seed = f.Context()) { seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); seed.SaveChanges(); }
        await CharacterAtomicSave.Execute(f.Context, 42, parent =>
        {
            using (var child = parent.CreateTransactionParticipant()) { child.CharacterCurrency.Remove(child.CharacterCurrency.Single()); child.SaveChanges(); }
            parent.ProtectTransactionParticipantWrites();
        });
        using var read = f.Context(); Assert.Empty(read.CharacterCurrency); Assert.Single(read.Character);
    }
    private sealed class FailFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new IOException("Outer flush failed after provisional child writes.");
    }
}
