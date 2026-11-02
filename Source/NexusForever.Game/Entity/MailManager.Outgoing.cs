using NexusForever.Database.Character;
using NexusForever.Game.Mail;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private readonly Dictionary<MailItem, OutgoingOwner> outgoingOwners = new(ReferenceEqualityComparer.Instance);

        private void SaveOutgoing(CharacterContext context)
        {
            OutgoingOwner[] snapshot;
            lock (outgoingMail)
            {
                snapshot = outgoingMail.Select(queued =>
                {
                    if (queued is not MailItem mail)
                    {
                        var error = new InvalidOperationException("Outgoing mail requires its concrete persistence owner.");
                        context.RejectSave(error); throw error;
                    }
                    if (!outgoingOwners.TryGetValue(mail, out var owner))
                        outgoingOwners.Add(mail, owner = new(this, mail));
                    return owner;
                }).ToArray();
            }
            foreach (var owner in snapshot) owner.Stage(context);
        }

        private void RemoveOutgoing(OutgoingOwner owner)
        {
            lock (outgoingMail)
            {
                var retained = outgoingMail.Where(candidate => !ReferenceEquals(candidate, owner.Mail)).ToArray();
                outgoingMail.Clear(); foreach (var candidate in retained) outgoingMail.Enqueue(candidate);
                if (outgoingOwners.TryGetValue(owner.Mail, out var current) && ReferenceEquals(current, owner))
                    outgoingOwners.Remove(owner.Mail);
            }
        }
    }
}
