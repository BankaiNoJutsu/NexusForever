using NexusForever.Database.Character;
using NexusForever.Game.Mail;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private readonly object expiryPublicationGate = new();
        private readonly Dictionary<MailItem, MailDeletionOwner> expiryDeletions = new(ReferenceEqualityComparer.Instance);

        private bool StageExpiryDeletion(MailItem mail, CharacterContext context)
        {
            MailDeletionOwner owner;
            lock (expiryPublicationGate) expiryDeletions.TryGetValue(mail, out owner);
            if (owner == null) return false;
            owner.Stage(context); return true;
        }
        private void RetainExpiryBatch(MailItem[] mails)
        {
            var batch = new ExpiryBatch(this, mails);
            lock (expiryPublicationGate)
                for (int i = 0; i < mails.Length; ++i) expiryDeletions.Add(mails[i], batch.Owners[i]);
        }
        private sealed class ExpiryBatch
        {
            private readonly MailManager manager;
            private readonly MailItem[] mails;
            private readonly ulong[] ids;
            internal readonly MailDeletionOwner[] Owners;
            private bool finished;

            internal ExpiryBatch(MailManager manager, MailItem[] mails)
            {
                this.manager = manager; this.mails = mails; ids = mails.Select(mail => mail.Id).ToArray();
                Owners = mails.Select(mail => new MailDeletionOwner(manager, mail, Publish)).ToArray();
            }
            // Only original-actor callbacks run this method. Member proof remains
            // ready after its callback so a later member can complete the batch.
            private void Publish()
            {
                if (finished || manager.DeliveryActor() && Owners.Any(owner => !owner.IsReady)) return;
                finished = true;
                lock (manager.expiryPublicationGate)
                    for (int i = 0; i < mails.Length; ++i)
                        if (manager.expiryDeletions.TryGetValue(mails[i], out var current) && ReferenceEquals(current, Owners[i]))
                            manager.expiryDeletions.Remove(mails[i]);
                if (!manager.DeliveryActor()) return;
                var packet = new ServerMailItemDeprecation(); packet.MailIds.AddRange(ids);
                manager.deliverySession.EnqueueMessageEncrypted(packet);
            }
        }
    }
}
