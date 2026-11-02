using NexusForever.Game.Mail;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        internal void EnqueueMarketplaceMail(MailItem mail, uint account, string database, Func<bool> publicationAllowed = null)
        {
            if (!DeliveryActor() || deliveryAccountId != account || deliveryCharacter != mail.RecipientId
                || deliveryContexts == null)
                return;

            // Route discovery belongs to the producer thread, outside mailbox
            // metadata locks. The actor callback performs no provider work.
            using var context = deliveryContexts();
            if (Item.GetRewardDatabaseKey(context) != database)
                return;

            var events = deliverySession.Events;
            if (events == null)
                return;
            bool CurrentActor() => DeliveryActor() && ReferenceEquals(deliverySession.Events, events)
                && publicationAllowed?.Invoke() != false;
            events.EnqueueThreadSafe(new PredicateEvent(
                () => !CurrentActor() || !IsMailSaveInProgress,
                () =>
                {
                    if (!CurrentActor()) return;
                    try { EnqueueMail(mail); }
                    catch (Exception error)
                    {
                        deliveryLog.Warn(error, "Committed marketplace mail publication failed; SQL inbox recovery remains available.");
                    }
                }));
        }
    }
}
