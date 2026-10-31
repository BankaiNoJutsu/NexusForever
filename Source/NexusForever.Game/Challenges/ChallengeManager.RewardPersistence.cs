using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private RewardPersistenceLease activeRewardLease;
        private int rewardQueueRequests;
        private bool rewardOwnerInvalidated;
        private bool rewardPersistenceRequiresReload;

        public bool HasPendingRewardOperation { get { lock (persistenceGate) return activeRewardLease != null || rewardQueueRequests != 0; } }
        public bool RequiresAuthoritativeReload { get { lock (persistenceGate) return rewardPersistenceRequiresReload; } }

        internal sealed record RewardIntent(ChallengeManager Owner, ushort ChallengeId, CharacterChallengeState Runtime, long Revision);

        internal sealed record RewardPersistenceIdentity(string PersistenceDatabase, string ExecutionDatabase);

        // Provider initialization may perform discovery. The queue captures this
        // immutable identity on its worker, then disposes that unused context.
        internal static RewardPersistenceIdentity CaptureRewardPersistenceIdentity(CharacterContext identity)
        {
            ArgumentNullException.ThrowIfNull(identity);
            if (identity.Database.CurrentTransaction != null || identity.ChangeTracker.Entries().Any()
                || identity.IsSaveRejected || identity.HasSavepointRollback)
                throw new InvalidOperationException("Reward identity requires a fresh context outside a transaction.");
            return new(ChallengeSaveDatabaseKey(identity), Item.GetRewardDatabaseKey(identity));
        }

        internal RewardIntent CaptureRewardIntent(ushort id)
        {
            lock (persistenceGate)
                return new(this, id, activeChallenges.TryGetValue(id, out var state)
                    ? CharacterChallengeState.FromModel(BuildModel(state)) : null, dirtyChallenges.GetValueOrDefault(id));
        }

        // Queue ownership spans gaps between SQL units. A later tier's provisional
        // intent must not leak through a full Player save between acknowledgements.
        internal IDisposable HoldRewardQueue()
        {
            lock (persistenceGate)
            {
                if (rewardOwnerInvalidated || rewardPersistenceRequiresReload)
                    throw new InvalidOperationException("Challenge reward owner is invalidated.");
                ++rewardQueueRequests;
                return new QueueOwnership(this);
            }
        }

        private sealed class QueueOwnership(ChallengeManager owner) : IDisposable
        {
            private bool released;
            public void Dispose()
            {
                lock (owner.persistenceGate)
                {
                    if (released) return;
                    released = true; --owner.rewardQueueRequests;
                }
            }
        }

        internal bool TryAcquireRewardPersistence(CharacterContext identity, IReadOnlyCollection<RewardIntent> intents,
            out RewardPersistenceLease lease, IInventoryRewardReservation delivery = null)
            => TryAcquireRewardPersistence(CaptureRewardPersistenceIdentity(identity), intents, out lease, delivery);

        internal bool TryAcquireRewardPersistence(RewardPersistenceIdentity identity, IReadOnlyCollection<RewardIntent> intents,
            out RewardPersistenceLease lease, IInventoryRewardReservation delivery = null)
        {
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(intents);
            string persistenceDatabase = identity.PersistenceDatabase;
            string executionDatabase = identity.ExecutionDatabase;
            lease = null;
            IInventory inventory = player?.Inventory;
            if (inventory?.SupportsTransactionalSoulboundDelivery != true) return false;
            IDisposable mutation = null;
            bool acquired = false;
            try
            {
                if (delivery != null)
                {
                    if (!inventory.OwnsRewardReservation(delivery) || delivery.CharacterId != characterId) return false;
                }
                else if (!inventory.TryAcquireMutationLease(out mutation)) return false;
                lock (persistenceGate)
                {
                    if (activeSave != null || activeRewardLease != null || rewardOwnerInvalidated || rewardPersistenceRequiresReload) return false;
                    if (intents.Any(intent => !ReferenceEquals(intent.Owner, this)) || intents.Select(intent => intent.ChallengeId).Distinct().Count() != intents.Count)
                        throw new InvalidOperationException("Reward intent belongs to another owner or is duplicated.");
                    var unit = new ChallengeSaveUnit { Key = (persistenceDatabase, characterId), RewardOwner = true };
                    lock (saveLeaseGate)
                    {
                        if (saveLeases.ContainsKey(unit.Key)) return false;
                        saveLeases.Add(unit.Key, unit);
                    }
                    try
                    {
                        var candidates = new Dictionary<ushort, IReadOnlyList<CharacterChallengeState>>();
                        foreach (ushort id in persistenceBaselines.Keys.Concat(uncertainPersistenceStates.Keys).Concat(intents.Select(intent => intent.ChallengeId)).Distinct())
                        {
                            var possible = new List<CharacterChallengeState> { CharacterChallengeState.FromModel(persistenceBaselines.GetValueOrDefault(id)) };
                            if (uncertainPersistenceStates.TryGetValue(id, out var uncertain)) possible.AddRange(uncertain.Select(CharacterChallengeState.FromModel));
                            candidates.Add(id, Array.AsReadOnly(possible.Distinct().ToArray()));
                        }
                        lease = new(this, unit, executionDatabase, intents.ToDictionary(intent => intent.ChallengeId), candidates, mutation, delivery);
                        activeRewardLease = lease; acquired = true;
                        return true;
                    }
                    catch { lock (saveLeaseGate) saveLeases.Remove(unit.Key); throw; }
                }
            }
            finally { if (!acquired) mutation?.Dispose(); }
        }

        public void InvalidateRewardOperations(bool final = false)
        {
            RewardPersistenceLease cancel = null;
            lock (persistenceGate)
            {
                rewardOwnerInvalidated = true;
                if (activeRewardLease?.IsPrepared == true)
                {
                    cancel = activeRewardLease;
                    rewardPersistenceRequiresReload = true;
                }
                if (activeRewardLease == null && rewardQueueRequests != 0) rewardPersistenceRequiresReload = true;
                if (final && (activeRewardLease != null || rewardQueueRequests != 0)) rewardPersistenceRequiresReload = true;
            }
            cancel?.Dispose();
            rewardQueue?.Invalidate(final);
        }

        internal void RequireRewardReload()
        {
            lock (persistenceGate) rewardPersistenceRequiresReload = true;
        }

        private enum RewardLeaseState { Prepared, Executing, Adopted, Canceled, Quarantined }

        internal sealed class RewardPersistenceLease : IDisposable
        {
            private readonly ChallengeManager owner;
            private readonly ChallengeSaveUnit unit;
            private readonly string database;
            private readonly Dictionary<ushort, RewardIntent> intents;
            private readonly Dictionary<ushort, IReadOnlyList<CharacterChallengeState>> candidates;
            private Dictionary<ushort, CharacterChallengeState> resolved;
            private IDisposable mutation;
            private readonly IInventoryRewardReservation delivery;
            private CharacterChallengeRewardCommand command;
            private RewardLeaseState state;
            internal bool IsPrepared => state == RewardLeaseState.Prepared;
            internal bool IsExecuting => state == RewardLeaseState.Executing;
            internal bool NeedsLoadProbe => resolved == null && candidates.Values.Any(possible => possible.Count > 1);
            internal bool CanShareQuestOwner(IPlayer player, string database)
                => ReferenceEquals(owner.player, player) && this.database == database && delivery == null && IsPrepared;

            internal RewardPersistenceLease(ChallengeManager owner, ChallengeSaveUnit unit, string database,
                Dictionary<ushort, RewardIntent> intents, Dictionary<ushort, IReadOnlyList<CharacterChallengeState>> candidates,
                IDisposable mutation, IInventoryRewardReservation delivery)
            {
                this.owner = owner; this.unit = unit; this.database = database; this.intents = intents;
                this.candidates = candidates; this.mutation = mutation; this.delivery = delivery;
            }

            public CharacterChallengeState Expected(ushort id)
            {
                lock (owner.persistenceGate)
                {
                    if (resolved != null) return resolved.GetValueOrDefault(id);
                    var possible = candidates.GetValueOrDefault(id) ?? new CharacterChallengeState[] { null };
                    if (possible.Count != 1) throw new InvalidOperationException("Uncertain Challenge baselines require a committed load probe.");
                    return possible[0];
                }
            }

            public Task<CharacterChallengeRewardExecution> ExecuteAsync(CharacterChallengeRewardExecutor executor,
                CharacterChallengeRewardCommand next, CancellationToken cancellation = default)
            {
                ArgumentNullException.ThrowIfNull(executor);
                ArgumentNullException.ThrowIfNull(next);
                lock (owner.persistenceGate)
                {
                    if (!ReferenceEquals(owner.activeRewardLease, this) || state != RewardLeaseState.Prepared
                        || owner.rewardOwnerInvalidated || next.CharacterId != owner.characterId)
                        throw new InvalidOperationException("Challenge reward lease is not ready for execution.");
                    if (next.After != null && (!intents.ContainsKey(next.After.ChallengeId) || next.Before != Expected(next.After.ChallengeId)))
                        throw new InvalidOperationException("Reward command lost its captured intent or persistence baseline.");
                    command = next; state = RewardLeaseState.Executing;
                }
                return executor.ExecuteOwnedAsync(next, database, delivery, cancellation);
            }

            public Task<CharacterChallengeRewardExecution> ResumeAsync(CharacterChallengeRewardExecutor executor,
                CharacterChallengeRewardExecution previous, CancellationToken cancellation = default)
            {
                lock (owner.persistenceGate)
                    if (!ReferenceEquals(owner.activeRewardLease, this) || state != RewardLeaseState.Executing)
                        throw new InvalidOperationException("Challenge reward lease no longer owns execution.");
                return executor.ResumeAsync(command, previous, delivery, cancellation);
            }

            public bool CompleteLoad(CharacterChallengeRewardExecution result)
            {
                IDisposable release = null;
                bool accepted = false;
                lock (owner.persistenceGate)
                {
                    if (!Owns(result) || command.Kind != CharacterChallengeRewardCommandKind.Load
                        || result.Outcome != CharacterChallengeRewardExecutionOutcome.Committed || !result.OriginalUnitEnded) return false;
                    try
                    {
                        var actual = Rows(result);
                        ValidateBaselines(actual, []);
                        resolved = actual; state = RewardLeaseState.Prepared; command = null; accepted = true;
                    }
                    catch { release = Quarantine(); }
                }
                release?.Dispose(); return accepted;
            }

            public bool AdoptCommitted(CharacterChallengeRewardExecution result) => AdoptCommitted(result, null);

            internal bool AdoptCommitted(CharacterChallengeRewardExecution result, QuestManager.QuestRewardCapture quests)
            {
                IDisposable release = null;
                bool accepted = false;
                lock (owner.persistenceGate)
                {
                    if (!Owns(result) || result.Outcome != CharacterChallengeRewardExecutionOutcome.Committed || !result.OriginalUnitEnded
                        || command.Kind == CharacterChallengeRewardCommandKind.Load) return false;
                    try
                    {
                        var actual = Rows(result);
                        var targets = command.After == null ? new HashSet<ushort>() : new HashSet<ushort> { command.After.ChallengeId };
                        ValidateBaselines(actual, targets);
                        var merged = new Dictionary<ushort, CharacterChallengeState>();
                        foreach (ushort id in targets)
                        {
                            if (!actual.TryGetValue(id, out var committed)) throw new InvalidOperationException("Committed Challenge row is missing.");
                            var intent = intents[id];
                            var current = owner.activeChallenges.TryGetValue(id, out var runtime) ? CharacterChallengeState.FromModel(owner.BuildModel(runtime)) : null;
                            merged.Add(id, owner.RetainsLaterProducerIdentity(intent, current) ? current : Merge(current, intent.Runtime, committed));
                        }
                        void Apply()
                        {
                            foreach (var pair in actual) owner.persistenceBaselines[pair.Key] = pair.Value.ToModel();
                            foreach (ushort id in candidates.Keys.Except(actual.Keys)) owner.persistenceBaselines.Remove(id);
                            foreach (ushort id in candidates.Keys.Concat(actual.Keys)) owner.uncertainPersistenceStates.Remove(id);
                            foreach (var pair in merged)
                            {
                                owner.ApplyRewardRuntime(pair.Value);
                                if (owner.dirtyChallenges.GetValueOrDefault(pair.Key) == intents[pair.Key].Revision && pair.Value == actual[pair.Key])
                                    owner.dirtyChallenges.Remove(pair.Key);
                                owner.SettleProducerIntent(intents[pair.Key]);
                            }
                        }
                        if (quests == null) Apply();
                        else if (!quests.AdoptCommitted(result, this, Apply)) throw new InvalidOperationException("Quest participant adoption was rejected.");
                        state = RewardLeaseState.Adopted; release = Release(); accepted = true;
                    }
                    catch { release = Quarantine(); }
                }
                release?.Dispose(); return accepted;
            }

            public bool AbandonAfterTerminal(CharacterChallengeRewardExecution result)
            {
                IDisposable release;
                lock (owner.persistenceGate)
                {
                    if (!Owns(result)) return false;
                    owner.rewardPersistenceRequiresReload = true;
                    if (!result.OriginalUnitEnded) return false;
                    release = Quarantine();
                }
                release?.Dispose(); return true;
            }

            private bool Owns(CharacterChallengeRewardExecution result)
                => ReferenceEquals(owner.activeRewardLease, this) && state == RewardLeaseState.Executing
                    && CharacterChallengeRewardExecutor.IsCompletionOf(result, command, database, delivery);

            private Dictionary<ushort, CharacterChallengeState> Rows(CharacterChallengeRewardExecution result)
            {
                if (result.State == null || result.State.Challenges.Any(row => row.CharacterId != owner.characterId))
                    throw new InvalidOperationException("Reward result has another character's Challenge state.");
                return result.State.Challenges.ToDictionary(row => row.ChallengeId);
            }

            private void ValidateBaselines(Dictionary<ushort, CharacterChallengeState> actual, HashSet<ushort> targets)
            {
                foreach (ushort id in candidates.Keys.Concat(actual.Keys).Distinct().Except(targets))
                {
                    var possible = resolved != null
                        ? new CharacterChallengeState[] { resolved.GetValueOrDefault(id) }
                        : candidates.GetValueOrDefault(id) ?? new CharacterChallengeState[] { null };
                    if (!possible.Contains(actual.GetValueOrDefault(id))) throw new InvalidOperationException("Challenge state changed outside the captured save baseline.");
                }
            }

            private IDisposable Quarantine()
            {
                owner.rewardPersistenceRequiresReload = true; state = RewardLeaseState.Quarantined;
                return Release();
            }
            private IDisposable Release()
            {
                if (ReferenceEquals(owner.activeRewardLease, this)) owner.activeRewardLease = null;
                lock (saveLeaseGate)
                    if (saveLeases.TryGetValue(unit.Key, out var current) && ReferenceEquals(current, unit)) saveLeases.Remove(unit.Key);
                var held = mutation; mutation = null; return held;
            }
            public void Dispose()
            {
                IDisposable release = null;
                lock (owner.persistenceGate)
                    if (state == RewardLeaseState.Prepared && ReferenceEquals(owner.activeRewardLease, this))
                    { state = RewardLeaseState.Canceled; release = Release(); }
                release?.Dispose();
            }
        }

        private static CharacterChallengeState Merge(CharacterChallengeState current, CharacterChallengeState captured, CharacterChallengeState committed)
        {
            if (current == null && captured == null) return committed;
            if (current == null || captured == null || current.CharacterId != committed.CharacterId || current.ChallengeId != committed.ChallengeId
                || current.AttemptId != captured.AttemptId && current.AttemptId != committed.AttemptId)
                throw new InvalidOperationException("A newer Challenge identity cannot be overwritten by an old reward result.");
            static T Value<T>(T now, T before, T durable) => EqualityComparer<T>.Default.Equals(now, before) ? durable : now;
            return current with
            {
                AttemptId = Value(current.AttemptId, captured.AttemptId, committed.AttemptId),
                Activated = Value(current.Activated, captured.Activated, committed.Activated),
                OnCooldown = Value(current.OnCooldown, captured.OnCooldown, committed.OnCooldown),
                LeftArea = Value(current.LeftArea, captured.LeftArea, committed.LeftArea),
                Count = Value(current.Count, captured.Count, committed.Count), Tier = Value(current.Tier, captured.Tier, committed.Tier),
                LastRewardTier = Value(current.LastRewardTier, captured.LastRewardTier, committed.LastRewardTier),
                Completions = Value(current.Completions, captured.Completions, committed.Completions),
                ActiveSeconds = Value(current.ActiveSeconds, captured.ActiveSeconds, committed.ActiveSeconds),
                CooldownSeconds = Value(current.CooldownSeconds, captured.CooldownSeconds, committed.CooldownSeconds),
                AreaSeconds = Value(current.AreaSeconds, captured.AreaSeconds, committed.AreaSeconds)
            };
        }

        private void ApplyRewardRuntime(CharacterChallengeState state)
        {
            if (!activeChallenges.TryGetValue(state.ChallengeId, out var runtime))
                activeChallenges.Add(state.ChallengeId, runtime = new ChallengeRuntimeState { ChallengeId = state.ChallengeId });
            runtime.ActiveAttemptId = state.AttemptId; runtime.Activated = state.Activated; runtime.OnCooldown = state.OnCooldown;
            runtime.LeftArea = state.LeftArea; runtime.CurrentCount = state.Count; runtime.CurrentTier = state.Tier;
            runtime.LastRewardTier = state.LastRewardTier; runtime.CompletionCount = state.Completions;
            runtime.ActiveTimer = state.ActiveSeconds; runtime.CooldownTimer = state.CooldownSeconds; runtime.AreaFailTimer = state.AreaSeconds;
        }
    }
}
