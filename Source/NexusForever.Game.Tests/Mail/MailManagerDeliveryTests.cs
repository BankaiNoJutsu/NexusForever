using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Mail;

[Collection(LegacyServiceProviderCollection.Name)]
public class MailManagerDeliveryTests
{
    [Fact]
    public void Update_PromotesReadyPendingMailOnlyOnce()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateReadyMail(123ul);
        GetPendingMail(manager).Add(mail);

        manager.Update(1000d);
        manager.Update(1000d);

        ServerMailAvailable available = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailAvailable>()
            .Single();
        ServerMailAvailable.Mail availableMail = Assert.Single(available.MailList);
        Assert.Equal(123ul, availableMail.MailId);
        Assert.Empty(GetPendingMail(manager));
    }

    [Fact]
    public void MailPayCod_QueuesInstantCashSettlementToSender()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(new AssetManager())
            .BuildServiceProvider();
        LegacyServiceProvider.Provider = provider;

        try
        {
            IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
            ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out RecordingDispatchProxy<ICurrencyManager> currencyProxy);
            currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
            playerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul);
            playerProxy.SetProperty(nameof(IPlayer.Session), session);
            playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

            var manager = new MailManager(player, new CharacterModel());
            MailItem codMail = new(MailModel(
                id: 321ul,
                recipientId: 100ul,
                senderId: 200ul,
                subject: "Crafted widget",
                currencyAmount: 550ul,
                isCashOnDelivery: true));
            GetAvailableMail(manager).Add(codMail.Id, codMail);

            manager.MailPayCod(codMail.Id);

            RecordingDispatchProxy<ICurrencyManager>.Invocation debit = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
            Assert.Equal(550ul, debit.Arguments[1]);
            Assert.Equal(false, debit.Arguments[2]);

            Assert.True(codMail.HasPaidOrCollectedCurrency);
            Assert.Equal(MailFlag.NotReturnable, codMail.Flags & MailFlag.NotReturnable);

            IMailItem settlement = Assert.Single(GetOutgoingMail(manager));
            Assert.Equal(200ul, settlement.RecipientId);
            Assert.Equal(100ul, settlement.SenderId);
            Assert.Equal(SenderType.Player, settlement.SenderType);
            Assert.Equal("Cash from: Crafted widget", settlement.Subject);
            Assert.Equal(550ul, settlement.CurrencyAmount);
            Assert.False(settlement.IsCashOnDelivery);
            Assert.Equal(DeliverySpeed.Instant, settlement.DeliverySpeed);

            ServerMailResult result = GetEncryptedMessages(sessionProxy)
                .OfType<ServerMailResult>()
                .Single();
            Assert.Equal(MailResultAction.PayCashOnDelivery, result.Action);
            Assert.Equal(321ul, result.MailId);
            Assert.Equal(GenericError.Ok, result.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void Update_ExpiresAvailableMailAndNotifiesClient()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateExpiredMail(456ul, out RecordingDispatchProxy<IMailItem> mailProxy);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.Update(1000d);

        Assert.Empty(GetAvailableMail(manager));
        Assert.Same(mail, Assert.Single(GetExpiredMail(manager)));
        RecordingDispatchProxy<IMailItem>.Invocation deleteCall =
            Assert.Single(mailProxy.GetInvocations(nameof(IMailItem.EnqueueDelete)));
        Assert.Equal(true, deleteCall.Arguments[0]);

        ServerMailItemDeprecation deprecation = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailItemDeprecation>()
            .Single();
        Assert.Equal(456ul, Assert.Single(deprecation.MailIds));
        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerMailUnavailable>());
    }

    [Fact]
    public void Update_DoesNotExpireAvailableMailBeforeExactExpiryInstant()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateMailNearExpiry(457ul, beforeExpiry: true);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.Update(1000d);

        Assert.Same(mail, Assert.Single(GetAvailableMail(manager).Values));
        Assert.Empty(GetExpiredMail(manager));
        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerMailItemDeprecation>());
    }

    [Fact]
    public void Update_ExpiresAvailableMailOnceExactExpiryInstantPasses()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateMailNearExpiry(458ul, beforeExpiry: false);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.Update(1000d);

        Assert.Empty(GetAvailableMail(manager));
        Assert.Same(mail, Assert.Single(GetExpiredMail(manager)));
        ServerMailItemDeprecation deprecation = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailItemDeprecation>()
            .Single();
        Assert.Equal(458ul, Assert.Single(deprecation.MailIds));
    }

    [Fact]
    public void EnqueueMail_SendsServerMailAvailableForReadyMail()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateReadyMail(459ul);

        manager.EnqueueMail(mail);

        Assert.Same(mail, Assert.Single(GetAvailableMail(manager).Values));
        ServerMailAvailable available = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailAvailable>()
            .Single();
        Assert.True(available.NewMail);
        Assert.Equal(459ul, Assert.Single(available.MailList).MailId);
    }

    [Fact]
    public void MailDelete_RemovesMailFromAvailableImmediately()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateReadyMail(460ul);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.MailDelete(460ul);

        Assert.Empty(GetAvailableMail(manager));
        ServerMailUnavailable unavailable = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailUnavailable>()
            .Single();
        Assert.Equal(460ul, unavailable.MailId);
    }

    [Fact]
    public void Update_ExpiresPendingMailWithoutClientNotification()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateExpiredMail(789ul, out RecordingDispatchProxy<IMailItem> mailProxy);
        GetPendingMail(manager).Add(mail);

        manager.Update(1000d);

        Assert.Empty(GetPendingMail(manager));
        Assert.Same(mail, Assert.Single(GetExpiredMail(manager)));
        RecordingDispatchProxy<IMailItem>.Invocation deleteCall =
            Assert.Single(mailProxy.GetInvocations(nameof(IMailItem.EnqueueDelete)));
        Assert.Equal(true, deleteCall.Arguments[0]);
        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerMailUnavailable>());
    }

    [Fact]
    public void MailDelete_EnqueuesDeleteUnavailableAndResult()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateReadyMail(654ul, out RecordingDispatchProxy<IMailItem> mailProxy);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.MailDelete(654ul);

        RecordingDispatchProxy<IMailItem>.Invocation deleteCall =
            Assert.Single(mailProxy.GetInvocations(nameof(IMailItem.EnqueueDelete)));
        Assert.Equal(true, deleteCall.Arguments[0]);
        ServerMailUnavailable unavailable = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailUnavailable>()
            .Single();
        Assert.Equal(654ul, unavailable.MailId);
        ServerMailResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailResult>()
            .Single();
        Assert.Equal(MailResultAction.Delete, result.Action);
        Assert.Equal(654ul, result.MailId);
        Assert.Equal(GenericError.Ok, result.Result);
    }

    [Fact]
    public void MailDelete_MissingMailReturnsDoesNotExistWithoutUnavailable()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());

        manager.MailDelete(655ul);

        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerMailUnavailable>());
        ServerMailResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailResult>()
            .Single();
        Assert.Equal(MailResultAction.Delete, result.Action);
        Assert.Equal(655ul, result.MailId);
        Assert.Equal(GenericError.MailDoesNotExist, result.Result);
    }

    [Fact]
    public void ReturnMail_MovesMailToOutgoingUnavailableAndResult()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateReadyMail(756ul, out RecordingDispatchProxy<IMailItem> mailProxy);
        mailProxy.SetProperty(nameof(IMailItem.Flags), MailFlag.None);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.ReturnMail(756ul);

        Assert.Empty(GetAvailableMail(manager));
        Assert.Same(mail, Assert.Single(GetOutgoingMail(manager)));
        Assert.Single(mailProxy.GetInvocations(nameof(IMailItem.ReturnMail)));
        ServerMailUnavailable unavailable = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailUnavailable>()
            .Single();
        Assert.Equal(756ul, unavailable.MailId);
        ServerMailResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailResult>()
            .Single();
        Assert.Equal(MailResultAction.Send, result.Action);
        Assert.Equal(756ul, result.MailId);
        Assert.Equal(GenericError.Ok, result.Result);
    }

    [Fact]
    public void ReturnMail_NotReturnableReturnsCannotReturnWithoutMoving()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        var manager = new MailManager(player, new CharacterModel());
        IMailItem mail = CreateReadyMail(757ul, out RecordingDispatchProxy<IMailItem> mailProxy);
        mailProxy.SetProperty(nameof(IMailItem.Flags), MailFlag.NotReturnable);
        GetAvailableMail(manager).Add(mail.Id, mail);

        manager.ReturnMail(757ul);

        Assert.Same(mail, Assert.Single(GetAvailableMail(manager).Values));
        Assert.Empty(GetOutgoingMail(manager));
        Assert.Empty(mailProxy.GetInvocations(nameof(IMailItem.ReturnMail)));
        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerMailUnavailable>());
        ServerMailResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerMailResult>()
            .Single();
        Assert.Equal(MailResultAction.Send, result.Action);
        Assert.Equal(757ul, result.MailId);
        Assert.Equal(GenericError.MailCannotReturn, result.Result);
    }

    private static IMailItem CreateReadyMail(ulong id)
    {
        return CreateReadyMail(id, out _);
    }

    private static IMailItem CreateReadyMail(ulong id, out RecordingDispatchProxy<IMailItem> mailProxy)
    {
        IMailItem mail = RecordingDispatchProxy<IMailItem>.Create(out mailProxy);
        mailProxy.SetProperty(nameof(IMailItem.Id), id);
        mailProxy.SetProperty(nameof(IMailItem.CreateTime), DateTime.UtcNow);
        mailProxy.SetProperty(nameof(IMailItem.ExpiryTime), 0f);
        mailProxy.SetMethodReturn(nameof(IMailItem.IsReadyToDeliver), true);
        mailProxy.SetMethodReturn(nameof(IMailItem.Build), new ServerMailAvailable.Mail
        {
            MailId = id
        });
        return mail;
    }

    private static IMailItem CreateExpiredMail(ulong id, out RecordingDispatchProxy<IMailItem> mailProxy)
    {
        IMailItem mail = RecordingDispatchProxy<IMailItem>.Create(out mailProxy);
        mailProxy.SetProperty(nameof(IMailItem.Id), id);
        mailProxy.SetProperty(nameof(IMailItem.CreateTime), DateTime.UtcNow.AddDays(-2d));
        mailProxy.SetProperty(nameof(IMailItem.ExpiryTime), 1f);
        mailProxy.SetMethodReturn(nameof(IMailItem.IsReadyToDeliver), false);
        return mail;
    }

    private static IMailItem CreateMailNearExpiry(ulong id, bool beforeExpiry)
    {
        IMailItem mail = RecordingDispatchProxy<IMailItem>.Create(out RecordingDispatchProxy<IMailItem> mailProxy);
        mailProxy.SetProperty(nameof(IMailItem.Id), id);
        mailProxy.SetProperty(nameof(IMailItem.CreateTime), beforeExpiry
            ? DateTime.UtcNow.AddDays(-1d).AddMinutes(5d)
            : DateTime.UtcNow.AddDays(-1d).AddMinutes(-5d));
        mailProxy.SetProperty(nameof(IMailItem.ExpiryTime), 1f);
        mailProxy.SetMethodReturn(nameof(IMailItem.IsReadyToDeliver), true);
        mailProxy.SetMethodReturn(nameof(IMailItem.Build), new ServerMailAvailable.Mail
        {
            MailId = id
        });
        return mail;
    }

    private static List<IMailItem> GetPendingMail(MailManager manager)
    {
        FieldInfo field = typeof(MailManager).GetField("pendingMail", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<IMailItem>)field.GetValue(manager)!;
    }

    private static Dictionary<ulong, IMailItem> GetAvailableMail(MailManager manager)
    {
        FieldInfo field = typeof(MailManager).GetField("availableMail", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Dictionary<ulong, IMailItem>)field.GetValue(manager)!;
    }

    private static Queue<IMailItem> GetOutgoingMail(MailManager manager)
    {
        FieldInfo field = typeof(MailManager).GetField("outgoingMail", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Queue<IMailItem>)field.GetValue(manager)!;
    }

    private static List<IMailItem> GetExpiredMail(MailManager manager)
    {
        FieldInfo field = typeof(MailManager).GetField("expiredMail", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<IMailItem>)field.GetValue(manager)!;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static CharacterMailModel MailModel(
        ulong id,
        ulong recipientId,
        ulong senderId,
        string subject,
        ulong currencyAmount,
        bool isCashOnDelivery)
    {
        return new CharacterMailModel
        {
            Id                         = id,
            RecipientId                = recipientId,
            SenderType                 = (byte)SenderType.Player,
            SenderId                   = senderId,
            Subject                    = subject,
            Message                    = "Message",
            CurrencyType               = (byte)CurrencyType.Credits,
            CurrencyAmount             = currencyAmount,
            IsCashOnDelivery           = Convert.ToByte(isCashOnDelivery),
            HasPaidOrCollectedCurrency = 0,
            Flags                      = 0,
            DeliveryTime               = (byte)DeliverySpeed.Instant,
            CreateTime                 = DateTime.UtcNow
        };
    }
}
