using NexusForever.Database.Character;

namespace NexusForever.Game.Mail
{
    public partial class MailItem
    {
        internal interface IOutgoingDelivery
        {
            bool IsReady { get; }
            bool IsValid { get; }
            bool TryAccept();
        }

        internal sealed record ConfirmedOutgoing(MailHeaderState Header, IReadOnlyList<MailAttachmentState> Attachments, string Database);

        // A receipt belongs to this exact enrollment, including its last flush.
        // A committed outer transaction with no flush cannot confirm delivery.
        internal OutgoingReceipt SaveOutgoing(CharacterContext context)
        {
            SavePersistence(context, force: true);
            lock (persistence.Gate)
            {
                if (!ReferenceEquals(membership?.Context, context))
                    throw new InvalidOperationException("Outgoing mail has no complete enrolled save unit.");
                return new(this, membership);
            }
        }

        internal sealed class OutgoingReceipt(MailItem owner, MembershipUnit unit)
        {
            internal string Database => owner.persistenceDatabase;
            internal ulong Recipient => unit.HeaderTarget?.RecipientId ?? owner.RecipientId;
            internal ConfirmedOutgoing Confirmation
            {
                get
                {
                    lock (owner.persistence.Gate)
                        return unit.Settled && unit.Committed && unit.Flushed
                            ? new(unit.HeaderSql, Array.AsReadOnly(unit.Sql.ToArray()), owner.persistenceDatabase) : null;
                }
            }
        }
    }
}
