using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal bool CollectMailCash(MailItem mail, MailItem settlement, Func<bool> currentManager, Func<bool> currentMail, Action<MailItem, uint> notify)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated || IsRealmBankPersistenceBlocked
                    || player.CurrencyManager is not CurrencyManager currencies || !currentManager() || !currentMail()
                    || mail.RecipientId != characterId) return false;
                using var batch = BeginRealmBankNotifications(true);
                if (batch == null) return false;
                MailResultAction action = mail.IsCashOnDelivery ? MailResultAction.PayCashOnDelivery : MailResultAction.TakeCash;
                batch.OnFailure(new ServerMailResult { Action = action, MailId = mail.Id, Result = GenericError.DbFailure });
                try
                {
                    // CurrentActor is also observed by a terminal worker tail.
                    // The manager reference is safe there; mailbox membership
                    // remains an actor-only adoption/publication check below.
                    batch.RequireActor(currentManager);
                    var receipt = mail.CaptureCashReceipt(characterId); batch.Attach(receipt);
                    var payment = currencies.CaptureBankCurrency(player,
                        [new CurrencyManager.BankCurrencyChange(mail.IsCashOnDelivery ? CurrencyType.Credits : mail.CurrencyType,
                            mail.CurrencyAmount, mail.IsCashOnDelivery)], () => batch.CurrentActor() && currentMail(), batch.After);
                    batch.Attach(payment);
                    if (settlement != null)
                    {
                        var delivery = new BankMailDelivery(settlement, characterId); batch.Attach(delivery);
                        batch.AfterSave(() => notify?.Invoke(settlement, delivery.RecipientAccount));
                    }
                    batch.Add(payment.Publish);
                    batch.Add(new ServerMailResult { Action = action, MailId = mail.Id, Result = GenericError.Ok });
                    batch.Complete(); return true;
                }
                catch (Exception error)
                {
                    log.Error(error, "Mail cash preparation failed."); return false;
                }
            }
        }
    }
}
