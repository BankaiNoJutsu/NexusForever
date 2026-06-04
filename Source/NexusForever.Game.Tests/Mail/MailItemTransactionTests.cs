using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Mail;

public class MailItemTransactionTests
{
    [Fact]
    public void PayOrTakeCash_Save_PersistsCurrencyCollectedAndNotReturnable()
    {
        MailItem mailItem = new(MailModel());

        mailItem.PayOrTakeCash();

        using CharacterContext context = CreateContext();
        mailItem.Save(context);

        EntityEntry<CharacterMailModel> entry = Assert.Single(context.ChangeTracker.Entries<CharacterMailModel>());
        Assert.Equal((byte)1, entry.Entity.HasPaidOrCollectedCurrency);
        Assert.Equal((byte)MailFlag.NotReturnable, entry.Entity.Flags);
        Assert.True(entry.Property(p => p.HasPaidOrCollectedCurrency).IsModified);
        Assert.True(entry.Property(p => p.Flags).IsModified);
    }

    [Fact]
    public void ReturnMail_Save_PersistsRecipientSubjectAndNotReturnable()
    {
        MailItem mailItem = new(MailModel(
            id: 2ul,
            recipientId: 100ul,
            senderId: 200ul,
            subject: "Settled auction"));

        mailItem.ReturnMail();

        using CharacterContext context = CreateContext();
        mailItem.Save(context);

        EntityEntry<CharacterMailModel> entry = Assert.Single(context.ChangeTracker.Entries<CharacterMailModel>());
        Assert.Equal(200ul, entry.Entity.RecipientId);
        Assert.Equal("Returned: Settled auction", entry.Entity.Subject);
        Assert.Equal((byte)MailFlag.NotReturnable, entry.Entity.Flags);
        Assert.True(entry.Property(p => p.RecipientId).IsModified);
        Assert.True(entry.Property(p => p.Subject).IsModified);
        Assert.True(entry.Property(p => p.Flags).IsModified);
    }

    [Fact]
    public void ReturnMail_NonPlayerSender_ThrowsWithoutMutation()
    {
        MailItem mailItem = new(MailModel(
            id: 4ul,
            recipientId: 100ul,
            senderId: 0ul,
            subject: "Expired auction",
            senderType: SenderType.ItemAuction));

        Assert.Throws<InvalidOperationException>(() => mailItem.ReturnMail());

        Assert.Equal(100ul, mailItem.RecipientId);
        Assert.Equal("Expired auction", mailItem.Subject);
        Assert.Equal(MailFlag.None, mailItem.Flags);
    }

    [Fact]
    public void AttachmentDelete_Save_EnqueuesDeletedAttachmentOnly()
    {
        MailItem mailItem = new(MailModel(id: 3ul));
        IMailAttachment attachment = RecordingDispatchProxy<IMailAttachment>.Create(out RecordingDispatchProxy<IMailAttachment> attachmentProxy);
        mailItem.AttachmentAdd(attachment);

        mailItem.AttachmentDelete(attachment, 0u);

        Assert.Null(mailItem.GetAttachment(0u));
        Assert.Empty(mailItem);
        Assert.Single(attachmentProxy.GetInvocations(nameof(IMailAttachment.EnqueueDelete)));

        using CharacterContext context = CreateContext();
        mailItem.Save(context);

        Assert.Single(attachmentProxy.GetInvocations(nameof(IMailAttachment.Save)));
    }

    private static CharacterMailModel MailModel(
        ulong id = 1ul,
        ulong recipientId = 100ul,
        ulong senderId = 200ul,
        string subject = "Subject",
        SenderType senderType = SenderType.Player)
    {
        return new CharacterMailModel
        {
            Id                         = id,
            RecipientId                = recipientId,
            SenderType                 = (byte)senderType,
            SenderId                   = senderId,
            Subject                    = subject,
            Message                    = "Message",
            CurrencyType               = (byte)CurrencyType.Credits,
            CurrencyAmount             = 123ul,
            IsCashOnDelivery           = 0,
            HasPaidOrCollectedCurrency = 0,
            Flags                      = 0,
            DeliveryTime               = (byte)DeliverySpeed.Instant,
            CreateTime                 = DateTime.UtcNow
        };
    }

    private static CharacterContext CreateContext()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new CharacterContext(options);
    }
}
