using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false, "header")] [InlineData(false, "currency")]
    [InlineData(true, "header")] [InlineData(true, "currency")] [InlineData(true, "settlement")]
    public async Task MailCashFailureRollsBackTheWalletReceiptAndCodSettlement(bool cod, string fault)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, cod);
        await SplitTrigger(fault == "currency" ? "character_currency" : "character_mail", fault == "settlement" ? "INSERT" : "UPDATE");
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(f.Inventory.RequiresAuthoritativeReload); Assert.False(mail.HasPaidOrCollectedCurrency);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(0, Assert.Single(read.CharacterMail).HasPaidOrCollectedCurrency);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task MailCashRetryKeepsOneReceiptWalletChangeAndCodSettlement(bool cod, bool lostAck)
    {
        int attempts = 0;
        var f = await MailFixture(command => command.ExecuteAsync(async attempt =>
        {
            try
            {
                ++attempts; await attempt();
                if (lostAck) throw new IOException("Lost mail cash commit acknowledgement.");
            }
            catch (Exception error) when (lostAck ? error is IOException : error is DbUpdateException)
            {
                if (!lostAck) { using var drop = new CharacterContext(Configuration); await drop.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split"); }
                ++attempts; await attempt();
            }
        }));
        var (manager, mail) = await IncomingCashMail(f, cod);
        if (!lostAck) await SplitTrigger("character_currency", "UPDATE");
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        await WaitForBankMove(f.Inventory, f.Item);
        Assert.Equal(2, attempts); Assert.False(f.Inventory.RequiresAuthoritativeReload);
        Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(cod ? 70ul : 130ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(1, read.CharacterMail.Single(row => row.Id == mail.Id).HasPaidOrCollectedCurrency);
        Assert.Equal(cod ? 2 : 1, read.CharacterMail.Count());
        if (cod) Assert.Equal(701ul, read.CharacterMail.Single(row => row.Id != mail.Id).Id);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailCashWaitsForCommitAndRetainsLaterReadFlags(bool cod)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { entered.SetResult(); await release.Task; await attempt(); }));
        var (manager, mail) = await IncomingCashMail(f, cod);
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            Assert.Empty(BankMessages(f.Player.Session)); Assert.False(mail.HasPaidOrCollectedCurrency);
            Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
            using var read = new CharacterContext(Configuration);
            Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Single(read.CharacterMail);
            manager.MailMarkAsRead(mail.Id);
            manager.MailDelete(mail.Id); manager.ReturnMail(mail.Id);
            if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
            Assert.Equal(3, BankMessages(f.Player.Session).OfType<ServerMailResult>().Count());
            Assert.All(BankMessages(f.Player.Session).OfType<ServerMailResult>(), result => Assert.Equal(GenericError.MailBusy, result.Result));
            using var blocked = new CharacterContext(Configuration);
            Assert.Throws<InvalidOperationException>(() => mail.Save(blocked)); Assert.True(blocked.IsSaveRejected);
        }
        finally { release.TrySetResult(); }
        await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(mail.HasPaidOrCollectedCurrency); Assert.True((mail.Flags & MailFlag.IsRead) != 0);
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Equal(0, read.CharacterMail.Single(row => row.Id == mail.Id).Flags & (byte)MailFlag.IsRead);
            Assert.Equal(1, read.CharacterMail.Single(row => row.Id == mail.Id).HasPaidOrCollectedCurrency);
        }
        using (var save = new CharacterContext(Configuration)) { mail.Save(save); await save.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration);
        Assert.NotEqual(0, final.CharacterMail.Single(row => row.Id == mail.Id).Flags & (byte)MailFlag.IsRead);
        Assert.Equal(1, final.CharacterMail.Single(row => row.Id == mail.Id).HasPaidOrCollectedCurrency);
        Assert.Equal(cod ? 70ul : 130ul, final.CharacterCurrency.Single().Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MailCashLosingItsMailManagerCannotPublishIntoTheReplacement(bool cod)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await MailFixture(command => command.ExecuteAsync(async attempt => { entered.SetResult(); await release.Task; await attempt(); }));
        var (manager, mail) = await IncomingCashMail(f, cod);
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), f.Mail);
        release.TrySetResult(); await WaitForBankMove(f.Inventory, f.Item);
        Assert.Empty(BankMessages(f.Player.Session)); Assert.True(f.Inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(cod ? 70ul : 130ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(1, read.CharacterMail.Single(row => row.Id == mail.Id).HasPaidOrCollectedCurrency);
        Assert.Equal(cod ? 2 : 1, read.CharacterMail.Count());
    }
}
