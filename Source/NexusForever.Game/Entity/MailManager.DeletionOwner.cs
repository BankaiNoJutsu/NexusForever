using NexusForever.Database.Character;
using NexusForever.Game.Mail;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        // Retains exact normal-save confirmation independently of storage-list
        // cleanup. Shared by a manual result and an available-mail expiry batch.
        private sealed class MailDeletionOwner(MailManager manager, MailItem mail, Action publish)
        {
            private sealed class Attempt(CharacterContext context)
            {
                internal readonly CharacterContext Context = context;
                internal MailItem.DeletionReceipt Receipt;
                internal bool Registered;
            }
            private readonly object gate = new();
            private readonly EventQueue events = manager.deliverySession.Events;
            private Attempt active;
            private bool confirmed, queued, finished;

            internal bool IsReady
            {
                get { lock (gate) return active == null && confirmed && !manager.IsMailSaveInProgress; }
            }
            internal void Stage(CharacterContext context)
            {
                Attempt attempt; bool created;
                lock (gate)
                {
                    if (finished || confirmed) return;
                    if (active != null && !ReferenceEquals(active.Context, context))
                    {
                        var error = new InvalidOperationException("Mail deletion already has an unfinished save attempt.");
                        context.RejectSave(error); throw error;
                    }
                    created = active == null; attempt = active ??= new(context);
                }
                if (created) context.OnSaveCompleted(_ => { if (!attempt.Registered) Finish(attempt, false); });
                try
                {
                    attempt.Receipt = mail.SaveDeletion(context);
                    if (created)
                    {
                        context.OnSaveSettled(committed => Finish(attempt, committed));
                        attempt.Registered = true;
                    }
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
            private void Finish(Attempt attempt, bool committed)
            {
                bool schedule;
                lock (gate)
                {
                    if (!ReferenceEquals(active, attempt)) return;
                    active = null;
                    confirmed = committed && attempt.Receipt?.IsConfirmed == true;
                    schedule = confirmed && !queued && !finished;
                    if (schedule) queued = true;
                }
                if (schedule)
                    events.EnqueueThreadSafe(new PredicateEvent(() => !manager.DeliveryActor() || IsReady, () =>
                    {
                        lock (gate) { if (finished) return; finished = true; }
                        publish();
                    }));
            }
            internal void Retire() { lock (gate) finished = true; }
        }
    }
}
