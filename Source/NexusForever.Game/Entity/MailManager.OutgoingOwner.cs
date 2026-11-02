using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Mail;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private static readonly object outgoingRegistryGate = new();
        private static readonly Dictionary<(string Database, ulong Mail), WeakReference<OutgoingOwner>> outgoingRegistry = [];

        private static void RegisterOutgoing(string database, OutgoingOwner owner)
        {
            lock (outgoingRegistryGate)
            {
                foreach (var key in outgoingRegistry.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToArray())
                    outgoingRegistry.Remove(key);
                var id = (database, owner.Mail.Id);
                if (outgoingRegistry.TryGetValue(id, out var reference) && reference.TryGetTarget(out var prior) && !ReferenceEquals(prior, owner))
                    throw new InvalidOperationException("Outgoing mail belongs to another retained delivery owner.");
                outgoingRegistry[id] = new(owner);
            }
        }
        private static void UnregisterOutgoing(string database, OutgoingOwner owner)
        {
            if (database == null) return;
            lock (outgoingRegistryGate)
                if (outgoingRegistry.TryGetValue((database, owner.Mail.Id), out var reference)
                    && reference.TryGetTarget(out var current) && ReferenceEquals(current, owner))
                    outgoingRegistry.Remove((database, owner.Mail.Id));
        }
        private static bool ResolveOutgoing(string database, CharacterMailModel row, out MailItem original)
        {
            OutgoingOwner owner;
            lock (outgoingRegistryGate)
            {
                original = null;
                if (!outgoingRegistry.TryGetValue((database, row.Id), out var reference)) return true;
                if (!reference.TryGetTarget(out owner)) { outgoingRegistry.Remove((database, row.Id)); return true; }
            }
            // Never call an owner while holding the global registry monitor.
            return owner.Resolve(row, out original);
        }

        private sealed class OutgoingOwner : MailItem.IOutgoingDelivery
        {
            private sealed class Attempt(CharacterContext context)
            {
                internal readonly CharacterContext Context = context;
                internal MailItem.OutgoingReceipt Receipt;
                internal uint Account;
                internal bool SettlementRegistered;
            }
            internal readonly MailItem Mail;
            private readonly MailManager manager;
            private readonly object gate = new();
            private Attempt active;
            private MailItem.ConfirmedOutgoing confirmed;
            private string database;
            private uint? recipientAccount;
            private ulong? recipient;
            private uint confirmedAccount;
            private bool published, quarantined, queued, accepted, recoveryPending;

            internal OutgoingOwner(MailManager manager, MailItem mail)
            {
                this.manager = manager; Mail = mail;
                Mail.OutgoingDelivery = this;
            }
            internal void Stage(CharacterContext context)
            {
                Attempt attempt; bool created;
                lock (gate)
                {
                    if (accepted || quarantined) return;
                    if (active != null && !ReferenceEquals(active.Context, context))
                    {
                        var error = new InvalidOperationException("Outgoing mail already has an unfinished save attempt.");
                        context.RejectSave(error); throw error;
                    }
                    created = active == null; attempt = active ??= new(context);
                }
                // Failed enrollment must release the attempt, but successful
                // settlement must run after the mail aggregate's own observer.
                if (created) context.OnSaveCompleted(_ =>
                {
                    if (!attempt.SettlementRegistered) Finish(attempt, false);
                });
                try
                {
                    var receipt = Mail.SaveOutgoing(context);
                    var target = context.Character.AsNoTracking().SingleOrDefault(row => row.Id == receipt.Recipient);
                    if (target == null || target.DeleteTime != null)
                        throw new InvalidOperationException("Outgoing mail lost its recipient.");
                    lock (gate)
                    {
                        if (database != null && database != receipt.Database || recipient != null && recipient != target.Id
                            || recipientAccount != null && recipientAccount != target.AccountId)
                            throw new InvalidOperationException("Outgoing mail changed its retained destination or database.");
                        database = receipt.Database; recipient = target.Id; recipientAccount = target.AccountId;
                        attempt.Receipt = receipt; attempt.Account = target.AccountId;
                    }
                    RegisterOutgoing(receipt.Database, this);
                    if (created)
                    {
                        context.OnSaveValidation(_ =>
                        {
                            var current = context.Character.AsNoTracking().SingleOrDefault(row => row.Id == target.Id);
                            if (current == null || current.DeleteTime != null || current.AccountId != target.AccountId)
                                throw new InvalidOperationException("Outgoing mail lost its captured recipient account before commit.");
                        });
                        context.OnSaveSettled(committed => Finish(attempt, committed));
                        attempt.SettlementRegistered = true;
                    }
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
            private void Finish(Attempt attempt, bool committed)
            {
                var proof = committed ? attempt.Receipt?.Confirmation : null;
                bool schedule;
                lock (gate)
                {
                    if (!ReferenceEquals(active, attempt)) return;
                    active = null; recoveryPending = proof == null;
                    if (proof != null) { confirmed = proof; confirmedAccount = attempt.Account; }
                    schedule = proof != null && !queued && !published && !quarantined;
                    if (schedule) queued = true;
                }
                if (schedule) QueuePublication();
            }
            private void QueuePublication()
            {
                var events = manager.deliverySession?.Events;
                if (events == null) { lock (gate) queued = false; return; }
                events.EnqueueThreadSafe(new PredicateEvent(
                    () => !manager.DeliveryActor() || Ready(), Publish));
            }
            private bool Ready()
            {
                lock (gate) return active == null && !recoveryPending && !manager.IsMailSaveInProgress;
            }
            public bool IsReady
            {
                get { lock (gate) return quarantined || accepted || published && Ready(); }
            }
            public bool IsValid
            {
                get { lock (gate) return published && !quarantined && !accepted && manager.DeliveryActor(); }
            }
            public bool TryAccept()
            {
                lock (gate)
                {
                    if (!IsValid || !Ready()) return false;
                    // Acceptance and Stage share this gate. After transfer,
                    // a stale source save snapshot cannot enroll the mail.
                    Delivered();
                    return true;
                }
            }
            private void Publish()
            {
                MailItem.ConfirmedOutgoing proof; uint account;
                lock (gate)
                {
                    queued = false;
                    if (active != null || published || quarantined) return;
                    if (!manager.DeliveryActor()) { Quarantine(); return; }
                    proof = confirmed; account = confirmedAccount;
                    if (proof == null || proof.Header != null && Mail.RecipientId != proof.Header.RecipientId) return;
                    published = true;
                }
                if (proof.Header == null) { manager.RetireManualAction(Mail); Delivered(); return; }
                try { manager.PublishManualReturn(Mail); }
                catch (Exception error) { deliveryLog.Warn(error, "Committed manual return source reply failed; recipient delivery continues."); }
                try { manager.NotifyCommittedMail(Mail, account); }
                catch (Exception error) { deliveryLog.Warn(error, "Committed outgoing mail notification failed; inbox recovery retains its owner."); }
            }
            internal bool Resolve(CharacterMailModel row, out MailItem original)
            {
                lock (gate)
                {
                    original = null;
                    if (active != null || recoveryPending || manager.IsMailSaveInProgress) return false;
                    if (!manager.DeliveryActor() || manager.deliverySession?.Events == null) { Quarantine(); return true; }
                    if (!published && !quarantined) return false;
                    if (!quarantined && confirmed?.Header == MailHeaderState.From(row)
                        && confirmed.Attachments.SequenceEqual(row.Attachment.Select(MailAttachmentState.From).OrderBy(a => a.Index)))
                        original = Mail;
                    else Delivered();
                    return true;
                }
            }
            private void Quarantine()
            {
                quarantined = true; UnregisterOutgoing(database, this);
                manager.RetireManualAction(Mail);
                if (ReferenceEquals(Mail.OutgoingDelivery, this)) Mail.OutgoingDelivery = null;
                manager.RemoveOutgoing(this);
            }
            private void Delivered()
            {
                lock (gate)
                {
                    if (!published && !quarantined) return;
                    accepted = true;
                    UnregisterOutgoing(database, this);
                    if (ReferenceEquals(Mail.OutgoingDelivery, this)) Mail.OutgoingDelivery = null;
                    manager.RemoveOutgoing(this);
                }
            }
        }
    }
}
