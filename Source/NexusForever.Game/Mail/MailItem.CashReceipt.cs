using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Mail;

namespace NexusForever.Game.Mail
{
    public partial class MailItem
    {
        private CashReceipt cashReceipt;
        internal bool HasPendingCashReceipt => cashReceipt != null;

        internal CashReceipt CaptureCashReceipt(ulong recipient)
        {
            lock (persistence.Gate)
            {
                persistence.AssertRewardOwner();
                if (HasPendingReceipt || RecipientId != recipient || PendingCreate || PendingDelete
                    || HasPaidOrCollectedCurrency || membership != null || membershipUncertain != null
                    || deletedAttachments.Count != 0 || !SameMembership(membershipBaseline, CurrentAttachmentSet()))
                    throw new InvalidOperationException("Mail cash requires its unchanged loaded receipt and membership.");
                var snapshot = persistence.CaptureNormal(null);
                var acknowledgement = persistence.CaptureReward(value => value, value => value);
                snapshot.Value = snapshot.Value with { HasPaidOrCollectedCurrency = 1,
                    Flags = (byte)(snapshot.Value.Flags | (byte)MailFlag.NotReturnable) };
                snapshot.NeedsSave = true;
                cashReceipt = new(this, snapshot, acknowledgement, membershipBaseline.ToArray(), recipient);
                return cashReceipt;
            }
        }

        private void AssertNoCashReceipt()
        {
            if (HasPendingReceipt) throw new InvalidOperationException("Mail belongs to an unfinished cash or attachment receipt.");
        }

        internal sealed class CashReceipt : IBankSaveCompanion
        {
            private readonly MailItem owner;
            private readonly QuestRowPersistence<CharacterMailModel, MailHeaderState>.NormalSnapshot snapshot;
            private readonly QuestRewardRow<MailHeaderState> acknowledgement;
            private readonly MailAttachmentState[] members;
            private readonly ulong recipient;
            private MailHeaderState storedHeader;
            private bool ended, adopted;
            public bool CanExecuteWithoutItems => true;

            internal CashReceipt(MailItem owner,
                QuestRowPersistence<CharacterMailModel, MailHeaderState>.NormalSnapshot snapshot,
                QuestRewardRow<MailHeaderState> acknowledgement, MailAttachmentState[] members, ulong recipient)
            { this.owner = owner; this.snapshot = snapshot; this.acknowledgement = acknowledgement; this.members = members; this.recipient = recipient; }

            private void ValidateOwner()
            {
                if (ended || !ReferenceEquals(owner.cashReceipt, this))
                    throw new InvalidOperationException("Mail cash lost its retained receipt owner.");
            }
            public void Stage(CharacterContext context)
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner();
                    string database = Item.GetRewardDatabaseKey(context);
                    if (owner.persistenceDatabase != null && owner.persistenceDatabase != database)
                        throw new InvalidOperationException("Mail cash changed its execution database.");
                    owner.persistenceDatabase = database;
                    owner.persistence.Save(context, force: true, frozen: snapshot);
                    storedHeader = MailHeaderState.From(context.ChangeTracker.Entries<CharacterMailModel>()
                        .Single(entry => entry.Entity.Id == owner.Id).Entity);
                    MailHeaderState target = storedHeader;
                    void Witness(bool sqlHeader)
                    {
                        if (!SameMembership(owner.ReadAttachmentSet(context), members))
                            throw new InvalidOperationException("Mail cash lost its complete SQL attachment membership.");
                        if (context.ChangeTracker.Entries<CharacterMailAttachmentModel>().Any(entry => entry.Entity.Id == owner.Id
                            && entry.State is not (EntityState.Unchanged or EntityState.Detached)))
                            throw new InvalidOperationException("Mail cash cannot enroll attachment mutations.");
                        if (sqlHeader && owner.ReadHeader(context) != target)
                            throw new InvalidOperationException("Mail cash did not produce its complete SQL receipt.");
                    }
                    Witness(false);
                    context.OnSaveValidation(phase => Witness(phase != CharacterSaveValidationPhase.BeforeFlush));
                    EventHandler<SavedChangesEventArgs> flushed = (_, _) =>
                    {
                        try { Witness(true); }
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
                    ValidateOwner();
                    var proof = acknowledgement.Candidates();
                    if (adopted || storedHeader == null || !owner.persistence.WasAttempted || proof.Count != 1 || proof[0] != storedHeader
                        || owner.RecipientId != recipient || owner.PendingCreate || owner.PendingDelete || owner.HasPaidOrCollectedCurrency
                        || !SameMembership(owner.CurrentAttachmentSet(), members))
                        throw new InvalidOperationException("Mail cash has no confirmed unchanged receipt.");
                    acknowledgement.Adopt(storedHeader);
                    // Later read flags keep their own revisions. The paid bit and
                    // NotReturnable flag already belong to the committed snapshot.
                    owner.hasPaidOrCollectedCurrency = true;
                    owner.flags |= MailFlag.NotReturnable;
                    adopted = true;
                }
            }
            public void Complete()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner();
                    if (!adopted) throw new InvalidOperationException("Mail cash cannot finish before adoption.");
                    ended = true; owner.cashReceipt = null;
                }
            }
            public bool ReleaseForOrdinarySave() { Complete(); return true; }
            public void Abort()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner(); ended = true; owner.cashReceipt = null;
                }
            }
            public void Quarantine()
            {
                lock (owner.persistence.Gate) { ended = true; owner.persistence.RetireRewardOwner(); }
            }
        }
    }
}
