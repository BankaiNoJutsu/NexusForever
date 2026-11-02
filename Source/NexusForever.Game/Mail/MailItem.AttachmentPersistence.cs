using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Mail
{
    public partial class MailItem
    {
        private MailAttachmentState[] membershipBaseline = [];
        private List<MailAttachmentState[]> membershipUncertain;
        private MembershipUnit membership;

        internal sealed class MembershipUnit(CharacterContext context)
        {
            internal readonly CharacterContext Context = context;
            internal readonly List<MailAttachmentState[]> Possible = [];
            internal MailAttachmentState[] Target, Sql;
            internal MailHeaderState HeaderTarget, HeaderSql;
            internal GameItem.MailSaveWitness[] TargetItems = [], SqlItems = [];
            internal bool Flushed;
            internal bool Settled, Committed;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }

        private MailAttachmentState[] CurrentAttachmentSet() => mailAttachments
            .Select(a => new MailAttachmentState(a.Id, a.Index, a.Item.Guid)).OrderBy(a => a.Index).ToArray();
        private MailAttachmentState[] ReadAttachmentSet(CharacterContext context) => context.CharacterMailAttachment.AsNoTracking()
            .Where(a => a.Id == Id).ToArray().Select(MailAttachmentState.From).OrderBy(a => a.Index).ToArray();
        private MailHeaderState ReadHeader(CharacterContext context) => MailHeaderState.From(context.CharacterMail.AsNoTracking().SingleOrDefault(row => row.Id == Id));
        private static bool SameMembership(MailAttachmentState[] left, MailAttachmentState[] right)
            => left != null && right != null && left.SequenceEqual(right);
        private static void RetainPossible(List<MailAttachmentState[]> candidates, MailAttachmentState[] value)
        {
            if (value != null && !candidates.Any(candidate => SameMembership(candidate, value))) candidates.Add(value);
        }

        private void BeginMembership(CharacterContext context)
        {
            if (membership != null)
            {
                if (!ReferenceEquals(membership.Context, context)) throw new InvalidOperationException("Mail attachment set has an unfinished save unit.");
                return;
            }
            // A single changed link still requires a transaction so SQL triggers
            // and unexpected membership can reject before implicit commit.
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            var unit = new MembershipUnit(context); membership = unit;
            unit.OnFlushed = (_, result) => ObserveMembershipFlush(unit, result.EntitiesSavedCount);
            context.SavedChanges += unit.OnFlushed;
            context.OnSaveValidation(phase => ValidateMembership(unit, phase));
            context.OnSaveSettled(committed => CompleteMembership(unit, committed));
            RetainPossible(unit.Possible, membershipBaseline);
            var actual = ReadAttachmentSet(context);
            if (membershipUncertain != null)
            {
                foreach (var candidate in membershipUncertain) RetainPossible(unit.Possible, candidate);
                if (!membershipUncertain.Any(candidate => SameMembership(candidate, actual)))
                    throw new InvalidOperationException("Mail attachment set changed outside its uncertain save candidates.");
                membershipBaseline = actual; membershipUncertain = null;
            }
            else if (!SameMembership(membershipBaseline, actual))
                throw new InvalidOperationException("Mail attachment set changed outside its loaded baseline.");
            unit.Sql = actual;
        }

        private void StageMembership(CharacterContext context)
        {
            if (!ReferenceEquals(membership?.Context, context)) throw new InvalidOperationException("Mail attachment set lost its save owner.");
            var target = deleteRequested ? [] : CurrentAttachmentSet();
            if (target.Any(a => a.Id != Id) || target.Select(a => a.Index).Distinct().Count() != target.Length
                || target.Select(a => a.ItemGuid).Distinct().Count() != target.Length)
                throw new InvalidOperationException("Mail attachment set has conflicting identities.");
            membership.Target = target; RetainPossible(membership.Possible, target);
            var header = context.ChangeTracker.Entries<CharacterMailModel>().SingleOrDefault(e => e.Entity.Id == Id);
            membership.HeaderTarget = deleteRequested ? null : MailHeaderState.From(header?.Entity);
            if (!deleteRequested && membership.HeaderTarget == null) throw new InvalidOperationException("Mail lost its complete header snapshot.");
            membership.TargetItems = mailAttachments.Concat(deletedAttachments).Select(a => a.Item).OfType<GameItem>()
                .DistinctBy(item => item.Guid).Select(item => item.CaptureMailSaveWitness(context)).ToArray();
        }

        private void ValidateMembership(MembershipUnit unit, CharacterSaveValidationPhase phase)
        {
            lock (persistence.Gate)
            {
                if (!ReferenceEquals(membership, unit)) throw new InvalidOperationException("Mail attachment set lost its save ownership.");
                if (unit.Context.HasSavepointRollback) return;
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    if (unit.Flushed)
                    {
                        if (!SameMembership(ReadAttachmentSet(unit.Context), unit.Sql) || ReadHeader(unit.Context) != unit.HeaderSql)
                            throw new InvalidOperationException("Mail SQL aggregate changed after its acknowledged flush.");
                        foreach (var item in unit.SqlItems) item.Validate(phase);
                    }
                    return;
                }
                if (unit.Target == null) throw new InvalidOperationException("Mail attachment set was not completely staged.");
                if (phase == CharacterSaveValidationPhase.BeforeImplicitCommit)
                {
                    if (!SameMembership(ReadAttachmentSet(unit.Context), unit.Target) || ReadHeader(unit.Context) != unit.HeaderTarget)
                        throw new InvalidOperationException("Mail SQL aggregate differs from its staged snapshot.");
                    foreach (var item in unit.TargetItems) item.Validate(phase);
                    return;
                }
                unit.Context.ChangeTracker.DetectChanges();
                var tracked = unit.Context.ChangeTracker.Entries<CharacterMailAttachmentModel>()
                    .Where(e => e.Entity.Id == Id && e.State is not (EntityState.Deleted or EntityState.Detached))
                    .Select(e => MailAttachmentState.From(e.Entity)).OrderBy(a => a.Index).ToArray();
                if (!SameMembership(tracked, unit.Target))
                    throw new InvalidOperationException("Mail lost its complete tracked attachment set.");
                foreach (var item in unit.TargetItems) item.Validate(phase);
            }
        }

        private void ObserveMembershipFlush(MembershipUnit unit, int saved)
        {
            lock (persistence.Gate)
            {
                if (!ReferenceEquals(membership, unit) || unit.Target == null) return;
                var actual = ReadAttachmentSet(unit.Context);
                if (!SameMembership(actual, unit.Target) || ReadHeader(unit.Context) != unit.HeaderTarget)
                {
                    var error = new InvalidOperationException("Mail flush did not produce its complete SQL aggregate.");
                    unit.Context.RejectSave(error); throw error;
                }
                try
                {
                    foreach (var item in unit.TargetItems) item.Validate(CharacterSaveValidationPhase.BeforeImplicitCommit);
                }
                catch (Exception error) { unit.Context.RejectSave(error); throw; }
                foreach (var item in unit.TargetItems) item.ObserveFlush();
                unit.HeaderSql = unit.HeaderTarget; unit.SqlItems = unit.TargetItems;
                unit.Sql = actual; unit.Flushed = true; RetainPossible(unit.Possible, actual);
            }
        }

        private void CompleteMembership(MembershipUnit unit, bool committed)
        {
            lock (persistence.Gate)
            {
                if (!ReferenceEquals(membership, unit)) return;
                try
                {
                    unit.Settled = true; unit.Committed = committed && unit.Flushed;
                    if (committed && unit.Flushed) { membershipBaseline = unit.Sql; membershipUncertain = null; }
                    else
                    {
                        membershipUncertain ??= [];
                        foreach (var candidate in unit.Possible) RetainPossible(membershipUncertain, candidate);
                        RetainPossible(membershipUncertain, membershipBaseline);
                    }
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; membership = null;
                    if (committed && unit.Flushed)
                        foreach (var deleted in deletedAttachments.ToArray())
                            if (deleted is not MailAttachment concrete || concrete.IsDeletionSettled)
                                deletedAttachments.Remove(deleted);
                }
            }
        }
    }
}
