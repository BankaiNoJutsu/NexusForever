using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal bool SendPlayerMail(MailItem mail, ulong debit, Action<MailItem, uint> notify)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated || IsRealmBankPersistenceBlocked
                    || player.CurrencyManager is not CurrencyManager currencies) return false;
                var attachments = mail.Select(a => a.Item).ToArray();
                if (attachments.Any(i => i is not Item || i.CharacterId != characterId || !ReferenceEquals(GetItem(i.Guid), i))) return false;
                using var batch = BeginRealmBankNotifications(true);
                if (batch == null) return false;
                batch.OnFailure(new ServerMailResult { Action = MailResultAction.Send, Result = GenericError.DbFailure });
                try
                {
                    var delivery = new BankMailDelivery(mail, characterId);
                    var payment = currencies.CaptureBankCurrency(player,
                        debit == 0 ? [] : [new CurrencyManager.BankCurrencyChange(CurrencyType.Credits, debit, true)], batch.CurrentActor, batch.After);
                    batch.Attach(payment); batch.Attach(delivery);
                    foreach (Item item in attachments)
                    {
                        ItemRemove(item); batch.StageDetached(item);
                    }
                    batch.Add(payment.Publish);
                    batch.Add(new ServerMailResult { Action = MailResultAction.Send, Result = GenericError.Ok });
                    batch.AfterSave(() => notify?.Invoke(mail, delivery.RecipientAccount));
                    batch.Complete(); return true;
                }
                catch (Exception error)
                {
                    log.Error(error, "Player mail preparation failed.");
                    return false;
                }
            }
        }
    }
}
