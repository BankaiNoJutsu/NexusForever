using NexusForever.Database.Character;

namespace NexusForever.Game.Mail
{
    public partial class MailItem
    {
        internal DeletionReceipt SaveDeletion(CharacterContext context)
        {
            lock (persistence.Gate)
            {
                if (!deleteRequested) throw new InvalidOperationException("Mail has no requested deletion.");
                SavePersistence(context, force: true);
                if (!ReferenceEquals(membership?.Context, context))
                    throw new InvalidOperationException("Mail deletion has no complete enrolled save unit.");
                return new(this, membership);
            }
        }
        internal sealed class DeletionReceipt(MailItem owner, MembershipUnit unit)
        {
            internal bool IsConfirmed
            {
                get
                {
                    lock (owner.persistence.Gate)
                        return unit.Settled && unit.Committed && unit.Flushed && unit.HeaderSql == null && unit.Sql.Length == 0;
                }
            }
        }
    }
}
