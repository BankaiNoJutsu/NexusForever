using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest.Reward;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private CharacterRewardQueue rewardQueue;

        /// <summary>
        /// One actor's ordered command stream. Only identity/context work and the
        /// immutable executor run on workers. Session events own live adoption.
        /// </summary>
        internal sealed class CharacterRewardQueue
        {
            private readonly object gate = new();
            private readonly ChallengeManager owner;
            private readonly Func<CharacterContext> contexts;
            private readonly CharacterChallengeRewardExecutor executor;
            private readonly EventQueue events;
            private readonly Action<CharacterChallengeRewardExecution> committed;
            private readonly Action failed;
            private readonly TimeSpan retryDelay;
            private readonly int maxRecoveries;
            private readonly Queue<Request> pending = new();
            private Request current;
            private RewardPersistenceIdentity identity;
            internal RewardPersistenceIdentity CapturedIdentity { get { lock (gate) return identity; } }
            private RewardPersistenceLease lease;
            private CharacterChallengeRewardCommand command;
            private Task<CharacterChallengeRewardExecution> executing;
            private QuestManager.QuestRewardCapture.Preparation questPreparation;
            private bool preparingIdentity, stopped, invalidated, finalized, probing;
            private int recoveries;

            private sealed record Request(RewardIntent[] Intents, Func<RewardPersistenceLease, CharacterChallengeRewardCommand> Build,
                IInventoryRewardReservation Delivery, IDisposable Ticket, QuestManager.QuestRewardCapture Quests, QuestManager.ContractClaimOperation Contract,
                ContractAccountSync Accounts = null, Action AfterCommit = null);

            internal CharacterRewardQueue(ChallengeManager owner, Func<CharacterContext> contexts,
                CharacterChallengeRewardExecutor executor, EventQueue events,
                Action<CharacterChallengeRewardExecution> committed, Action failed,
                TimeSpan? retryDelay = null, int maxRecoveries = 3)
            {
                this.owner = owner; this.contexts = contexts; this.executor = executor;
                this.events = events ?? throw new ArgumentNullException(nameof(events));
                this.committed = committed; this.failed = failed;
                this.retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
                this.maxRecoveries = maxRecoveries;
                if (this.retryDelay < TimeSpan.Zero || maxRecoveries < 0) throw new ArgumentOutOfRangeException(nameof(retryDelay));
                if (owner.rewardQueue != null) throw new InvalidOperationException("Challenge owner already has a reward queue.");
                owner.rewardQueue = this;
            }

            internal bool Enqueue(IReadOnlyCollection<RewardIntent> intents,
                Func<RewardPersistenceLease, CharacterChallengeRewardCommand> build, IInventoryRewardReservation delivery = null,
                QuestManager.QuestRewardCapture quests = null, QuestManager.ContractClaimOperation contract = null,
                Action afterCommit = null)
            {
                ArgumentNullException.ThrowIfNull(intents); ArgumentNullException.ThrowIfNull(build);
                lock (gate)
                {
                    if (invalidated || stopped || owner.RequiresAuthoritativeReload) return false;
                    if (quests != null && delivery != null) return false;
                    if (quests != null && contract != null) return false;
                    if (delivery != null && owner.player?.Inventory?.OwnsRewardReservation(delivery) != true) return false;
                    var copy = intents.ToArray();
                    if (copy.Any(intent => !ReferenceEquals(intent.Owner, owner))) throw new InvalidOperationException("Foreign queued reward intent.");
                    var ticket = owner.HoldRewardQueue();
                    if (quests != null && !quests.TryQueue(this, owner.player)) { ticket.Dispose(); return false; }
                    if (contract != null && !contract.TryQueue(this, owner.player)) { ticket.Dispose(); return false; }
                    pending.Enqueue(new(copy, build, delivery, ticket, quests, contract, AfterCommit: afterCommit));
                    Pump(); return true;
                }
            }

            internal bool EnqueueAccounts(ContractAccountSync operation)
            {
                ArgumentNullException.ThrowIfNull(operation);
                lock (gate)
                {
                    if (invalidated || stopped || owner.RequiresAuthoritativeReload) return false;
                    var ticket = owner.HoldRewardQueue();
                    pending.Enqueue(new([], _ => operation.LoadCommand(), null, ticket, null, null, operation));
                    Pump(); return true;
                }
            }

            private void Pump()
            {
                if (stopped || invalidated || current != null || preparingIdentity) return;
                if (pending.Count == 0) return;
                if (identity == null)
                {
                    preparingIdentity = true;
                    var prepared = Task.Run(() =>
                    {
                        using var context = contexts() ?? throw new InvalidOperationException("Reward context factory returned null.");
                        return CaptureRewardPersistenceIdentity(context);
                    });
                    events.EnqueueEvent(new TaskGenericEvent<RewardPersistenceIdentity>(prepared, value =>
                    {
                        lock (gate) { preparingIdentity = false; identity = value; Pump(); }
                    }, () => { lock (gate) { preparingIdentity = false; Stop(); } }));
                    return;
                }
                current = pending.Dequeue(); recoveries = 0;
                TryStart();
            }

            private void TryStart()
            {
                if (stopped || invalidated) return;
                try
                {
                    if (!owner.TryAcquireRewardPersistence(identity, current.Intents, out lease, current.Delivery))
                    {
                        if (owner.RequiresAuthoritativeReload || owner.player?.Inventory?.RequiresAuthoritativeReload == true
                            || owner.player?.Inventory?.SupportsTransactionalSoulboundDelivery != true) { Stop(); return; }
                        // A normal save may already own the inventory/Challenge
                        // unit. Keep the ticket across the wait and try later.
                        events.EnqueueEvent(new DelayEvent(retryDelay > TimeSpan.Zero ? retryDelay : TimeSpan.FromMilliseconds(1), () => { lock (gate) TryStart(); }));
                        return;
                    }
                    if (current.Quests != null && current.Quests.Database != identity.ExecutionDatabase)
                        throw new InvalidOperationException("Quest capture changed its retained execution database.");
                    if (current.Accounts != null)
                    {
                        var request = current; probing = false;
                        request.Accounts.Start(identity.ExecutionDatabase, events, () =>
                        {
                            lock (gate)
                            {
                                if (stopped || invalidated || !ReferenceEquals(current, request)) return;
                                try
                                {
                                    if (request.Accounts.SkipLoad)
                                    { owner.CompleteContractAccounts(request.Accounts); ReleaseCurrent(); Pump(); }
                                    else PrepareCommand();
                                }
                                catch { Stop(); }
                            }
                        }, () => { lock (gate) if (ReferenceEquals(current, request)) Stop(); });
                        return;
                    }
                    probing = lease.NeedsLoadProbe;
                    if (probing) { command = CharacterChallengeRewardCommand.Load(owner.characterId); StartExecution(); }
                    else PrepareCommand();
                }
                catch { Stop(); }
            }

            private void PrepareCommand()
            {
                command = current.Build(lease);
                current.Contract?.Begin(this, command, identity.ExecutionDatabase, current.Delivery);
                if (current.Quests == null) { StartExecution(); return; }
                var request = current;
                var prepared = request.Quests.PrepareAsync(contexts, this);
                events.EnqueueEvent(new TaskGenericEvent<QuestManager.QuestRewardCapture.Preparation>(prepared,
                    value =>
                    {
                        lock (gate)
                        {
                            if (stopped || invalidated || !ReferenceEquals(current, request)) return;
                            try { questPreparation = value; StartExecution(); } catch { Stop(); }
                        }
                    }, () => { lock (gate) if (ReferenceEquals(current, request)) Stop(); }));
            }

            private void StartExecution(CharacterChallengeRewardExecution previous = null)
            {
                executing = !probing && current.Quests != null
                    ? previous == null ? current.Quests.ExecuteAsync(executor, questPreparation, command, lease, this)
                        : current.Quests.ResumeAsync(executor, previous, lease, this)
                    : previous == null ? lease.ExecuteAsync(executor, command) : lease.ResumeAsync(executor, previous);
                var operation = executing;
                var held = lease;
                // Session events may no longer be polled after final disposal.
                // This tail only abandons dead-owner bookkeeping; it never
                // adopts runtime state or publishes client/inventory messages.
                _ = operation.ContinueWith(task =>
                {
                    lock (gate)
                        if (finalized && ReferenceEquals(executing, operation) && task.Status == TaskStatus.RanToCompletion)
                            FinishDeadOwner(task.Result, held);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                events.EnqueueEvent(new TaskGenericEvent<CharacterChallengeRewardExecution>(operation,
                    result => { lock (gate) if (ReferenceEquals(executing, operation)) Handle(result); },
                    () => { lock (gate) if (ReferenceEquals(executing, operation)) Stop(); }));
            }

            private void Handle(CharacterChallengeRewardExecution result)
            {
                if (finalized) { FinishDeadOwner(result, lease); return; }
                if (invalidated && (current.Quests != null || current.Contract != null || current.Accounts != null)) { Abandon(result); return; }
                if (result.Outcome == CharacterChallengeRewardExecutionOutcome.Uncertain)
                {
                    if (!invalidated && result.OriginalUnitEnded && recoveries++ < maxRecoveries)
                    {
                        var previous = result;
                        events.EnqueueEvent(new DelayEvent(retryDelay, () =>
                        {
                            lock (gate)
                            {
                                if (stopped || invalidated) { Abandon(previous); return; }
                                try { StartExecution(previous); } catch { Abandon(previous); }
                            }
                        }));
                        return;
                    }
                    Abandon(result); return;
                }
                if (result.Outcome != CharacterChallengeRewardExecutionOutcome.Committed) { Abandon(result); return; }
                if (current.Accounts != null && !current.Accounts.CanAdopt(result)) { Abandon(result); return; }
                bool loaded = command.Kind == CharacterChallengeRewardCommandKind.Load;
                if (loaded ? !lease.CompleteLoad(result) : !lease.AdoptCommitted(result, current.Quests)) { Abandon(result); return; }
                if (probing && !invalidated)
                {
                    probing = false;
                    try { PrepareCommand(); } catch { Stop(); }
                    return;
                }
                if (loaded) lease.Dispose();
                try
                {
                    if (current.Contract != null && !current.Contract.Adopt(result)) throw new InvalidOperationException("Contract claim adoption was rejected.");
                    if (current.Delivery?.State == InventoryRewardReservationState.Committed && !current.Delivery.PublishCommitted())
                        throw new InvalidOperationException("Committed inventory publication was rejected.");
                    if (current.Contract != null && !current.Contract.PublishCurrency()) throw new InvalidOperationException("Contract currency publication was rejected.");
                    if (current.Accounts != null && !current.Accounts.CanPublish()) throw new InvalidOperationException("Contract account snapshot lost its live actor.");
                    if (!invalidated) committed?.Invoke(result);
                    if (current.Accounts != null && !current.Accounts.CanPublish()) throw new InvalidOperationException("Contract account publication lost its live actor.");
                    owner.CompleteContractAccounts(current.Accounts);
                    if (current.Contract != null && !current.Contract.Finish(result)) throw new InvalidOperationException("Contract claim publication lost its owner.");
                    if (!invalidated) current.AfterCommit?.Invoke();
                }
                catch { Stop(); return; }
                bool accountAward = current.Contract?.HasAccountAward == true;
                ReleaseCurrent();
                if (accountAward)
                {
                    try { owner.QueueContractAccounts(); } catch { Stop(); return; }
                }
                Pump();
            }

            private void Abandon(CharacterChallengeRewardExecution result)
            {
                current?.Quests?.AbandonAfterTerminal(result);
                current?.Contract?.Abandon(result);
                if (lease?.AbandonAfterTerminal(result) == true) lease = null;
                Stop();
            }

            private void FinishDeadOwner(CharacterChallengeRewardExecution result, RewardPersistenceLease held)
            {
                if (!held.AbandonAfterTerminal(result)) return; // unknown original unit remains pinned
                current?.Quests?.AbandonAfterTerminal(result);
                current?.Contract?.Abandon(result);
                lease = null; executing = null; ReleaseCurrent();
            }

            private void ReleaseCurrent()
            {
                lease?.Dispose(); lease = null; executing = null; command = null; questPreparation = null;
                current?.Quests?.Dispose();
                owner.ReleaseContractAccounts(current?.Accounts);
                current?.Delivery?.Dispose(); current?.Contract?.Dispose(); current?.Ticket.Dispose(); current = null;
            }

            private void Stop()
            {
                bool notify = !stopped && !invalidated;
                stopped = true; owner.RequireRewardReload();
                current?.Accounts?.Dispose();
                current?.Contract?.Invalidate();
                current?.Delivery?.InvalidateOwner();
                CancelPending();
                if (lease?.IsExecuting != true) ReleaseCurrent();
                if (notify) failed?.Invoke();
            }

            private void CancelPending()
            {
                while (pending.TryDequeue(out var request))
                {
                    try { request.Quests?.Dispose(); request.Contract?.Invalidate(); request.Contract?.Dispose(); owner.ReleaseContractAccounts(request.Accounts); request.Delivery?.InvalidateOwner(); request.Delivery?.Dispose(); }
                    finally { request.Ticket.Dispose(); }
                }
            }

            internal void Invalidate(bool final)
            {
                lock (gate)
                {
                    invalidated = true; finalized |= final;
                    current?.Quests?.Invalidate(final);
                    current?.Contract?.Invalidate();
                    current?.Accounts?.Dispose();
                    current?.Delivery?.InvalidateOwner();
                    if (pending.Count != 0) owner.RequireRewardReload();
                    CancelPending();
                    if (lease?.IsExecuting != true) ReleaseCurrent();
                    else if (finalized && executing?.IsCompletedSuccessfully == true) FinishDeadOwner(executing.Result, lease);
                }
            }
        }
    }
}
