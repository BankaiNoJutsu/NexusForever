using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailSelectionCollectsEveryEligibleAttachmentAndGift(bool gift)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingAttachmentMail(f, 2, gift ? 30ul : 0ul);
        manager.MailTakeAllFromSelection([mail.Id], 1); await WaitForBankMove(f.Inventory);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(2, read.Item.Count()); Assert.All(read.Item, item => Assert.Equal(100ul, item.OwnerId));
        Assert.Empty(read.CharacterMailAttachment); Assert.Equal(gift ? 130ul : 100ul, read.CharacterCurrency.Single().Amount);
        var replies = BankMessages(f.Player.Session).OfType<ServerMailTakeAttachment>().ToArray();
        Assert.Equal(2, replies.Length); Assert.All(replies, reply => Assert.Equal(GenericError.Ok, reply.Result));
        Assert.Equal(gift ? 1 : 0, read.CharacterMail.Single().HasPaidOrCollectedCurrency);
    }
}
