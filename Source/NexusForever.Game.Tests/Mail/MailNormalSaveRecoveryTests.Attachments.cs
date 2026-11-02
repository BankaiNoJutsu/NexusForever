using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Mail;

public partial class MailNormalSaveRecoveryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NewAttachmentIntentSurvivesDisposalAndOuterRollback(bool flush)
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        var item = new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items);
        mail.AttachmentAdd(new MailAttachment(500, 0, item));
        using (var attempt = f.Context())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync(); mail.Save(attempt);
            if (flush) await attempt.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Equal(701ul, Assert.Single(read.CharacterMailAttachment).ItemGuid);
        Assert.Equal(2u, Assert.Single(read.Item).StackCount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DeletedAttachmentIntentSurvivesDisposalAndOuterRollback(bool flush)
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems());
        mail.AttachmentDelete(mail.GetAttachment(0), 0);
        using (var attempt = f.Context())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync(); mail.Save(attempt);
            if (flush) await attempt.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(2, read.Item.Count());
    }

    [Fact]
    public async Task AnAttachmentCannotDeleteAForeignItemLinkAtItsOldOrdinal()
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems());
        using (var transfer = f.Context())
        {
            transfer.CharacterMailAttachment.Single().ItemGuid = 702; await transfer.SaveChangesAsync();
        }
        mail.AttachmentDelete(mail.GetAttachment(0), 0);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var stale = f.Context(); mail.Save(stale); await stale.SaveChangesAsync();
        });
        using var read = f.Context(); Assert.Equal(702ul, Assert.Single(read.CharacterMailAttachment).ItemGuid);
    }

    [Fact]
    public async Task AnUnfinishedAttachmentRowExcludesASecondLoadedOwner()
    {
        using var f = new Fixture(); await SeedAttachment(f); var items = AttachmentItems();
        var first = new MailItem(f.Load(), itemManager: items).GetAttachment(0);
        var other = new MailItem(f.Load(), itemManager: items).GetAttachment(0);
        first.EnqueueDelete(); other.EnqueueDelete();
        using var attempt = f.Context(); first.Save(attempt);
        using var overlap = f.Context(); Assert.Throws<InvalidOperationException>(() => other.Save(overlap));
    }

    [Fact]
    public async Task ParentMailDeletionCommitsItsAttachmentCascadeWithoutDiscardingTheItem()
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems());
        mail.EnqueueDelete(true);
        using (var save = f.Context()) { mail.Save(save); await save.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment); Assert.Equal(2, read.Item.Count());
    }

    [Fact]
    public async Task LostAttachmentCreationAcknowledgementReplaysWithoutDuplicateRows()
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        mail.AttachmentAdd(new MailAttachment(500, 0, new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items)));
        var fault = new LostAcknowledgement();
        await Assert.ThrowsAsync<IOException>(async () =>
        {
            using var attempt = f.Context(fault); await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); await attempt.SaveChangesAsync(); await transaction.CommitAsync();
        });
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Single(read.CharacterMailAttachment); Assert.Single(read.Item);
    }

    [Fact]
    public async Task AttachmentSettlementDoesNotConsumeLaterItemStackIntent()
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        var item = new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items); mail.AttachmentAdd(new MailAttachment(500, 0, item));
        using (var save = f.Context()) { mail.Save(save); item.StackCount = 3; await save.SaveChangesAsync(); }
        using (var read = f.Context()) Assert.Equal(2u, Assert.Single(read.Item).StackCount);
        using (var later = f.Context()) { mail.Save(later); await later.SaveChangesAsync(); }
        using var final = f.Context(); Assert.Equal(3u, Assert.Single(final.Item).StackCount); Assert.Single(final.CharacterMailAttachment);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AForeignExtraAttachmentCannotHideBeforeOrAfterAHeaderFlush(bool afterFlush)
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems()); mail.MarkAsRead();
        if (!afterFlush)
        {
            using var insert = f.Context(); insert.CharacterMailAttachment.Add(new CharacterMailAttachmentModel { Id = 500, Index = 1, ItemGuid = 702 }); await insert.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(); await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); await attempt.SaveChangesAsync();
            if (afterFlush) await attempt.Database.ExecuteSqlRawAsync("INSERT INTO character_mail_attachment (id, \"index\", itemGuid) VALUES (500, 1, 702)");
            await transaction.CommitAsync();
        });
    }

    [Theory]
    [InlineData("header")] [InlineData("membership")]
    public async Task SqlTriggersCannotRewriteTheMailAggregateBeforeImplicitCommit(string rewrite)
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems()); mail.MarkAsRead();
        using (var setup = f.Context())
        {
            string statement = rewrite == "header"
                ? "CREATE TRIGGER rewrite_mail AFTER UPDATE ON character_mail BEGIN UPDATE character_mail SET recipientId = 84 WHERE id = NEW.id; END"
                : "CREATE TRIGGER rewrite_mail AFTER UPDATE ON character_mail BEGIN INSERT INTO character_mail_attachment (id, \"index\", itemGuid) VALUES (500, 1, 702); END";
            await setup.Database.ExecuteSqlRawAsync(statement);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(); mail.Save(attempt); await attempt.SaveChangesAsync();
        });
        var stored = f.Load(); Assert.Equal(42ul, stored.RecipientId); Assert.Equal((byte)0, stored.Flags); Assert.Single(stored.Attachment);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SuppressedMailFlushCannotAcknowledgeAnUnsavedAttachmentItem(bool existing)
    {
        using var f = new Fixture(); var items = AttachmentItems(); if (existing) await SeedAttachment(f);
        var mail = new MailItem(f.Load(), itemManager: items);
        var item = existing ? (GameItem)mail.GetAttachment(0).Item : new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items);
        if (existing) item.StackCount = 3; else mail.AttachmentAdd(new MailAttachment(500, 0, item));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(new SuppressMailFlush()); mail.Save(attempt); await attempt.SaveChangesAsync();
        });
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var read = f.Context(); Assert.Equal(existing ? 3u : 2u, read.Item.Single(row => row.Id == 701).StackCount);
        Assert.Single(read.CharacterMailAttachment);
    }

    private sealed class SuppressMailFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(InterceptionResult<int>.SuppressWithResult(0));
    }

    [Fact]
    public async Task SavepointRollbackRetainsTheCompleteAttachmentUnitForAFreshRetry()
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        mail.AttachmentAdd(new MailAttachment(500, 0, new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items)));
        using (var attempt = f.Context())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync(); await transaction.CreateSavepointAsync("mail_before");
            mail.Save(attempt); await attempt.SaveChangesAsync(); await transaction.RollbackToSavepointAsync("mail_before"); await transaction.CommitAsync();
        }
        using (var read = f.Context()) { Assert.Empty(read.Item); Assert.Empty(read.CharacterMailAttachment); }
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var final = f.Context(); Assert.Single(final.Item); Assert.Single(final.CharacterMailAttachment);
    }

    [Fact]
    public async Task ANoOpSecondFlushRetainsLaterLiveItemIntent()
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        var item = new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items); mail.AttachmentAdd(new MailAttachment(500, 0, item));
        using (var save = f.Context())
        {
            await using var transaction = await save.Database.BeginTransactionAsync();
            mail.Save(save); await save.SaveChangesAsync(); item.StackCount = 3; Assert.Equal(0, await save.SaveChangesAsync()); await transaction.CommitAsync();
        }
        using (var read = f.Context()) Assert.Equal(2u, Assert.Single(read.Item).StackCount);
        using (var later = f.Context()) { mail.Save(later); await later.SaveChangesAsync(); }
        using var final = f.Context(); Assert.Equal(3u, Assert.Single(final.Item).StackCount);
    }

    [Fact]
    public async Task ACommittedCreationCannotConsumeALaterAttachmentDeletion()
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        var attachment = new MailAttachment(500, 0, new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items)); mail.AttachmentAdd(attachment);
        using (var save = f.Context())
        {
            await using var transaction = await save.Database.BeginTransactionAsync();
            mail.Save(save); await save.SaveChangesAsync(); mail.AttachmentDelete(attachment, 0); await transaction.CommitAsync();
        }
        using (var read = f.Context()) Assert.Single(read.CharacterMailAttachment);
        using (var later = f.Context()) { mail.Save(later); await later.SaveChangesAsync(); }
        using var final = f.Context(); Assert.Empty(final.CharacterMailAttachment); Assert.Single(final.Item);
    }

    [Fact]
    public async Task RestagingAfterFlushCapturesTheLatestHeaderAndItemTogether()
    {
        using var f = new Fixture(); var items = AttachmentItems(); var mail = new MailItem(f.Load(), itemManager: items);
        var item = new GameItem(null, items.GetItemInfo(7001), 2, itemManager: items); mail.AttachmentAdd(new MailAttachment(500, 0, item));
        using (var save = f.Context())
        {
            await using var transaction = await save.Database.BeginTransactionAsync();
            mail.Save(save); await save.SaveChangesAsync(); item.StackCount = 3; mail.MarkAsRead(); mail.Save(save); await save.SaveChangesAsync(); await transaction.CommitAsync();
        }
        using var read = f.Context(); Assert.Equal(3u, Assert.Single(read.Item).StackCount);
        Assert.Equal((byte)NexusForever.Game.Static.Mail.MailFlag.IsRead, read.CharacterMail.Single().Flags); Assert.Single(read.CharacterMailAttachment);
    }

    [Fact]
    public async Task AForeignSqlItemTransferAfterFlushRejectsTheWholeMailCommit()
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems()); mail.MarkAsRead();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(); await using var transaction = await attempt.Database.BeginTransactionAsync();
            mail.Save(attempt); await attempt.SaveChangesAsync();
            await attempt.Item.Where(i => i.Id == 701).ExecuteUpdateAsync(update => update.SetProperty(i => i.OwnerId, 84ul));
            await transaction.CommitAsync();
        });
        using var read = f.Context(); Assert.Null(read.Item.Single(i => i.Id == 701).OwnerId); Assert.Equal((byte)0, read.CharacterMail.Single().Flags);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TrackedItemRewritesCannotHideUnderTheMailSnapshot(bool original)
    {
        using var f = new Fixture(); await SeedAttachment(f); var mail = new MailItem(f.Load(), itemManager: AttachmentItems());
        mail.GetAttachment(0).Item.StackCount = 3;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var attempt = f.Context(); mail.Save(attempt); var entry = attempt.ChangeTracker.Entries<ItemModel>().Single();
            if (original) { entry.OriginalValues.SetValues(entry.CurrentValues); entry.State = EntityState.Unchanged; }
            else entry.Entity.StackCount = 7;
            await attempt.SaveChangesAsync();
        });
        using (var read = f.Context()) Assert.Equal(2u, read.Item.Single(i => i.Id == 701).StackCount);
        using (var retry = f.Context()) { mail.Save(retry); await retry.SaveChangesAsync(); }
        using var final = f.Context(); Assert.Equal(3u, final.Item.Single(i => i.Id == 701).StackCount);
    }

    private static IItemManager AttachmentItems()
    {
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var ip); ip.SetProperty(nameof(IItemInfo.Id), 7001u);
        ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 1 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var manager); manager.SetProperty(nameof(IItemManager.NextItemId), 701ul);
        manager.SetMethodReturn(nameof(IItemManager.GetItemInfo), info); return items;
    }

    private static async Task SeedAttachment(Fixture f)
    {
        using var seed = f.Context();
        foreach (ulong id in new[] { 701ul, 702ul }) seed.Item.Add(new ItemModel { Id = id, ItemId = 7001, Location = (ushort)InventoryLocation.None,
            BagIndex = 0, StackCount = 2, Charges = 1, Durability = 1, MicrochipIds = "", RuneSlots = "" });
        seed.CharacterMailAttachment.Add(new CharacterMailAttachmentModel { Id = 500, Index = 0, ItemGuid = 701 }); await seed.SaveChangesAsync();
    }
}
