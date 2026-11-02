using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;

namespace NexusForever.Game.Mail
{
    public partial class MailItem
    {
        private AttachmentTakeReceipt attachmentTake;
        internal bool HasPendingReceipt => cashReceipt != null || attachmentTake != null;

        internal AttachmentTakeReceipt CaptureAttachmentTake(MailAttachment selected, ulong recipient, Func<bool> currentMail)
            => CaptureAttachmentTake([selected], recipient, currentMail, false);

        private bool HasAttachmentTakeCustody(MailAttachment attachment, ulong recipient)
        {
            var item = attachment.Item;
            // Legacy marketplace/system producers persisted recipient ownership
            // without an inventory page. Exact link and Item witnesses still
            // authorize the transfer; foreign owners/pages remain excluded.
            bool legacyProducer = SenderId == 0 && (SenderType is SenderType.ItemAuction or SenderType.CommodityAuction or SenderType.GM);
            return item is Item concrete && concrete.HasMailTakeSourceCustody(recipient, legacyProducer);
        }

        internal AttachmentTakeReceipt CaptureAttachmentTake(MailAttachment[] selected, ulong recipient, Func<bool> currentMail, bool collectCash)
        {
            lock (persistence.Gate)
            {
                persistence.AssertRewardOwner();
                if (HasPendingReceipt || RecipientId != recipient || PendingCreate || PendingDelete || membership != null
                    || membershipUncertain != null || deletedAttachments.Count != 0 || selected.Length == 0
                    || selected.Select(link => link.Index).Distinct().Count() != selected.Length
                    || selected.Any(link => !mailAttachments.Contains(link))
                    || !SameMembership(membershipBaseline, CurrentAttachmentSet())
                    || mailAttachments.Any(link => link.Item is not Item item || item.HasPendingSave)
                    || selected.Any(link => !HasAttachmentTakeCustody(link, recipient))
                    || IsCashOnDelivery && !HasPaidOrCollectedCurrency
                    || collectCash && (IsCashOnDelivery || HasPaidOrCollectedCurrency || CurrencyAmount == 0))
                    throw new InvalidOperationException("Mail attachment take requires its loaded receipt and unchanged custody.");
                var snapshot = persistence.CaptureNormal(null);
                var acknowledgement = persistence.CaptureReward(value => value, value => value);
                snapshot.Value = snapshot.Value with { Flags = (byte)(snapshot.Value.Flags | (byte)MailFlag.NotReturnable),
                    HasPaidOrCollectedCurrency = collectCash ? (byte)1 : snapshot.Value.HasPaidOrCollectedCurrency };
                snapshot.NeedsSave = true;
                var links = new List<MailAttachment.TakeReceipt>();
                try
                {
                    foreach (var attachment in selected) links.Add(attachment.CaptureTake());
                    return attachmentTake = new(this, selected.ToArray(), links.ToArray(), snapshot, acknowledgement,
                        membershipBaseline.ToArray(), mailAttachments.Select(attachment => (Item)attachment.Item).ToArray(), recipient, currentMail, collectCash);
                }
                catch { foreach (var link in links) link.Abort(); throw; }
            }
        }

        internal sealed class AttachmentTakeReceipt : IBankSaveCompanion
        {
            private readonly MailItem owner;
            private readonly MailAttachment[] selected;
            private readonly MailAttachment.TakeReceipt[] links;
            private readonly HashSet<uint> selectedIndices;
            private readonly ulong[] selectedItems;
            private readonly bool collectCash;
            private readonly QuestRowPersistence<CharacterMailModel, MailHeaderState>.NormalSnapshot snapshot;
            private readonly QuestRewardRow<MailHeaderState> acknowledgement;
            private readonly MailAttachmentState[] before, after;
            private readonly Item[] items;
            private readonly ulong recipient;
            private readonly Func<bool> currentMail;
            private MailHeaderState storedHeader;
            private bool ended, adopted;
            public bool CanExecuteWithoutItems => false;

