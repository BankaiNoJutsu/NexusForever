using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private readonly object persistenceGate = new();
        private long persistenceRevision;
        private readonly Dictionary<ushort, CharacterChallengeModel> persistenceBaselines = [];
        private readonly Dictionary<ushort, List<CharacterChallengeModel>> uncertainPersistenceStates = [];
        private ChallengeSaveUnit activeSave;

        // This orders live owners using the same configured database identity. It
        // is not a persistent reward receipt or a multi-process concurrency token.
        private static readonly object saveLeaseGate = new();
        private static readonly Dictionary<(string Database, ulong Character), ChallengeSaveUnit> saveLeases = [];

        internal sealed class ChallengeSaveUnit
        {
            internal NormalSaveSnapshot Frozen;
            public CharacterContext Context;
            public (string Database, ulong Character) Key;
            public readonly Dictionary<ushort, ChallengeSaveRow> Rows = [];
            public EventHandler<SavedChangesEventArgs> OnFlushed;
            public bool RewardOwner;
        }

        internal sealed class ChallengeSaveRow
        {
            public CharacterChallengeModel Model;
            public long StagedRevision;
            public readonly List<CharacterChallengeModel> PossibleStates = [];
            public bool FlushObserved;
            public CharacterChallengeModel FlushedState;
            public long FlushedRevision;
        }

        private void SaveChallengePersistence(CharacterContext context, NormalSaveSnapshot frozen = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            lock (persistenceGate)
            {
                if (frozen == null && (contractCompanion != null || rewardPersistenceRequiresReload || activeRewardLease != null || rewardQueueRequests != 0 || activeSave?.Frozen != null))
                {
                    var error = new InvalidOperationException("Challenge save overlaps pending or quarantined reward work.");
                    context.RejectSave(error); throw error;
                }
                if (activeSave != null && (activeSave.Context != null && !ReferenceEquals(activeSave.Context, context) || !ReferenceEquals(activeSave.Frozen, frozen)))
                    throw new InvalidOperationException($"Character {characterId} already has an unfinished challenge save unit.");
                if (activeSave != null && context.HasSavepointRollback)
                    throw new InvalidOperationException($"Character {characterId} requires a fresh challenge save context after savepoint rollback.");

                var snapshots = frozen?.Rows ?? new Dictionary<ushort, CharacterChallengeModel>();
                foreach ((ushort id, ChallengeRuntimeState runtime) in frozen == null ? activeChallenges.ToArray() : [])
                {
                    CharacterChallengeModel snapshot = BuildModel(runtime);
                    // Nonterminal timer ticks intentionally do not call MarkDirty.
                    // Compare the complete runtime state at the save boundary too.
                    if (!dirtyChallenges.ContainsKey(id)
                        && !uncertainPersistenceStates.ContainsKey(id)
                        && SameChallengeState(persistenceBaselines.GetValueOrDefault(id), snapshot))
                        continue;
                    snapshots.Add(id, snapshot);
                }
                if (snapshots.Count == 0 && frozen == null)
                    return;

                ChallengeSaveUnit unit = activeSave;
                if (unit == null || unit.Context == null)
                {
                    if (unit == null)
                    {
                        unit = new ChallengeSaveUnit { Key = (ChallengeSaveDatabaseKey(context), characterId) };
                        lock (saveLeaseGate)
                        {
                            if (saveLeases.TryGetValue(unit.Key, out var other))
                            {
                                var error = new InvalidOperationException($"Character {characterId} is staged by another live challenge owner.");
                                if (other.RewardOwner) context.RejectSave(error);
                                throw error;
                            }
                            saveLeases.Add(unit.Key, unit);
                        }
                    }
                    unit.Context = context;
                    activeSave = unit;
                    unit.OnFlushed = (_, _) => CaptureChallengeFlush(unit);
                    context.SavedChanges += unit.OnFlushed;
                    // Register before any reconciliation/attach can fail. The
                    // context's terminal outcome always releases this unit's lease.
                    context.OnSaveCompleted(committed => CompleteChallengeSave(unit, committed));
                }

                foreach ((ushort id, CharacterChallengeModel snapshot) in snapshots)
                {
                    if (!unit.Rows.TryGetValue(id, out ChallengeSaveRow row))
                    {
                        CharacterChallengeModel baseline;
                        if (frozen?.ComposedBaselines?.TryGetValue(id, out var composed) == true)
                        {
                            baseline = context.CharacterChallenge.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.ChallengeId == id);
                            if (!SameChallengeState(composed, baseline)) throw new InvalidOperationException("Contract Challenge baseline changed after command staging.");
                        }
                        else baseline = ReconcileChallengeBaseline(context, id);
                        EntityEntry<CharacterChallengeModel> tracked = context.ChangeTracker.Entries<CharacterChallengeModel>()
                            .SingleOrDefault(entry => entry.Entity.Id == characterId && entry.Entity.ChallengeId == id);
                        if (tracked != null && (baseline == null || tracked.State == EntityState.Added
                            || !SameChallengeState(baseline, (CharacterChallengeModel)tracked.OriginalValues.ToObject())))
                            throw new InvalidOperationException($"Challenge {id} requires its complete loaded model in the shared save context.");

                        row = new ChallengeSaveRow();
                        row.PossibleStates.Add(CopyChallengeModel(baseline));
                        if (tracked != null)
                            row.Model = tracked.Entity;
                        else if (baseline == null)
                        {
                            row.Model = CopyChallengeModel(snapshot);
                            context.CharacterChallenge.Add(row.Model);
                        }
                        else
                        {
                            row.Model = CopyChallengeModel(baseline);
                            context.CharacterChallenge.Attach(row.Model);
                        }
                        unit.Rows.Add(id, row);
                    }

                    EntityEntry<CharacterChallengeModel> entry = context.Entry(row.Model);
                    if (entry.State is EntityState.Detached or EntityState.Deleted)
                        throw new InvalidOperationException($"Challenge {id} lost its tracked save ownership.");
                    entry.CurrentValues.SetValues(snapshot);
                    row.StagedRevision = (frozen?.Revisions ?? dirtyChallenges).GetValueOrDefault(id);
                    row.PossibleStates.Add(CopyChallengeModel(snapshot));
                }
            }
        }

        private CharacterChallengeModel ReconcileChallengeBaseline(CharacterContext context, ushort id)
        {
            CharacterChallengeModel actual = context.CharacterChallenge.AsNoTracking()
                .SingleOrDefault(model => model.Id == characterId && model.ChallengeId == id);
            if (uncertainPersistenceStates.TryGetValue(id, out List<CharacterChallengeModel> possible))
            {
                if (!possible.Any(state => SameChallengeState(state, actual)))
                    throw new InvalidOperationException($"Challenge {id} changed outside its pending save unit.");
                persistenceBaselines[id] = CopyChallengeModel(actual);
                uncertainPersistenceStates.Remove(id);
            }
            else if (!SameChallengeState(persistenceBaselines.GetValueOrDefault(id), actual))
                throw new InvalidOperationException($"Challenge {id} no longer matches its loaded state.");
            return actual;
        }

        private void CaptureChallengeFlush(ChallengeSaveUnit unit)
        {
            lock (persistenceGate)
            {
                if (!ReferenceEquals(activeSave, unit))
                    return;
                foreach (ChallengeSaveRow row in unit.Rows.Values)
                {
                    row.FlushObserved = true;
                    row.FlushedState = CopyChallengeModel(row.Model);
                    row.FlushedRevision = row.StagedRevision;
                    row.PossibleStates.Add(CopyChallengeModel(row.FlushedState));
                }
            }
        }

        private void CompleteChallengeSave(ChallengeSaveUnit unit, bool committed)
        {
            lock (persistenceGate)
            {
                if (!ReferenceEquals(activeSave, unit))
                    return;
                try
                {
                    foreach ((ushort id, ChallengeSaveRow row) in unit.Rows)
                    {
                        if (!committed || !row.FlushObserved)
                        {
                            uncertainPersistenceStates[id] = row.PossibleStates;
                            continue;
                        }
                        persistenceBaselines[id] = CopyChallengeModel(row.FlushedState);
                        uncertainPersistenceStates.Remove(id);
                        if (dirtyChallenges.GetValueOrDefault(id) == row.FlushedRevision
                            && activeChallenges.TryGetValue(id, out ChallengeRuntimeState current)
                            && SameChallengeState(BuildModel(current), row.FlushedState))
                            dirtyChallenges.Remove(id);
                        // A timer can move without MarkDirty. A later Save compares
                        // its complete state with this durable baseline regardless.
                    }
                }
                finally
                {
                    bool retain = !committed && unit.Context.IsProvenBidRollback && unit.Frozen is NormalSaveSnapshot { RetainBidRollback: true };
                    unit.Context.SavedChanges -= unit.OnFlushed;
                    if (retain)
                    {
                        unit.Context = null; unit.OnFlushed = null; unit.Rows.Clear();
                    }
                    else
                    {
                        activeSave = null;
                        lock (saveLeaseGate)
                            if (saveLeases.TryGetValue(unit.Key, out ChallengeSaveUnit current) && ReferenceEquals(current, unit))
                                saveLeases.Remove(unit.Key);
                    }
                }
            }
        }

        private static string ChallengeSaveDatabaseKey(CharacterContext context)
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = context.Database.GetDbConnection().ConnectionString };
            builder.Remove("Password");
            builder.Remove("Pwd");
            var normalized = new DbConnectionStringBuilder();
            foreach (string key in builder.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                normalized[key.ToLowerInvariant()] = builder[key];
            return context.Database.ProviderName + ":" + normalized.ConnectionString;
        }

        private static CharacterChallengeModel CopyChallengeModel(CharacterChallengeModel model) => model == null ? null : new()
        {
            Id = model.Id, ChallengeId = model.ChallengeId, ActiveAttemptId = model.ActiveAttemptId, Activated = model.Activated,
            OnCooldown = model.OnCooldown, LeftArea = model.LeftArea, CurrentCount = model.CurrentCount,
            CurrentTier = model.CurrentTier, LastRewardTier = model.LastRewardTier, CompletionCount = model.CompletionCount,
            ActiveTimerSeconds = model.ActiveTimerSeconds, CooldownTimerSeconds = model.CooldownTimerSeconds,
            AreaFailTimerSeconds = model.AreaFailTimerSeconds
        };

        private static bool SameChallengeState(CharacterChallengeModel left, CharacterChallengeModel right)
        {
            if (left == null || right == null)
                return left == right;
            return left.Id == right.Id && left.ChallengeId == right.ChallengeId && left.ActiveAttemptId == right.ActiveAttemptId
                && left.Activated == right.Activated
                && left.OnCooldown == right.OnCooldown && left.LeftArea == right.LeftArea
                && left.CurrentCount == right.CurrentCount && left.CurrentTier == right.CurrentTier
                && left.LastRewardTier == right.LastRewardTier && left.CompletionCount == right.CompletionCount
                && left.ActiveTimerSeconds.Equals(right.ActiveTimerSeconds)
                && left.CooldownTimerSeconds.Equals(right.CooldownTimerSeconds)
                && left.AreaFailTimerSeconds.Equals(right.AreaFailTimerSeconds);
        }
    }
}
