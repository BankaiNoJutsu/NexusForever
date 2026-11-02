using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutgoingMailSaveUsesAnInboxSnapshotWhenAnotherMailArrives(bool ready)
    {
        var f = await MailFixture();
        var first = RecordingDispatchProxy<IMailItem>.Create(out var fp);
        fp.SetProperty(nameof(IMailItem.Id), 800ul);
        var later = RecordingDispatchProxy<IMailItem>.Create(out var lp);
        lp.SetProperty(nameof(IMailItem.Id), 801ul);
        lp.SetMethodReturn(nameof(IMailItem.IsReadyToDeliver), ready);
        int firstSaves = 0, laterSaves = 0;
        fp.SetMethodHandler(nameof(IMailItem.Save), _ =>
        {
            if (++firstSaves == 1) f.Mail.EnqueueMail(later);
            return null;
        });
        lp.SetMethodHandler(nameof(IMailItem.Save), _ => { ++laterSaves; return null; });
        f.Mail.EnqueueMail(first);
        using (var context = new CharacterContext(Configuration)) { f.Mail.Save(context); await context.SaveChangesAsync(); }
        Assert.Equal(1, firstSaves); Assert.Equal(0, laterSaves);
        using (var context = new CharacterContext(Configuration)) { f.Mail.Save(context); await context.SaveChangesAsync(); }
        Assert.Equal(2, firstSaves); Assert.Equal(1, laterSaves);
    }
}
