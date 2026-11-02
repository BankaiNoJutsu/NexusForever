using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailInitialLoginCoordinatesWithTheRetainedOriginal(bool adopted)
    {
        var f = await MailFixture(); var (recipient, _) = RecipientMailbox(f);
        var (manager, mail) = await IncomingCashMail(f, false); manager.ReturnMail(mail.Id);
        using (var context = new CharacterContext(Configuration)) { manager.Save(context); await context.SaveChangesAsync(); }
        if (adopted) f.Player.Session.Events.Update(0);
        using var read = new CharacterContext(Configuration);
        var model = read.Character.AsNoTracking().Include(row => row.Mail).ThenInclude(row => row.Attachment)
            .ThenInclude(row => row.Item).Single(row => row.Id == 200);
        var inbox = new MailManager(recipient, model, realmId: 2, itemManager: f.Items,
            gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)recipient).SetProperty(nameof(IPlayer.MailManager), inbox);
        if (adopted) Assert.Same(mail, InboxMail(inbox));
        else
        {
            Assert.Empty((Dictionary<ulong, NexusForever.Game.Abstract.Mail.IMailItem>)typeof(MailManager)
                .GetField("availableMail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(inbox)!);
            await DrainOutgoingMail(f, recipient); Assert.Same(mail, InboxMail(inbox));
        }
    }
}
