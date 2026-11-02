using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;


namespace NexusForever.Game.Tests.Mail;

public class MailItemTransactionTests
{
    [Fact]
    public void NewMail_UsesUtcCreateTime()
    {
        var assetManager = new AssetManager();

        DateTime beforeUtc = DateTime.UtcNow;

        MailItem mailItem = new(new MailParameters
        {
            RecipientCharacterId = 100ul,
            SenderCharacterId    = 200ul,
            MessageType          = SenderType.Player,
            DeliverySpeed        = DeliverySpeed.Instant
        }, assetManager);

        Assert.Equal(DateTimeKind.Utc, mailItem.CreateTime.Kind);
        Assert.InRange(mailItem.CreateTime, beforeUtc, DateTime.UtcNow);
    }

    [Fact]
    public void IsReadyToDeliver_TreatsUnspecifiedCreateTimeAsUtc()
    {
        DateTime recentUtc = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-30d), DateTimeKind.Unspecified);
        MailItem mailItem = new(MailModel(deliverySpeed: DeliverySpeed.Hour, createTime: recentUtc));

        Assert.False(mailItem.IsReadyToDeliver());
    }

    [Fact]
    public void PayOrTakeCash_Save_PersistsCurrencyCollectedAndNotReturnable()
    {
        var model = MailModel();
        MailItem mailItem = new(model);

        mailItem.PayOrTakeCash();

        using CharacterContext context = CreateContext(model);
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
        var model = MailModel(id: 2ul, recipientId: 100ul, senderId: 200ul, subject: "Settled auction");
        MailItem mailItem = new(model);

        mailItem.ReturnMail();

        using CharacterContext context = CreateContext(model);
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
        var model = MailModel(id: 3ul);
        MailItem mailItem = new(model);
        IMailAttachment attachment = RecordingDispatchProxy<IMailAttachment>.Create(out RecordingDispatchProxy<IMailAttachment> attachmentProxy);
        mailItem.AttachmentAdd(attachment);

        mailItem.AttachmentDelete(attachment, 0u);

        Assert.Null(mailItem.GetAttachment(0u));
        Assert.Empty(mailItem);
        Assert.Single(attachmentProxy.GetInvocations(nameof(IMailAttachment.EnqueueDelete)));

        using CharacterContext context = CreateContext(model);
        mailItem.Save(context);

        Assert.Single(attachmentProxy.GetInvocations(nameof(IMailAttachment.Save)));
    }

    private static CharacterMailModel MailModel(
        ulong id = 1ul,
        ulong recipientId = 100ul,
        ulong senderId = 200ul,
        string subject = "Subject",
        SenderType senderType = SenderType.Player,
        DeliverySpeed deliverySpeed = DeliverySpeed.Instant,
        DateTime? createTime = null)
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
            DeliveryTime               = (byte)deliverySpeed,
            CreateTime                 = createTime ?? DateTime.UtcNow
        };
    }

    private static CharacterContext CreateContext(CharacterMailModel mail)
    {
        var context = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite($"Data Source=NexusForeverMailHeader_{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False").Options);
        context.Database.OpenConnection(); context.Database.EnsureCreated();
        context.Character.AddRange(new CharacterModel { Id = 100, Name = "Recipient" }, new CharacterModel { Id = 200, Name = "Sender" });
        context.CharacterMail.Add(mail); context.SaveChanges(); context.ChangeTracker.Clear();
        return context;
    }
}
