using NexusForever.Game.Abstract.Mail;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        // Only copy metadata under this gate. SQL, Inventory mutation and
        // delivery callbacks run after release to preserve their lock order.
        private IMailItem[] AvailableSnapshot()
        {
            lock (mailboxGate) return availableMail.Values.ToArray();
        }
        private IMailItem[] PendingSnapshot()
        {
            lock (mailboxGate) return pendingMail.ToArray();
        }
        private bool TryGetAvailable(ulong id, out IMailItem mail)
        {
            lock (mailboxGate) return availableMail.TryGetValue(id, out mail);
        }
    }
}
