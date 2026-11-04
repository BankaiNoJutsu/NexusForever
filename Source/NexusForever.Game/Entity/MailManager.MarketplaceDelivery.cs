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

            var events = deliverySession.Events;
            if (events == null)
                return;
            bool CurrentActor() => DeliveryActor() && ReferenceEquals(deliverySession.Events, events)
                && (deliverySession is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
                && publicationAllowed?.Invoke() != false;
            void Queue(Task delay, Action action) => events.EnqueueThreadSafe(new PredicateEvent(
                () => !CurrentActor() || delay.IsCompleted && !IsMailSaveInProgress, action));

            bool NeedsRoute()
            {
                if (!CurrentActor()) return false;
                lock (mailboxGate)
                    return !knownMail.Contains(mail.Id);
            }

            bool MatchesRoute()
            {
                using var context = deliveryContexts();
                return Item.GetRewardDatabaseKey(context) == database;
            }

            void RetryRoute(Exception error)
            {
                if (!CurrentActor()) return;
                deliveryLog.Warn(error, "Committed marketplace mail route check failed; retrying for its original actor.");
                Queue(Task.Delay(TimeSpan.FromSeconds(1)), () =>
                {
                    if (!NeedsRoute()) return;
                    // Provider discovery can connect to MySQL. Keep recovery off the actor thread.
                    var lookup = Task.Run(MatchesRoute);
                    events.EnqueueThreadSafe(new TaskGenericEvent<bool>(lookup, matches =>
                    {
                        if (matches && CurrentActor()) Queue(Task.CompletedTask, Publish);
                    }, () => RetryRoute(lookup.Exception)));
                });
            }

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
                    if (noticePending) Queue(Task.Delay(TimeSpan.FromSeconds(1)), Publish);
                }
            }

            if (!NeedsRoute()) return;
            bool initialRouteMatches;
            try { initialRouteMatches = MatchesRoute(); }
            catch (Exception error) { RetryRoute(error); return; }
            if (initialRouteMatches) Queue(Task.CompletedTask, Publish);
        }
    }
}
