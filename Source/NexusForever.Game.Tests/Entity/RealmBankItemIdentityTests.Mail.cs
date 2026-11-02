using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MailPreflightRejectsDuplicateAttachmentsAndCombinedCostBeforeAnyMutation(bool duplicate)
    {
        var f = await MailFixture();
        f.Mail.SendMail(MailRequest(duplicate ? [401ul, 401ul] : [401ul], duplicate ? 0ul : 90ul));
        Assert.Same(f.Item, f.Inventory.GetItem(401ul)); Assert.Equal(InventoryLocation.RealmBank, f.Item.Location);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(duplicate ? GenericError.MailInvalidInventorySlot : GenericError.MailInsufficientFunds,
            Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration); Assert.Single(read.RealmBankItem); Assert.Empty(read.Item); Assert.Empty(read.CharacterMail);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("bank")]
    [InlineData("item")]
    [InlineData("mail")]
    [InlineData("attachment")]
    [InlineData("currency")]
    public async Task MailTransferCommitsItsBankWithdrawalAttachmentsAndTotalDebitTogether(string fault)
    {
        var f = await MailFixture();
        if (fault != "none")
            await SplitTrigger(fault switch { "bank" => "realm_bank_item", "item" => "item", "mail" => "character_mail", "attachment" => "character_mail_attachment", _ => "character_currency" },
                fault == "bank" ? "DELETE" : fault == "currency" ? "UPDATE" : "INSERT");
        f.Mail.SendMail(MailRequest([401ul], 30)); await WaitForBankMove(f.Inventory, f.Item);
        using var read = new CharacterContext(Configuration);
        if (fault == "none")
        {
            Assert.Empty(read.RealmBankItem); var stored = Assert.Single(read.Item);
            Assert.Null(stored.OwnerId); Assert.Equal((ushort)InventoryLocation.None, stored.Location); Assert.Equal(401ul, stored.Id);
            Assert.Equal(200ul, Assert.Single(read.CharacterMail).RecipientId); Assert.Equal(30ul, read.CharacterMail.Single().CurrencyAmount);
            Assert.Equal(401ul, Assert.Single(read.CharacterMailAttachment).ItemGuid); Assert.Equal(50ul, Assert.Single(read.CharacterCurrency).Amount);
            Assert.Equal(GenericError.Ok, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        }
        else
        {
            Assert.Single(read.RealmBankItem); Assert.Empty(read.Item); Assert.Empty(read.CharacterMail); Assert.Empty(read.CharacterMailAttachment);
            Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount);
            Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
            Assert.True(f.Inventory.RequiresAuthoritativeReload);
        }
    }

    private sealed record MailState(Inventory Inventory, IPlayer Player, GameItem Item, CurrencyManager Currency, MailManager Mail, IGameTableManager Tables, IItemManager Items, IPlayerManager Players);
    private async Task<MailState> MailFixture(Func<RealmBankManager.BankSaveCommand, Task> execution = null)
    {
        var bank = BankRow(401); bank.Soulbound = false; await Seed(bank: bank);
        using (var seed = new CharacterContext(Configuration)) { seed.Character.Add(new CharacterModel { Id = 200, AccountId = 13, Name = "MailRecipient", OriginalName = "MailRecipient", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow }); await seed.SaveChangesAsync(); }
        var (bankManager, player, items) = Access(execution); var (inventory, _, source) = SwapInventory(bankManager, player, items, null, bank);
        var currency = await InstallBankCurrency(player);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(
            new GameFormulaEntry { Id = 237, Datafloat0 = 5 }, new GameFormulaEntry { Id = 860, Dataint0 = 20 },
            new GameFormulaEntry { Id = 861, Dataint0 = 10, Dataint01 = 10 }, new GameFormulaEntry { Id = 862, Dataint0 = 10, Dataint01 = 10 }, new GameFormulaEntry { Id = 863, Dataint0 = 10, Dataint01 = 10 }));
        var mailbox = RecordingDispatchProxy<IMailboxEntity>.Create(out _);
        var pp = (RecordingDispatchProxy<IPlayer>)(object)player; pp.SetProperty(nameof(IPlayer.SignatureEnabled), true); pp.SetMethodReturn(nameof(IPlayer.GetVisible), mailbox);
        var target = RecordingDispatchProxy<ICharacter>.Create(out var targetProxy); targetProxy.SetProperty(nameof(ICharacter.CharacterId), 200ul);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var cp); cp.SetMethodReturn(nameof(ICharacterManager.GetCharacter), target);
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ulong next = 501; ap.SetMethodReturnFactory("get_NextMailId", () => next++);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        var mail = new MailManager(player, new CharacterModel(), assets, characters, realmId: 2, playerManager: players, itemManager: items, gameTableManager: tables, deliveryContexts: Database().CreateContext);
        pp.SetProperty(nameof(IPlayer.MailManager), mail);
        return new(inventory, player, source, currency, mail, tables, items, players);
    }
    private static ClientMailSend MailRequest(ulong[] items, ulong gift = 0, ulong cod = 0, DeliverySpeed speed = DeliverySpeed.Instant)
    {
        using var stream = new MemoryStream(); using (var writer = new GamePacketWriter(stream))
        {
            writer.WriteStringWide("MailRecipient"); writer.WriteStringWide(""); writer.WriteStringWide("Subject"); writer.WriteStringWide("Body");
            writer.Write(gift); writer.Write(cod); writer.Write(speed, 2u); writer.Write(7u);
            for (int i = 0; i < 10; ++i) writer.Write(i < items.Length ? items[i] : 0ul); writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray())); var result = new ClientMailSend(); result.Read(reader); return result;
    }
}
