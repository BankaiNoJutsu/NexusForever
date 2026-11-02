using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData("header")] [InlineData("membership")] [InlineData("trigger")]
    public async Task MailCashRejectsForeignHeaderMembershipAndSqlRewrites(string conflict)
    {
        var f = await MailFixture(command => command.ExecuteAsync(async attempt =>
        {
            using (var alter = new CharacterContext(Configuration))
            {
                if (conflict == "header")
                {
                    alter.CharacterMail.Single(row => row.Id == 600).CurrencyAmount = 999;
                    await alter.SaveChangesAsync();
                }
                if (conflict == "membership")
                {
                    alter.CharacterMailAttachment.Add(new CharacterMailAttachmentModel { Id = 600, Index = 0, ItemGuid = 900 });
                    await alter.SaveChangesAsync();
                }
            }
            await attempt();
        }));
        var (manager, mail) = await IncomingCashMail(f, false);
        using (var seed = new CharacterContext(Configuration))
        {
            if (conflict == "membership")
            {
                var item = CharacterRow(); item.Id = 900; item.OwnerId = null; item.Location = (ushort)InventoryLocation.None;
                seed.Item.Add(item); await seed.SaveChangesAsync();
            }
            if (conflict == "trigger") await seed.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? "CREATE TRIGGER rewrite_cash AFTER UPDATE ON character_mail WHEN NEW.id=600 BEGIN UPDATE character_mail SET currencyAmount=999 WHERE id=NEW.id; END;"
                : "CREATE TRIGGER rewrite_cash BEFORE UPDATE ON character_mail FOR EACH ROW SET NEW.currencyAmount=999");
        }
        manager.MailTakeCash(mail.Id, 1); await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(f.Inventory.RequiresAuthoritativeReload); Assert.False(mail.HasPaidOrCollectedCurrency);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(GenericError.DbFailure, Assert.Single(BankMessages(f.Player.Session).OfType<ServerMailResult>()).Result);
        using var read = new CharacterContext(Configuration);
        var stored = Assert.Single(read.CharacterMail);
        Assert.Equal(0, stored.HasPaidOrCollectedCurrency); Assert.Equal(conflict == "header" ? 999ul : 30ul, stored.CurrencyAmount);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(conflict == "membership" ? 1 : 0, read.CharacterMailAttachment.Count());
    }
}
