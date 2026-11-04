using NexusForever.Game.Abstract.Entity;
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
                && (deliverySession is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
                && publicationAllowed?.Invoke() != false;
            bool noticePending = false;
            void Publish()
            {
                if (!CurrentActor()) return;
                try
                {
                    if (!noticePending)
                    {
                        if (!AdoptIncomingMail(mail)) return;
                        noticePending = true;
                    }
                    lock (mailboxGate)
                        if (mail.PendingDelete || !availableMail.TryGetValue(mail.Id, out var current)
                            || !ReferenceEquals(current, mail)) return;
                    if (CurrentActor()) SendIncomingMail(mail);
                }
                catch (Exception error)
                {
                    deliveryLog.Warn(error, "Committed marketplace mail publication failed.");
                    // The inbox already knows this row, so only the notice can be retried.
                    if (noticePending) Queue(Task.Delay(TimeSpan.FromSeconds(1)));
                }
            }
            void Queue(Task delay) => events.EnqueueThreadSafe(new PredicateEvent(
                () => !CurrentActor() || delay.IsCompleted && !IsMailSaveInProgress, Publish));

            Queue(Task.CompletedTask);
        }
    }
}
