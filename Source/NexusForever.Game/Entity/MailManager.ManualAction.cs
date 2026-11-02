using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Mail;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private readonly object manualActionGate = new();
        private readonly Dictionary<ulong, ManualAction> manualActions = [];
        private bool ManualActionActor() => DeliveryActor() && deliverySession.Events != null;

        private bool HasManualAction(ulong id) { lock (manualActionGate) return manualActions.ContainsKey(id); }
        private void RetainManualAction(MailItem mail, MailResultAction action)
        {
            lock (manualActionGate) manualActions.Add(mail.Id, new(this, mail, action));
        }
        private ManualAction FindManualAction(MailItem mail)
        {
            lock (manualActionGate)
                return manualActions.TryGetValue(mail.Id, out var action) && ReferenceEquals(action.Mail, mail) ? action : null;
        }
        private bool StageRetainedDeletion(IMailItem mail, CharacterContext context)
        {
            if (mail is not MailItem concrete) return false;
            if (FindManualAction(concrete) is { IsDeletion: true } action)
            {
                action.StageDeletion(context); return true;
            }
            return StageExpiryDeletion(concrete, context);
        }
        private void PublishManualReturn(MailItem mail) => FindManualAction(mail)?.Publish();
        private void RetireManualAction(MailItem mail) => FindManualAction(mail)?.Retire();

        // SQL settlement records proof; only the original session actor publishes.
        // This owner outlives expiredMail's confirmed-tombstone cleanup.
        private sealed class ManualAction
        {
            private readonly MailManager manager;
            private readonly MailResultAction action;
            private readonly MailDeletionOwner deletion;
            internal readonly MailItem Mail;
            internal bool IsDeletion => action == MailResultAction.Delete;
            private readonly object gate = new();
            private bool finished;

            internal ManualAction(MailManager manager, MailItem mail, MailResultAction action)
            {
                this.manager = manager; this.action = action; Mail = mail;
                if (IsDeletion) deletion = new(manager, mail, Publish);
            }
            internal void StageDeletion(CharacterContext context) => deletion.Stage(context);
            internal void Publish()
            {
                lock (gate)
                {
                    if (finished) return;
                    if (IsDeletion && !deletion.IsReady && manager.DeliveryActor()) return;
                    finished = true;
                }
                try
                {
                    if (!manager.DeliveryActor()) return;
                    manager.deliverySession.EnqueueMessageEncrypted(new ServerMailUnavailable { MailId = Mail.Id });
                    if (!manager.DeliveryActor()) return;
                    manager.deliverySession.EnqueueMessageEncrypted(new ServerMailResult { MailId = Mail.Id, Action = action, Result = GenericError.Ok });
                }
                finally { Remove(); }
            }
            internal void Retire()
            {
                lock (gate) finished = true;
                deletion?.Retire();
                Remove();
            }
            private void Remove()
            {
                lock (manager.manualActionGate)
                    if (manager.manualActions.TryGetValue(Mail.Id, out var current) && ReferenceEquals(current, this))
                        manager.manualActions.Remove(Mail.Id);
            }
        }
    }
}
