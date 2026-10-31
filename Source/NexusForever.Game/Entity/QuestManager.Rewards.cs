using System.Data;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private readonly object questRewardGate = new();
        private readonly List<QuestRewardCapture> questRewards = [];
        private bool questRewardReload, questRewardInvalidated;
        private int questRewardCaptureDepth;
        internal bool IsRewardCaptureInProgress => questRewardCaptureDepth != 0;
        public bool HasPendingRewardOperation { get { lock (questRewardGate) return contractClaimOperation != null || questRewardCaptureDepth != 0 || questRewards.Count != 0; } }
        public bool RequiresAuthoritativeReload { get { lock (questRewardGate) return questRewardReload; } }

        private Dictionary<ushort, QuestRewardGraph> RewardCensus()
            => completedQuests.Values.Concat(inactiveQuests.Values).Concat(activeQuests.Values).Distinct()
                .Select(quest => quest is Quest.Quest concrete ? concrete.CaptureRewardGraph(normalSave?.Token)
                    : throw new InvalidOperationException("Quest capture requires concrete actor ownership."))
                .ToDictionary(graph => graph.Owner.Id);

        internal bool TryCaptureRewardCompletion(string database, Action completion, out QuestRewardCapture capture)
        {
            ArgumentException.ThrowIfNullOrEmpty(database); ArgumentNullException.ThrowIfNull(completion); capture = null;
            lock (questRewardGate)
            {
                if (questRewardReload || questRewardInvalidated || HasContractCompletionWork && normalSave == null
                    || player is Player real && real.IsNormalSaveInProgress && normalSave == null) return false;
                if (questRewards.Any(pending => pending.Database != database)) throw new InvalidOperationException("Quest captures cannot change their retained database.");
                if (!QuestSaveScope.TryAcquireReward(this, database, player.CharacterId, out var scope, normalSave?.Token)) return false;
                bool invoked = false; Dictionary<ushort, QuestRewardGraph> before = null;
                try
                {
                    DrainCommittedDeletions(); before = RewardCensus();
                    invoked = true;
                    try { questRewardCaptureDepth++; completion(); }
                    finally { questRewardCaptureDepth--; }
                    var after = RewardCensus();
                    capture = new(this, database, before, after, scope); questRewards.Add(capture); return true;
                }
                catch
                {
                    if (invoked)
                    {
                        questRewardReload = true;
                        foreach (var graph in before.Values) graph.Retire();
                        // A script may create a Quest before throwing, even when
                        // a later complete census is impossible.
                        foreach (var quest in completedQuests.Values.Concat(inactiveQuests.Values).Concat(activeQuests.Values).OfType<Quest.Quest>())
                            quest.RetireRewardOwner();
                    }
                    scope.Dispose(); throw;
                }
            }
        }

        public void InvalidateRewardOperations(bool final = false)
        {
            lock (questRewardGate)
            {
                questRewardInvalidated = true;
                contractClaimOperation?.Invalidate();
                if (questRewards.Count != 0) questRewardReload = true;
                foreach (var capture in questRewards.ToArray()) capture.Invalidate(final);
            }
        }

        internal sealed class QuestRewardCapture : IDisposable
        {
            private enum Phase { Captured, Preparing, Executing, Ended }
            private readonly QuestManager owner;
            private readonly string database;
            internal string Database => database;
            private readonly Dictionary<ushort, QuestRewardGraph> before, after;
            private readonly ushort[] affected;
            private IDisposable scope;
            private Phase phase;
            private object preparationToken;
            private CharacterChallengeRewardCommand command;
            private object queueOwner;
            private ChallengeManager.RewardPersistenceLease companion;
            private Preparation prepared;
            private bool invalidated, finalized;
            private Task<CharacterChallengeRewardExecution> executing;
            private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Preparation, object> preparations = new();

            internal sealed class Preparation(object token, IReadOnlyList<CharacterChallengeQuestChange> changes)
            {
                internal readonly object Token = token;
                internal IReadOnlyList<CharacterChallengeQuestChange> Changes { get; } = changes;
            }
            private sealed record Input(ushort Id, IReadOnlyList<CharacterChallengeQuestState> Roots,
                IReadOnlyDictionary<byte, IReadOnlyList<CharacterChallengeQuestObjectiveState>> Objectives,
                CharacterChallengeQuestSnapshot Target);

            internal QuestRewardCapture(QuestManager owner, string database, Dictionary<ushort, QuestRewardGraph> before,
                Dictionary<ushort, QuestRewardGraph> after, IDisposable scope)
            {
                this.owner = owner; this.database = database; this.before = before; this.after = after; this.scope = scope;
                affected = before.Keys.Concat(after.Keys).Distinct().Where(id => !before.TryGetValue(id, out var old)
                    || !after.TryGetValue(id, out var next) || next.ChangedFrom(old)).Order().ToArray();
            }

            private void Head()
            {
                if (phase == Phase.Ended || invalidated || owner.questRewardReload || owner.questRewardInvalidated
                    || owner.questRewards.FirstOrDefault() != this)
                    throw new InvalidOperationException("Quest reward capture is not the current valid FIFO owner.");
            }

            internal bool TryQueue(object queue, IPlayer player)
            {
                lock (owner.questRewardGate)
                {
                    if (queue == null || queueOwner != null || phase != Phase.Captured || invalidated
                        || owner.questRewardReload || owner.questRewardInvalidated || !ReferenceEquals(owner.player, player)) return false;
                    int position = owner.questRewards.IndexOf(this);
                    if (position < 0 || owner.questRewards.Take(position).Any(previous => !ReferenceEquals(previous.queueOwner, queue))) return false;
                    queueOwner = queue; return true;
                }
            }

            private void CheckQueue(object queue)
            {
                if (!ReferenceEquals(queueOwner, queue)) throw new InvalidOperationException("Quest capture belongs to another queue.");
            }

            internal Task<Preparation> PrepareAsync(Func<CharacterContext> contexts, object queue = null)
            {
                ArgumentNullException.ThrowIfNull(contexts);
                lock (owner.questRewardGate)
                {
                    Head(); CheckQueue(queue);
                    if (owner.normalSave != null) throw new InvalidOperationException("Quest probe must wait for the frozen normal save to end.");
                    if (phase != Phase.Captured) throw new InvalidOperationException("Quest capture already has a preparation unit.");
                    var inputs = affected.Select(id =>
                    {
                        var baseline = before.GetValueOrDefault(id) ?? after[id];
                        return new Input(id, baseline.Root.Candidates(),
                            new ReadOnlyDictionary<byte, IReadOnlyList<CharacterChallengeQuestObjectiveState>>(
                                baseline.Objectives.ToDictionary(p => p.Key, p => p.Value.Candidates())), after.GetValueOrDefault(id)?.Snapshot);
                    }).ToArray();
                    phase = Phase.Preparing; preparationToken = new object();
                    // Only copied scalar input and an opaque identity token cross
                    // to this worker. No Quest, manager or row callback runs here.
                    return PrepareWorker(contexts, database, owner.player.CharacterId, inputs, preparationToken);
                }
            }

            private static Task<Preparation> PrepareWorker(Func<CharacterContext> contexts, string database, ulong character, Input[] inputs, object token)
                => Task.Run(async () =>
                {
                    await using var strategyContext = contexts() ?? throw new InvalidOperationException("Quest probe context factory returned null.");
                    ValidateProbeContext(strategyContext, database);
                    return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                    {
                        await using var context = contexts() ?? throw new InvalidOperationException("Quest probe context factory returned null.");
                        ValidateProbeContext(context, database);
                        await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                        bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                        if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported Quest reward provider.");
                        var parent = mysql ? (await context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={character} FOR UPDATE")
                            .AsNoTracking().ToListAsync()).SingleOrDefault() : await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == character);
                        if (parent == null || parent.DeleteTime != null) throw new InvalidOperationException("Quest reward character no longer exists.");
                        var dates = new Dictionary<DateTime, DateTime>();
                        CharacterChallengeQuestState Encode(CharacterChallengeQuestState row)
                        {
                            if (!mysql || row?.Reset is not { } instant) return row;
                            if (!dates.TryGetValue(instant, out var stored))
                            {
                                stored = context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", instant).AsEnumerable().Single(); dates.Add(instant, stored);
                            }
                            return row with { Reset = stored };
                        }
                        var changes = new List<CharacterChallengeQuestChange>();
                        foreach (var input in inputs)
                        {
                            var root = CharacterChallengeQuestState.FromModel(await context.CharacterQuest.AsNoTracking()
                                .SingleOrDefaultAsync(row => row.Id == character && row.QuestId == input.Id));
                            var leaves = (await context.CharacterQuestObjective.AsNoTracking().Where(row => row.Id == character && row.QuestId == input.Id)
                                .OrderBy(row => row.Index).ToArrayAsync()).Select(CharacterChallengeQuestObjectiveState.FromModel).ToArray();
                            if (!input.Roots.Select(Encode).Contains(root) || root == null && leaves.Length != 0)
                                throw new InvalidOperationException("Quest SQL root changed outside its captured baseline candidates.");
                            if (root != null)
                            {
                                if (leaves.Any(row => !input.Objectives.ContainsKey(row.Index))) throw new InvalidOperationException("Quest SQL has an uncaptured objective.");
                                var actual = leaves.ToDictionary(row => row.Index);
                                foreach (var row in input.Objectives)
                                    if (!row.Value.Contains(actual.GetValueOrDefault(row.Key))) throw new InvalidOperationException("Quest SQL objective changed outside its baseline candidates.");
                            }
                            var previous = root == null ? null : new CharacterChallengeQuestSnapshot(root, leaves);
                            var target = input.Target == null ? null : new CharacterChallengeQuestSnapshot(Encode(input.Target.Quest), input.Target.Objectives);
                            if (previous != null || target != null) changes.Add(new(previous, target));
                        }
                        await unit.CommitAsync();
                        var result = new Preparation(token, Array.AsReadOnly(changes.ToArray()));
                        preparations.Add(result, token); return result;
                    });
                });

            private static void ValidateProbeContext(CharacterContext context, string database)
            {
                if (context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any() || context.IsSaveRejected
                    || context.HasSavepointRollback || Item.GetRewardDatabaseKey(context) != database)
                    throw new InvalidOperationException("Quest probe requires its fresh retained database context.");
            }

            internal Task<CharacterChallengeRewardExecution> ExecuteAsync(CharacterChallengeRewardExecutor executor,
                Preparation preparation, CharacterChallengeRewardCommand reward,
                ChallengeManager.RewardPersistenceLease shared = null, object queue = null)
            {
                ArgumentNullException.ThrowIfNull(executor); ArgumentNullException.ThrowIfNull(preparation); ArgumentNullException.ThrowIfNull(reward);
                lock (owner.questRewardGate)
                {
                    Head(); CheckQueue(queue);
                    if (phase != Phase.Preparing || !preparations.TryGetValue(preparation, out var token)
                        || !ReferenceEquals(preparationToken, token) || reward.CharacterId != owner.player.CharacterId)
                        throw new InvalidOperationException("Quest preparation belongs to another capture/character.");
                    if (shared != null && !shared.CanShareQuestOwner(owner.player, database))
                        throw new InvalidOperationException("Quest capture cannot share another player, database or item delivery.");
                    prepared = preparation; command = reward.WithQuestChanges(preparation.Changes); phase = Phase.Executing;
                    companion = shared;
                    try { executing = shared == null ? executor.ExecuteOwnedAsync(command, database) : shared.ExecuteAsync(executor, command); }
                    catch { phase = Phase.Preparing; companion = null; command = null; throw; }
                    Watch(executing); return executing;
                }
            }

            internal Task<CharacterChallengeRewardExecution> ResumeAsync(CharacterChallengeRewardExecutor executor, CharacterChallengeRewardExecution previous,
                ChallengeManager.RewardPersistenceLease shared = null, object queue = null)
            {
                lock (owner.questRewardGate)
                {
                    Head(); CheckQueue(queue);
                    if (phase != Phase.Executing || !Owns(previous) || !ReferenceEquals(companion, shared))
                        throw new InvalidOperationException("Quest recovery requires its exact retained result and companion.");
                    executing = shared == null ? executor.ResumeAsync(command, previous) : shared.ResumeAsync(executor, previous);
                    Watch(executing); return executing;
                }
            }

            private void Watch(Task<CharacterChallengeRewardExecution> operation)
            {
                _ = operation.ContinueWith(task =>
                {
                    lock (owner.questRewardGate)
                        if (finalized && ReferenceEquals(executing, operation) && task.IsCompletedSuccessfully) AbandonAfterTerminal(task.Result);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            private bool Owns(CharacterChallengeRewardExecution result)
                => phase == Phase.Executing && CharacterChallengeRewardExecutor.IsCompletionOf(result, command, database, null);

            internal bool AdoptCommitted(CharacterChallengeRewardExecution result) => AdoptCommitted(result, null, null);

            internal bool AdoptCommitted(CharacterChallengeRewardExecution result, ChallengeManager.RewardPersistenceLease shared, Action adoptCompanion)
            {
                lock (owner.questRewardGate)
                {
                    if (invalidated || owner.questRewardReload || !ReferenceEquals(companion, shared)
                        || !Owns(result) || result.Outcome != CharacterChallengeRewardExecutionOutcome.Committed || !result.OriginalUnitEnded) return false;
                    try
                    {
                        if (!result.QuestStates.Keys.Order().SequenceEqual(prepared.Changes.Select(c => c.QuestId).Order())
                            || prepared.Changes.Any(c => !CharacterChallengeQuestSnapshot.Same(c.After, result.QuestStates[c.QuestId])))
                            throw new InvalidOperationException("Quest result does not match every frozen participant.");
                        var current = owner.RewardCensus();
                        // Preflight complete before mutating any persistence owner.
                        foreach (var change in prepared.Changes)
                        {
                            var selected = after.GetValueOrDefault(change.QuestId) ?? before[change.QuestId];
                            if (change.After != null && !selected.Objectives.Keys.Order().SequenceEqual(change.After.Objectives.Select(row => row.Index).Order()))
                                throw new InvalidOperationException("Quest result lost an owned objective.");
                        }
                        // The Challenge lease already preflighted its merge. Its
                        // acknowledgement runs only after every Quest preflight,
                        // while both persistence owners remain retained.
                        adoptCompanion?.Invoke();
                        foreach (var change in prepared.Changes)
                        {
                            ushort id = change.QuestId; var actual = result.QuestStates[id]; var selected = after.GetValueOrDefault(id) ?? before[id];
                            var aliases = owner.questRewards.SelectMany(c => new[] { c.before.GetValueOrDefault(id), c.after.GetValueOrDefault(id) })
                                .Append(current.GetValueOrDefault(id)).Where(g => g != null).DistinctBy(g => g.Owner).ToArray();
                            foreach (var alias in aliases.Where(g => !ReferenceEquals(g.Owner, selected.Owner))) alias.Rebase(actual);
                            selected.Adopt(actual);
                            foreach (var alias in aliases) alias.Retire(!current.TryGetValue(id, out var active) || !ReferenceEquals(active.Owner, alias.Owner));
                            if (actual == null && selected.Owner.PendingDelete)
                            {
                                selected.Retire();
                                lock (owner.deletionGate) owner.committedDeletions.Add(selected.Owner);
                            }
                        }
                        End(); owner.DrainCommittedDeletions(); return true;
                    }
                    catch { owner.questRewardReload = true; End(); return false; }
                }
            }

            internal bool AbandonAfterTerminal(CharacterChallengeRewardExecution result)
            {
                lock (owner.questRewardGate)
                {
                    if (!Owns(result)) return false;
                    owner.questRewardReload = true;
                    if (!result.OriginalUnitEnded) return false;
                    End(); return true;
                }
            }
            internal void Invalidate(bool final)
            {
                lock (owner.questRewardGate)
                {
                    invalidated = true; finalized |= final; owner.questRewardReload = true;
                    if (phase != Phase.Executing) End();
                    else if (finalized && executing?.IsCompletedSuccessfully == true) AbandonAfterTerminal(executing.Result);
                }
            }
            private void End()
            {
                if (owner.questRewardReload)
                    foreach (var graph in before.Values.Concat(after.Values)) graph.Retire();
                phase = Phase.Ended; owner.questRewards.Remove(this); scope?.Dispose(); scope = null;
            }
            public void Dispose()
            {
                lock (owner.questRewardGate)
                {
                    if (phase == Phase.Ended) return;
                    invalidated = true;
                    owner.questRewardReload = true;
                    if (phase != Phase.Executing) End(); // unknown writes remain pinned
                }
            }
        }
    }
}
