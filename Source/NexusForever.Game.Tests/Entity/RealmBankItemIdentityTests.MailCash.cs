using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CashCollectionRejectsCodAndCodPaymentRejectsGift(bool cod)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, cod);
        if (cod) manager.MailTakeCash(mail.Id, 1); else manager.MailPayCod(mail.Id);
        await WaitForBankMove(f.Inventory, f.Item);
        Assert.False(mail.HasPaidOrCollectedCurrency);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        var result = Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>());
        Assert.NotEqual(GenericError.Ok, result.Result);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(0, read.CharacterMail.Single().HasPaidOrCollectedCurrency);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Single(read.CharacterMail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CashCollectionAndCodPaymentMutateTheirSelectedMode(bool cod)
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, cod);
        if (cod) manager.MailPayCod(mail.Id); else manager.MailTakeCash(mail.Id, 1);
        await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(mail.HasPaidOrCollectedCurrency);
        Assert.Equal(cod ? 70ul : 130ul, Assert.Single(f.Currency).Amount);
        var result = Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>());
        Assert.Equal(GenericError.Ok, result.Result);
        Assert.Equal(cod ? MailResultAction.PayCashOnDelivery : MailResultAction.TakeCash, result.Action);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(cod ? 70ul : 130ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(1, read.CharacterMail.Single(row => row.Id == mail.Id).HasPaidOrCollectedCurrency);
        Assert.Equal(cod ? 2 : 1, read.CharacterMail.Count());
        if (cod)
        {
            var settlement = read.CharacterMail.Single(row => row.Id != mail.Id);
            Assert.Equal(200ul, settlement.RecipientId); Assert.Equal(30ul, settlement.CurrencyAmount);
            Assert.Equal(0, settlement.IsCashOnDelivery);
        }
    }

    [Fact]
    public async Task GiftCollectionRetainsPaidReceiptWhenAnUnrelatedSendPersistsTheWallet()
    {
        var f = await MailFixture(); var (manager, mail) = await IncomingCashMail(f, false);
        manager.MailTakeCash(mail.Id, 1); await WaitForBankMove(f.Inventory, f.Item);
        Assert.Equal(130ul, Assert.Single(f.Currency).Amount);
        manager.SendMail(MailRequest([])); await WaitForBankMove(f.Inventory, f.Item);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(110ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(2, read.CharacterMail.Count());
        Assert.Equal(1, read.CharacterMail.Single(row => row.Id == mail.Id).HasPaidOrCollectedCurrency);
    }

    private async Task<(MailManager Manager, MailItem Mail)> IncomingCashMail(MailState f, bool cod)
    {
        using var seed = new CharacterContext(Configuration);
        var row = new CharacterMailModel
        {
            Id = 600, RecipientId = 100, SenderId = 200, SenderType = (byte)SenderType.Player,
            Subject = "Incoming cash", Message = "Cash receipt regression", CurrencyType = (byte)CurrencyType.Credits,
            CurrencyAmount = 30, IsCashOnDelivery = Convert.ToByte(cod), CreateTime = DateTime.UtcNow.AddMinutes(-1)
        };
        seed.CharacterMail.Add(row); await seed.SaveChangesAsync();
        // The manager represents loaded mail. Use the stored timestamp rather
        // than the seed's higher precision value on providers with datetime(0).
        await seed.Entry(row).ReloadAsync();
        var target = RecordingDispatchProxy<ICharacter>.Create(out var tp); tp.SetProperty(nameof(ICharacter.CharacterId), 200ul);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var cp); cp.SetMethodReturn(nameof(ICharacterManager.GetCharacter), target);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ulong next = 701; ap.SetMethodReturnFactory("get_NextMailId", () => next++);
        var model = new CharacterModel(); model.Mail.Add(row);
        var manager = new MailManager(f.Player, model, assets, characters, realmId: 2, playerManager: f.Players,
            itemManager: f.Items, gameTableManager: f.Tables, deliveryContexts: Database().CreateContext);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.MailManager), manager);
        return (manager, (MailItem)InboxMail(manager));
    }
}
