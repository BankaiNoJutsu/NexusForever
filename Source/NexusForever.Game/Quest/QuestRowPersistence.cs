using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Quest
{
    // Tracks normal Quest saves; reward integration must retain this ownership.
    // No monitor survives a SQL await. State records contain scalar copies only.
    internal sealed class QuestRowPersistence<TModel, TState> where TModel : class where TState : class
    {
        internal readonly object Gate = new();
        private readonly string key;
        private readonly string group;
        private readonly Func<CharacterContext, TModel> read;
        private readonly Func<TModel, TState> snapshot;
        private readonly Func<TState, TModel> model;
        private readonly Func<TState> current;
        private readonly Func<CharacterContext, (Func<TState, TState> Stage, Func<TState, TState> Ack)> normalization;
        private readonly Dictionary<int, long> revisions = [];
        private long revision;
        private int mask;
        private TState baseline;
        private List<TState> uncertain;
        private Unit active;
        private bool attempted;
        private bool rewardRetired;
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string Database, string Row), Unit> leases = [];

        private sealed class Unit
        {
            internal object Frozen;
            internal CharacterContext Context;
            internal Func<TState, TState> NormalizeStage, NormalizeAck;
            internal IDisposable Group;
            internal (string Database, string Row) Key;
            internal TModel Model;
            internal TState Target, SqlState, Flushed, Original;
            internal Dictionary<int, long> StagedRevisions, FlushedRevisions;
            internal readonly List<TState> Possible = [];
            internal bool FlushObserved;
            internal bool ParentCascade;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }

        internal QuestRowPersistence(string key, string group, TState baseline, Func<CharacterContext, TModel> read,
            Func<TModel, TState> snapshot, Func<TState, TModel> model, Func<TState> current,
            Func<CharacterContext, (Func<TState, TState> Stage, Func<TState, TState> Ack)> normalization = null)
        {
            this.key = key; this.group = group; this.baseline = baseline; this.read = read; this.snapshot = snapshot;
            this.model = model; this.current = current; this.normalization = normalization ?? (_ => (value => value, value => value));
        }

        internal bool NeedsSave { get { lock (Gate) return mask != 0 || active != null || uncertain != null; } }
        internal bool WasAttempted { get { lock (Gate) return attempted; } }
        internal bool IsAbsent { get { lock (Gate) return attempted && active == null && uncertain == null && baseline == null; } }
        internal bool OwnsOtherContext(CharacterContext context) { lock (Gate) return active != null && !ReferenceEquals(active.Context, context); }
        internal bool HasFlushedDeletion(CharacterContext context) { lock (Gate) return active != null && ReferenceEquals(active.Context, context) && active.FlushObserved && active.SqlState == null; }
        internal bool Has(int field) { lock (Gate) return (mask & field) != 0; }
        internal void AssertRewardOwner() { lock (Gate) if (rewardRetired) throw new InvalidOperationException("Retired Quest owner requires reload."); }
        internal void RetireRewardOwner() { lock (Gate) rewardRetired = true; }
        internal sealed class NormalSnapshot
        {
            internal QuestRowPersistence<TModel, TState> Owner;
            internal object Token;
            internal TState Value;
            internal Dictionary<int, long> Revisions;
            internal bool NeedsSave;
        }
        internal NormalSnapshot CaptureNormal(object token)
        {
            lock (Gate)
            {
                AssertRewardOwner();
                if (active != null) throw new InvalidOperationException("Quest row already has a normal save unit.");
                return new() { Owner = this, Token = token, Value = current(), Revisions = new(revisions), NeedsSave = mask != 0 || uncertain != null };
            }
        }
        internal QuestRewardRow<TCopy> CaptureReward<TCopy>(Func<TState, TCopy> copy, Func<TCopy, TState> restore, object frozen = null) where TCopy : class
        {
            lock (Gate)
            {
                if (active != null && (frozen == null || !ReferenceEquals(active.Frozen, frozen))) throw new InvalidOperationException("Quest row still belongs to a normal save.");
                TState captured = current(); var capturedRevisions = new Dictionary<int, long>(revisions);
                return new(copy(captured), revision, () =>
                {
                    lock (Gate)
                    {
                        if (active != null) throw new InvalidOperationException("Quest baseline is still being saved.");
                        return Array.AsReadOnly((uncertain ?? []).Append(baseline).Distinct().Select(copy).ToArray());
                    }
                }, stored =>
                {
                    lock (Gate)
                    {
                        if (active != null) throw new InvalidOperationException("Quest reward lost normal-save exclusion.");
                        baseline = restore(stored); uncertain = null; attempted = true;
                        if (Equal(current(), captured))
                            foreach (var field in capturedRevisions)
                                if (revisions.GetValueOrDefault(field.Key) == field.Value) { mask &= ~field.Key; revisions.Remove(field.Key); }
                        // An acknowledged cascade must remain recreatable if the
                        // actor canceled deletion/reaccepted after capture.
                        if (baseline == null) Dirty(1);
                    }
                }, stored =>
                {
                    lock (Gate)
                    {
                        if (active != null) throw new InvalidOperationException("Quest rebase lost normal-save exclusion.");
                        baseline = restore(stored); uncertain = null; attempted = true;
                        // A rebase needs a save if this owner differs, but must
                        // not replace an already captured dirty revision. A
                        // later queued capture can acknowledge that revision.
                        if (!Equal(current(), baseline) && mask == 0) Dirty(1);
                    }
                }, value => { lock (Gate) rewardRetired = value; });
            }
        }
        internal void Cancel(int field) { lock (Gate) { mask &= ~field; revisions.Remove(field); ++revision; } }
        internal void Dirty(int fields)
        {
            lock (Gate)
                for (int bit = 1; bit != 0; bit <<= 1)
                    if ((fields & bit) != 0) { mask |= bit; revisions[bit] = ++revision; }
        }

        internal void ParentDeleted()
        {
            lock (Gate)
            {
                if (active != null) return; // its own last flush/terminal callback settles it
                baseline = null; uncertain = null; attempted = true;
                // A canceled parent deletion must be able to recreate its complete
                // objectives even if they were clean before the parent cascade.
                Dirty(1);
            }
        }

        internal void RestoreParentInContext(CharacterContext context)
        {
            lock (Gate)
            {
                if (active != null && !ReferenceEquals(active.Context, context)) throw new InvalidOperationException("Objective belongs to another save unit.");
                if (active == null) uncertain = (uncertain ?? []).Append(baseline).Append(null).Distinct().ToList();
                Dirty(1);
            }
        }

        internal void Save(CharacterContext context, bool delete = false, bool force = false, NormalSnapshot frozen = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            lock (Gate)
            {
                try
                {
                    AssertRewardOwner();
                    if (frozen != null && !ReferenceEquals(frozen.Owner, this)) throw new InvalidOperationException("Foreign frozen Quest row.");
                    if (active != null && !ReferenceEquals(active.Context, context)) throw new InvalidOperationException("Quest row has an unfinished save unit.");
                    if (active != null && !ReferenceEquals(active.Frozen, frozen?.Token)) throw new InvalidOperationException("Quest row changed its frozen save owner.");
                    if (context.HasSavepointRollback || context.IsSaveRejected) throw new InvalidOperationException("Quest row requires a fresh save context.");
                    if (!force && active == null && !delete && (frozen == null ? mask == 0 && uncertain == null : !frozen.NeedsSave)) return;
                    Unit unit = active;
                    if (unit == null)
                    {
                        unit = new() { Context = context, Key = (Item.GetRewardDatabaseKey(context), key), Frozen = frozen?.Token };
                        unit.Group = QuestSaveScope.Acquire(context, unit.Key.Database, group, frozen?.Token);
                        try
                        {
                            lock (leaseGate)
                            {
                                if (leases.ContainsKey(unit.Key)) throw new InvalidOperationException("Quest row belongs to another loaded owner.");
                                leases.Add(unit.Key, unit);
                            }
                        }
                        catch { unit.Group.Dispose(); throw; }
                        active = unit; attempted = true;
                        unit.OnFlushed = (_, result) => CaptureFlush(unit, result.EntitiesSavedCount);
                        context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => Complete(unit, committed));
                        context.OnSaveValidation(phase => Validate(unit, phase));
                        (unit.NormalizeStage, unit.NormalizeAck) = normalization(context);
                        TState actual = snapshot(read(context));
                        if (uncertain != null)
                        {
                            unit.Possible.AddRange(uncertain);
                            if (!uncertain.Contains(actual)) throw new InvalidOperationException("Quest row changed outside its uncertain save candidates.");
                            baseline = actual; uncertain = null;
                        }
                        else if (!Equal(baseline, actual)) throw new InvalidOperationException("Quest row changed outside its loaded baseline.");
                        unit.SqlState = baseline; unit.Possible.Add(baseline);
                    }
                    TState desired = delete ? null : unit.NormalizeStage(frozen == null ? current() : frozen.Value);
                    TModel identity = model(desired ?? unit.SqlState ?? (frozen == null ? current() : frozen.Value));
                    var primary = context.Entry(identity).Metadata.FindPrimaryKey()!;
                    var identityEntry = context.Entry(identity);
                    EntityEntry<TModel> tracked = context.ChangeTracker.Entries<TModel>().SingleOrDefault(entry =>
                        primary.Properties.All(property => Equals(entry.Property(property.Name).CurrentValue, identityEntry.Property(property.Name).CurrentValue)));
                    if (unit.Model == null && tracked != null && (unit.SqlState == null || tracked.State == EntityState.Added
                        || !Equal(snapshot((TModel)tracked.OriginalValues.ToObject()), unit.SqlState)))
                        throw new InvalidOperationException("Quest row requires its complete loaded model in the shared context.");
                    if (unit.Model != null && tracked != null && !ReferenceEquals(unit.Model, tracked.Entity))
                        throw new InvalidOperationException("Quest row lost its tracked ownership.");
                    if (desired == null)
                    {
                        if (tracked != null)
                        {
                            tracked.State = tracked.State == EntityState.Added ? EntityState.Detached : EntityState.Deleted;
                            unit.Model = tracked.Entity;
                        }
                        else if (unit.SqlState != null) { unit.Model = model(unit.SqlState); context.Entry(unit.Model).State = EntityState.Deleted; }
                    }
                    else if (unit.SqlState == null)
                    {
                        if (tracked?.State == EntityState.Deleted)
                        {
                            tracked.State = EntityState.Unchanged; tracked.CurrentValues.SetValues(model(desired)); unit.Model = tracked.Entity;
                        }
                        else if (tracked?.State == EntityState.Added && ReferenceEquals(tracked.Entity, unit.Model)) tracked.CurrentValues.SetValues(model(desired));
                        else { unit.Model = model(desired); context.Add(unit.Model); }
                    }
                    else
                    {
                        if (tracked == null)
                        {
                            unit.Model = model(unit.SqlState); tracked = context.Entry(unit.Model);
                            // Objective ordinal0 is a real composite key. Attach's
                            // generated-key inference must not turn it into a create.
                            tracked.State = EntityState.Unchanged;
                        }
                        else { unit.Model = tracked.Entity; if (tracked.State == EntityState.Deleted) tracked.State = EntityState.Unchanged; }
                        tracked.CurrentValues.SetValues(model(desired));
                    }
                    unit.Target = desired; unit.ParentCascade = false; unit.StagedRevisions = new(frozen?.Revisions ?? revisions); unit.Possible.Add(desired);
                    unit.Original = unit.Model == null ? null : snapshot((TModel)context.Entry(unit.Model).OriginalValues.ToObject());
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }

        private void CaptureFlush(Unit unit, int saved)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.StagedRevisions == null) return;
                if (saved == 0 && (!Equal(unit.Target, unit.SqlState) || unit.Model != null
                    && unit.Context.Entry(unit.Model).State is EntityState.Added or EntityState.Modified or EntityState.Deleted)) return;
                // A parent cascade can detach a previously staged objective.
                unit.Flushed = unit.Model == null || unit.Context.Entry(unit.Model).State is EntityState.Detached or EntityState.Deleted
                    ? null : snapshot(unit.Model);
                unit.FlushObserved = true; unit.FlushedRevisions = new(unit.StagedRevisions);
                unit.ParentCascade = unit.Flushed == null && unit.Target != null;
                unit.SqlState = unit.Flushed; unit.Possible.Add(unit.Flushed);
                unit.Original = unit.Model == null ? null : snapshot((TModel)unit.Context.Entry(unit.Model).OriginalValues.ToObject());
            }
        }

        private void Validate(Unit unit, CharacterSaveValidationPhase phase)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Quest row lost its save ownership.");
                if (unit.Context.HasSavepointRollback) return; // terminal settlement retains uncertainty
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    if (unit.FlushObserved && !Equal(snapshot(read(unit.Context)), unit.SqlState))
                        throw new InvalidOperationException("Quest SQL row changed after its acknowledged flush.");
                    return;
                }
                unit.Context.ChangeTracker.DetectChanges();
                if (unit.Model == null) { if (unit.Target != null) throw new InvalidOperationException("Quest row lost its staged model."); return; }
                var entry = unit.Context.Entry(unit.Model);
                bool cascade = unit.Model is CharacterQuestObjectiveModel objective
                    && unit.Context.ChangeTracker.Entries<CharacterQuestModel>().Any(parent => parent.State == EntityState.Deleted
                        && parent.Entity.Id == objective.Id && parent.Entity.QuestId == objective.QuestId);
                if (cascade && entry.State == EntityState.Deleted || unit.ParentCascade && entry.State == EntityState.Detached) return;
                if (unit.Target == null)
                {
                    if (entry.State is not (EntityState.Deleted or EntityState.Detached)) throw new InvalidOperationException("Quest deletion was detached from its save intent.");
                    if (unit.SqlState != null && !Equal(snapshot(unit.Model), unit.SqlState)) throw new InvalidOperationException("Quest deletion changed its loaded identity.");
                }
                else
                {
                    if (!Equal(snapshot(unit.Model), unit.Target) || entry.State is EntityState.Deleted or EntityState.Detached)
                        throw new InvalidOperationException("Quest staged snapshot changed outside its owner.");
                    if (unit.SqlState == null && entry.State != EntityState.Added) throw new InvalidOperationException("Quest create lost its tracked ownership.");
                    if (unit.SqlState != null && !Equal(snapshot((TModel)entry.OriginalValues.ToObject()), unit.Original))
                        throw new InvalidOperationException("Quest original values changed outside its owner.");
                    if (unit.SqlState != null && !Equal(unit.Target, unit.SqlState) && entry.State != EntityState.Modified)
                        throw new InvalidOperationException("Quest update lost its modified state.");
                    if (unit.SqlState != null)
                    {
                        var before = unit.Context.Entry(model(unit.SqlState));
                        foreach (var property in entry.Properties)
                            if (!Equals(property.CurrentValue, before.Property(property.Metadata.Name).CurrentValue) && !property.IsModified)
                                throw new InvalidOperationException("Quest update lost a modified field.");
                    }
                }
            }
        }

        private void Complete(Unit unit, bool committed)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.FlushObserved)
                    {
                        baseline = unit.Flushed; uncertain = null;
                        TState now = unit.NormalizeAck(current());
                        if (Equal(now, unit.Flushed) || unit.Flushed == null && unit.Target == null)
                            foreach (var field in unit.FlushedRevisions)
                                if (revisions.GetValueOrDefault(field.Key) == field.Value) { mask &= ~field.Key; revisions.Remove(field.Key); }
                    }
                    else
                    {
                        uncertain = (uncertain ?? []).Concat(unit.Possible).Append(baseline).Distinct().ToList();
                    }
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                    lock (leaseGate) if (leases.TryGetValue(unit.Key, out var held) && ReferenceEquals(held, unit)) leases.Remove(unit.Key);
                    unit.Group.Dispose();
                }
            }
        }
        private static bool Equal(TState left, TState right) => EqualityComparer<TState>.Default.Equals(left, right);
    }

    internal static class QuestSaveScope
    {
        private static readonly object gate = new();
        private sealed class Scope(CharacterContext context, object frozen) { internal readonly CharacterContext Context = context; internal readonly object Frozen = frozen; internal int References; }
        private static readonly Dictionary<(string Database, string Quest), Scope> scopes = [];
        private sealed class RewardScope(object owner, object frozen) { internal readonly object Owner = owner; internal readonly object Frozen = frozen; internal int References; }
        private static readonly Dictionary<(string Database, string Character), RewardScope> rewards = [];
        private sealed record FrozenOwner(object Owner, object Token);
        private static readonly Dictionary<(string Database, string Character), FrozenOwner> frozenSaves = [];
        internal static IDisposable AcquireFrozen(object owner, object token, string database, ulong character)
        {
            lock (gate)
            {
                var key = (database, character.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (frozenSaves.ContainsKey(key) || rewards.ContainsKey(key) || scopes.Keys.Any(k => k.Database == database && k.Quest.StartsWith(key.Item2 + ":", StringComparison.Ordinal)))
                    throw new InvalidOperationException("Quest character already has persistence ownership.");
                var entry = new FrozenOwner(owner, token); frozenSaves.Add(key, entry); return new FrozenLease(key, entry);
            }
        }
        internal static bool TryAcquireReward(object owner, string database, ulong character, out IDisposable lease, object frozen = null)
        {
            lock (gate)
            {
                lease = null; string id = character.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var key = (database, id);
                if (frozenSaves.TryGetValue(key, out var saved) && (!ReferenceEquals(saved.Owner, owner) || !ReferenceEquals(saved.Token, frozen))) return false;
                if (scopes.Any(k => k.Key.Database == database && k.Key.Quest.StartsWith(id + ":", StringComparison.Ordinal)
                    && (frozen == null || !ReferenceEquals(k.Value.Frozen, frozen)))) return false;
                if (rewards.TryGetValue(key, out var current) && !ReferenceEquals(current.Owner, owner)) return false;
                if (current == null) { current = new(owner, frozen); rewards.Add(key, current); }
                ++current.References; lease = new RewardLease(key, current); return true;
            }
        }
        internal static IDisposable Acquire(CharacterContext context, string database, string quest, object frozen = null)
        {
            lock (gate)
            {
                var key = (database, quest);
                string character = quest.Split(':', 2)[0];
                if (frozenSaves.TryGetValue((database, character), out var saved) && !ReferenceEquals(saved.Token, frozen)
                    || frozen != null && (saved == null || !ReferenceEquals(saved.Token, frozen)))
                    throw new InvalidOperationException("Quest save lost its frozen owner.");
                if (rewards.TryGetValue((database, character), out var reward) && (frozen == null || !ReferenceEquals(reward.Frozen, frozen)))
                    throw new InvalidOperationException("Quest character belongs to an unfinished reward unit.");
                if (!scopes.TryGetValue(key, out var scope)) { scope = new(context, frozen); scopes.Add(key, scope); }
                if (!ReferenceEquals(scope.Context, context) || !ReferenceEquals(scope.Frozen, frozen)) throw new InvalidOperationException("Quest aggregate has an unfinished save unit.");
                ++scope.References; return new Lease(key, scope);
            }
        }
        private sealed class FrozenLease((string Database, string Character) key, FrozenOwner entry) : IDisposable
        {
            public void Dispose()
            {
                lock (gate) if (frozenSaves.TryGetValue(key, out var current) && ReferenceEquals(current, entry)) frozenSaves.Remove(key);
            }
        }
        private sealed class Lease((string Database, string Quest) key, Scope scope) : IDisposable
        {
            private bool released;
            public void Dispose()
            {
                lock (gate)
                {
                    if (released) return; released = true;
                    if (--scope.References == 0 && scopes.TryGetValue(key, out var current) && ReferenceEquals(current, scope)) scopes.Remove(key);
                }
            }
        }
        private sealed class RewardLease((string Database, string Character) key, RewardScope scope) : IDisposable
        {
            private bool released;
            public void Dispose()
            {
                lock (gate)
                {
                    if (released) return; released = true;
                    if (--scope.References == 0 && rewards.TryGetValue(key, out var current) && ReferenceEquals(current, scope)) rewards.Remove(key);
                }
            }
        }
    }
}