            internal AttachmentTakeReceipt(MailItem owner, MailAttachment[] selected, MailAttachment.TakeReceipt[] links,
                QuestRowPersistence<CharacterMailModel, MailHeaderState>.NormalSnapshot snapshot,
                QuestRewardRow<MailHeaderState> acknowledgement, MailAttachmentState[] members, Item[] items,
                ulong recipient, Func<bool> currentMail, bool collectCash)
            {
                this.owner = owner; this.selected = selected; this.links = links; this.snapshot = snapshot;
                this.acknowledgement = acknowledgement; before = members; this.items = items;
                this.recipient = recipient; this.currentMail = currentMail; this.collectCash = collectCash;
                selectedIndices = selected.Select(link => link.Index).ToHashSet();
                selectedItems = selected.Select(link => link.Item.Guid).ToArray();
                after = before.Where(member => !selectedIndices.Contains(member.Index)).ToArray();
            }
            private void ValidateOwner()
            {
                if (ended || !ReferenceEquals(owner.attachmentTake, this))
                    throw new InvalidOperationException("Mail lost its retained attachment take.");
            }
            public void Stage(CharacterContext context)
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner();
                    string database = Item.GetRewardDatabaseKey(context);
                    if (owner.persistenceDatabase != null && owner.persistenceDatabase != database)
                        throw new InvalidOperationException("Mail attachment take changed its retained database.");
                    owner.persistenceDatabase = database;
                    if (context.RealmBankItem.AsNoTracking().Any(row => selectedItems.Contains(row.Id)))
                        throw new InvalidOperationException("Mail attachment also belongs to realm bank storage.");
                    owner.persistence.Save(context, force: true, frozen: snapshot);
                    foreach (var link in links) link.Stage(context);
                    // The row owners above reconcile an earlier observed commit
                    // before this aggregate accepts its already-removed member.
                    // An unexplained first-attempt absence still fails there.
                    var initialMembers = owner.ReadAttachmentSet(context);
                    if (!SameMembership(initialMembers, before) && !SameMembership(initialMembers, after))
                        throw new InvalidOperationException("Mail attachment take changed outside its retained membership candidates.");
                    var initialCustody = initialMembers.Where(member => selectedIndices.Contains(member.Index)).ToArray();
                    storedHeader = MailHeaderState.From(context.ChangeTracker.Entries<CharacterMailModel>()
                        .Single(entry => entry.Entity.Id == owner.Id).Entity);
                    var target = storedHeader;
                    var witnesses = items.Select(item => item.CaptureMailSaveWitness(context)).ToArray();
                    void Witness(bool sqlTarget, CharacterSaveValidationPhase phase)
                    {
                        if (!SameMembership(owner.ReadAttachmentSet(context), sqlTarget ? after : initialMembers))
                            throw new InvalidOperationException("Mail attachment take lost its complete SQL membership.");
                        var custody = context.CharacterMailAttachment.AsNoTracking().Where(row => selectedItems.Contains(row.ItemGuid))
                            .ToArray().Select(MailAttachmentState.From).OrderBy(row => row.Index).ToArray();
                        var expected = sqlTarget ? [] : initialCustody;
                        if (!SameMembership(custody, expected))
                            throw new InvalidOperationException("Mail attachment has another SQL custody link.");
                        if (sqlTarget && owner.ReadHeader(context) != target)
                            throw new InvalidOperationException("Mail attachment take did not produce its complete SQL header.");
                        foreach (var item in witnesses) item.Validate(phase);
                    }
                    Witness(false, CharacterSaveValidationPhase.BeforeFlush);
                    context.OnSaveValidation(phase => Witness(phase != CharacterSaveValidationPhase.BeforeFlush, phase));
                    EventHandler<SavedChangesEventArgs> flushed = (_, _) =>
                    {
                        try
                        {
                            Witness(true, CharacterSaveValidationPhase.BeforeImplicitCommit);
                            foreach (var item in witnesses) item.ObserveFlush();
                        }
                        catch (Exception error) { context.RejectSave(error); throw; }
                    };
                    context.SavedChanges += flushed;
                    context.OnSaveSettled(_ => context.SavedChanges -= flushed);
                }
            }
            public void Adopt()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner(); var proof = acknowledgement.Candidates();
                    if (adopted || !currentMail() || storedHeader == null || proof.Count != 1 || proof[0] != storedHeader
                        || owner.RecipientId != recipient || owner.PendingCreate || owner.PendingDelete
                        || !SameMembership(owner.CurrentAttachmentSet(), before)
                        || selected.Any(link => link.Item.CharacterId != recipient || link.Item.Location != InventoryLocation.Inventory)
                        || collectCash && owner.HasPaidOrCollectedCurrency)
                        throw new InvalidOperationException("Mail attachment take has no confirmed current adoption.");
                    foreach (var link in links) link.ValidateConfirmed();
                    foreach (var link in links) link.Adopt();
                    acknowledgement.Adopt(storedHeader);
                    owner.flags |= MailFlag.NotReturnable;
                    if (collectCash) owner.hasPaidOrCollectedCurrency = true;
                    foreach (var link in selected) owner.mailAttachments.Remove(link);
                    owner.membershipBaseline = after.ToArray();
                    adopted = true;
                }
            }
            public void Complete()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner();
                    if (!adopted) throw new InvalidOperationException("Mail attachment take cannot finish before adoption.");
                    foreach (var link in links) link.Complete();
                    ended = true; owner.attachmentTake = null;
                }
            }
            public bool ReleaseForOrdinarySave() { Complete(); return true; }
            public void Abort()
            {
                lock (owner.persistence.Gate) { ValidateOwner(); foreach (var link in links) link.Abort(); ended = true; owner.attachmentTake = null; }
            }
            public void Quarantine()
            {
                lock (owner.persistence.Gate) { ended = true; foreach (var link in links) link.Quarantine(); owner.persistence.RetireRewardOwner(); }
            }
        }
    }
}
