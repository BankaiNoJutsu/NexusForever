using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Mail;

public partial class MailNormalSaveRecoveryTests
{
    [Theory]
    [InlineData("read")]
    [InlineData("currency")]
    [InlineData("return")]
    public async Task DisposedEnrollmentRetainsTheMailHeaderUntilARealCommit(string change)
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load());
        if (change == "read") mail.MarkAsRead();
        else if (change == "currency") mail.PayOrTakeCash();
        else mail.ReturnMail();
        using (var abandoned = f.Context()) mail.Save(abandoned);
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        var stored = f.Load();
        if (change == "read") Assert.Equal((byte)MailFlag.IsRead, stored.Flags);
        else if (change == "currency") Assert.Equal((byte)1, stored.HasPaidOrCollectedCurrency);
        else { Assert.Equal(84ul, stored.RecipientId); Assert.Equal("Returned: Original", stored.Subject); }
    }

    [Fact]
    public async Task FlushedMailHeaderSurvivesAnOuterRollbackAndFreshContextRetry()
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load()); mail.MarkAsRead();
        using (var attempt = f.Context())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); await attempt.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        Assert.Equal((byte)MailFlag.None, f.Load().Flags);
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        Assert.Equal((byte)MailFlag.IsRead, f.Load().Flags);
    }

    [Fact]
    public async Task DeletedInboxMailIsActuallyDeletedFromSqlAndDoesNotReturnOnRelog()
    {
        using var f = new Fixture(); var manager = f.Manager(); manager.MailDelete(500);
        using (var save = f.Context()) { manager.Save(save); await save.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Empty(read.CharacterMail);
    }

    [Fact]
    public async Task ExpiredMailDeletionSurvivesAnUncommittedSaveEnrollment()
    {
        using var f = new Fixture(expired: true); var manager = f.Manager();
        using (var abandoned = f.Context()) manager.Save(abandoned);
        using (var retry = f.Context()) { manager.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Empty(read.CharacterMail);
    }

    [Fact]
    public async Task ALoadedMailOwnerCannotUpdateAHeaderTransferredBehindItsBack()
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load()); mail.MarkAsRead();
        using (var transfer = f.Context())
        {
            var row = transfer.CharacterMail.Single(); row.RecipientId = 84; await transfer.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var stale = f.Context(); mail.Save(stale); await stale.SaveChangesAsync();
        });
        var stored = f.Load(); Assert.Equal(84ul, stored.RecipientId); Assert.Equal((byte)MailFlag.None, stored.Flags);
    }

    [Fact]
    public async Task ACommittedOlderHeaderDoesNotConsumeALaterReturnMutation()
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load()); mail.MarkAsRead();
        using (var save = f.Context())
        {
            mail.Save(save); mail.ReturnMail(); await save.SaveChangesAsync();
        }
        Assert.Equal(42ul, f.Load().RecipientId);
        using (var later = f.Context()) { mail.Save(later); await later.SaveChangesAsync(); }
        var stored = f.Load(); Assert.Equal(84ul, stored.RecipientId);
        Assert.Equal((byte)(MailFlag.IsRead | MailFlag.NotReturnable), stored.Flags);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NewMailCreationWaitsForOuterCommitAndCanRetryRollback(bool rollback)
    {
        using var f = new Fixture(); var mail = NewMail();
        using (var attempt = f.Context())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); Assert.True(mail.PendingCreate); await attempt.SaveChangesAsync(); Assert.True(mail.PendingCreate);
            if (rollback) await transaction.RollbackAsync(); else await transaction.CommitAsync();
        }
        if (rollback)
        {
            Assert.True(mail.PendingCreate);
            using var retry = f.Context(); mail.Save(retry); await retry.SaveChangesAsync();
        }
        Assert.False(mail.PendingCreate);
        using var read = f.Context(); Assert.Single(read.CharacterMail.Where(m => m.Id == 501));
    }

    [Fact]
    public async Task LostCreationAcknowledgementDoesNotDuplicateMailOrLoseLaterReadIntent()
    {
        using var f = new Fixture(); var mail = NewMail(); var fault = new LostAcknowledgement();
        await Assert.ThrowsAsync<IOException>(async () =>
        {
            using var attempt = f.Context(fault); await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); await attempt.SaveChangesAsync(); await transaction.CommitAsync();
        });
        Assert.True(fault.Fired); mail.MarkAsRead();
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Equal((byte)MailFlag.IsRead, Assert.Single(read.CharacterMail.Where(m => m.Id == 501)).Flags);
        Assert.False(mail.PendingCreate);
    }

    [Fact]
    public async Task RestagingAfterAFlushCommitsTheLatestCompleteHeader()
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load()); mail.MarkAsRead();
        using (var save = f.Context())
        {
            await using var transaction = await save.Database.BeginTransactionAsync();
            mail.Save(save); await save.SaveChangesAsync(); mail.ReturnMail(); mail.Save(save); await save.SaveChangesAsync(); await transaction.CommitAsync();
        }
        var stored = f.Load(); Assert.Equal(84ul, stored.RecipientId); Assert.Equal("Returned: Original", stored.Subject);
        using var clean = f.Context(); mail.Save(clean); Assert.Empty(clean.ChangeTracker.Entries<CharacterMailModel>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AnUnfinishedMailRowExcludesOtherContextsAndLoadedOwners(bool sameObject)
    {
        using var f = new Fixture(); var first = new MailItem(f.Load()); var other = sameObject ? first : new MailItem(f.Load());
        using (var attempt = f.Context())
        {
            first.MarkAsRead(); first.Save(attempt); other.PayOrTakeCash();
            using var overlap = f.Context(); Assert.Throws<InvalidOperationException>(() => other.Save(overlap)); Assert.True(overlap.IsSaveRejected);
        }
        using (var retry = f.Context()) { first.Save(retry); await retry.SaveChangesAsync(); }
        Assert.Equal(sameObject ? (byte)1 : (byte)0, f.Load().HasPaidOrCollectedCurrency);
    }

    [Theory]
    [InlineData("snapshot")] [InlineData("detach")] [InlineData("original")]
    public async Task AForeignRewriteCannotCommitUnderTheMailOwner(string rewrite)
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load()); mail.MarkAsRead();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(); mail.Save(attempt); var entry = attempt.ChangeTracker.Entries<CharacterMailModel>().Single();
            if (rewrite == "snapshot") entry.Entity.Subject = "Foreign";
            else if (rewrite == "detach") entry.State = EntityState.Detached;
            else { entry.OriginalValues.SetValues(entry.CurrentValues); entry.State = EntityState.Unchanged; }
            await attempt.SaveChangesAsync();
        });
        Assert.Equal((byte)MailFlag.None, f.Load().Flags);
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        Assert.Equal((byte)MailFlag.IsRead, f.Load().Flags);
    }

    [Fact]
    public async Task AForeignSqlTransferAfterFlushCannotHideInTheSameCommit()
    {
        using var f = new Fixture(); var mail = new MailItem(f.Load()); mail.MarkAsRead();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(); await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); await attempt.SaveChangesAsync();
            await attempt.CharacterMail.Where(m => m.Id == 500).ExecuteUpdateAsync(update => update.SetProperty(m => m.RecipientId, 84ul));
            await transaction.CommitAsync();
        });
        Assert.Equal(42ul, f.Load().RecipientId); Assert.Equal((byte)MailFlag.None, f.Load().Flags);
    }

    [Fact]
    public async Task ADeletionTombstoneSurvivesAFlushedOuterRollback()
    {
        using var f = new Fixture(); var manager = f.Manager(); manager.MailDelete(500);
        using (var attempt = f.Context())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync();
            manager.Save(attempt); await attempt.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        Assert.Equal(500ul, f.Load().Id);
        using (var retry = f.Context()) { manager.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Empty(read.CharacterMail);
    }

    [Fact]
    public async Task AnExternallyQueuedInboxDeletionIsRetainedWhenManagerEnrollmentIsAbandoned()
    {
        using var f = new Fixture(); var manager = f.Manager();
        var available = (Dictionary<ulong, NexusForever.Game.Abstract.Mail.IMailItem>)typeof(MailManager)
            .GetField("availableMail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(manager)!;
        available[500].EnqueueDelete(true);
        using (var abandoned = f.Context()) manager.Save(abandoned);
        using (var retry = f.Context()) { manager.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Empty(read.CharacterMail);
    }

    [Fact]
    public async Task AFailedMailAttemptCannotMoveItsIntentToAnIdenticalOtherDatabase()
    {
        using var first = new Fixture(); using var other = new Fixture(clone: first.Load());
        var mail = new MailItem(first.Load()); mail.MarkAsRead(); using (var attempt = first.Context()) mail.Save(attempt);
        using (var wrong = other.Context()) Assert.Throws<InvalidOperationException>(() => mail.Save(wrong));
        Assert.Equal((byte)MailFlag.None, other.Load().Flags);
        using (var retry = first.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        Assert.Equal((byte)MailFlag.IsRead, first.Load().Flags);
    }

    private static MailItem NewMail()
    {
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ap.SetProperty(nameof(IAssetManager.NextMailId), 501ul);
        return new MailItem(new MailParameters { RecipientCharacterId = 42, SenderCharacterId = 84,
            MessageType = SenderType.Player, Subject = "Created", Body = "Body", DeliverySpeed = DeliverySpeed.Instant }, assets);
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        internal bool Fired;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!Fired) { Fired = true; throw new IOException("Mail acknowledgement lost after commit."); }
            return Task.CompletedTask;
        }
    }

    internal sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new($"Data Source=NexusForeverMailNormal_{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
        internal Fixture(bool expired = false, CharacterMailModel clone = null)
        {
            connection.Open(); using var seed = Context(); seed.Database.EnsureCreated();
            seed.Character.AddRange(new CharacterModel { Id = 42, Name = "Recipient" }, new CharacterModel { Id = 84, Name = "Sender" });
            seed.CharacterMail.Add(clone ?? new CharacterMailModel { Id = 500, RecipientId = 42, SenderId = 84,
                SenderType = (byte)SenderType.Player, Subject = "Original", Message = "Body",
                CreateTime = DateTime.UtcNow.AddDays(expired ? -31 : 0), DeliveryTime = (byte)DeliverySpeed.Instant });
            seed.SaveChanges();
        }
        internal CharacterContext Context(params IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).AddInterceptors(interceptors).Options);
        internal CharacterMailModel Load() { using var read = Context(); return read.CharacterMail.AsNoTracking().Include(m => m.Attachment).ThenInclude(a => a.Item).Single(); }
        internal MailManager Manager()
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            var session = RecordingDispatchProxy<IGameSession>.Create(out _);
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul); pp.SetProperty(nameof(IPlayer.Session), session);
            var model = new CharacterModel { Id = 42, Mail = [Load()] };
            var manager = new MailManager(player, model); pp.SetProperty(nameof(IPlayer.MailManager), manager); return manager;
        }
        public void Dispose() => connection.Dispose();
    }
}
