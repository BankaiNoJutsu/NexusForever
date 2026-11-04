using NexusForever.Database.Character;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private NormalSaveSnapshot normalSave;
        internal void RequireNormalSaveReload()
        {
            lock (questRewardGate)
            {
                questRewardReload = true;
                contractCompletion?.Scope?.Dispose();
                if (contractCompletion != null) contractCompletion.Scope = null;
                foreach (var quest in completedQuests.Values.Concat(inactiveQuests.Values).Concat(activeQuests.Values).OfType<Quest.Quest>()) quest.RetireRewardOwner();
            }
        }
        internal NormalSaveSnapshot CaptureNormalSave(string database, object frozenOwner = null, object frozenToken = null)
        {
            lock (questRewardGate)
            {
                if (normalSave != null || questRewardReload || questRewards.Count != 0 || IsContractCompletionMutationInProgress)
                    throw new InvalidOperationException("Quest manager already has persistence ownership.");
                if (contractCompletion != null && (contractCompletion.Database != database || contractCompletion.Scope == null))
                    throw new InvalidOperationException("Contract journal lost its pending frozen owner.");
                bool borrowed = frozenOwner != null || frozenToken != null;
                if (borrowed && (frozenOwner == null || frozenToken == null || contractCompletion != null))
                    throw new InvalidOperationException("Borrowed reconciliation cannot take ownership of a Contract journal.");
                var token = frozenToken ?? contractCompletion?.Token ?? new object();
                var scope = borrowed ? QuestSaveScope.BorrowFrozen(frozenOwner, token, database, player.CharacterId)
                    : contractCompletion?.Scope ?? QuestSaveScope.AcquireFrozen(this, token, database, player.CharacterId);
                try
                {
                    DrainCommittedDeletions();
                    var graphs = completedQuests.Values.Concat(inactiveQuests.Values).Concat(activeQuests.Values).Distinct()
                        .Select(quest => quest is Quest.Quest concrete ? concrete.CaptureNormalSave(token)
                            : throw new InvalidOperationException("Frozen Quest save requires concrete owners.")).ToArray();
                    if (contractCompletion is { } ticket && !graphs.Any(graph => ReferenceEquals(graph.Owner, ticket.Quest)
                        && graph.Target?.Quest.State == (byte)QuestState.Completed && graph.Target.Quest.ContractAcceptanceId == ticket.Acceptance))
                        throw new InvalidOperationException("Contract journal lost its frozen terminal Quest generation.");
                    normalSave = new(this, database, token, graphs, scope, contractCompletion, CapturePublicEventPublications());
                    if (contractCompletion != null) contractCompletion.Scope = null; // ownership moves into this snapshot
                    return normalSave;
                }
                catch { scope.Dispose(); if (contractCompletion != null) RequireNormalSaveReload(); throw; }
            }
        }
        internal sealed class NormalSaveSnapshot(QuestManager owner, string database, object token, QuestNormalSaveGraph[] graphs, IDisposable scope,
            ContractCompletionTicket contract, Action<CharacterContext> stagePublicEventPublications) : IDisposable
        {
            internal object Token { get; } = token;
            private bool disposed;
            private CharacterContext bound;
            private ContractNormalSaveUnit contractUnit;
            private bool contractEnded;
            internal bool HasContractCompletion => contract != null;
            internal bool CanRetryBidRollback(CharacterContext context, bool partialEnrollment = false) => !disposed && ReferenceEquals(owner.normalSave, this)
                && (ReferenceEquals(bound, context) || partialEnrollment && bound == null)
                && (context == null || context.IsProvenBidRollback) && contract == null && !owner.questRewardReload;
            internal void RetryBidRollback(CharacterContext context, bool partialEnrollment = false)
            {
                if (!CanRetryBidRollback(context, partialEnrollment)) throw new InvalidOperationException("Frozen Quest save lost its proven bid rollback.");
                bound = null;
            }
            private void Bind(CharacterContext context)
            {
                if (disposed || !ReferenceEquals(owner.normalSave, this) || database != Item.GetRewardDatabaseKey(context)
                    || bound != null && !ReferenceEquals(bound, context)) throw new InvalidOperationException("Frozen Quest save lost its context/owner.");
                bound = context;
            }
            internal void BeginContractSave(CharacterContext context)
            {
                if (contract == null) return;
                try
                {
                    Bind(context);
                    if (contractUnit != null || context.AtomicCharacterId != owner.player.CharacterId)
                        throw new InvalidOperationException("Contract frozen save requires its single atomic Character enrollment.");
                    var participants = new QuestTransactionParticipants(context, owner.player.CharacterId, []);
                    var changes = graphs.Where(graph => graph.Needed || ReferenceEquals(graph.Owner, contract.Quest))
                        .Select(graph => new { Before = participants.ReadQuest(graph.Owner.Id), After = graph.Target })
                        .Where(change => change.Before != null || change.After != null)
                        .Select(change => new CharacterChallengeQuestChange(change.Before, change.After)).ToArray();
                    var command = ContractCompletionCommand.FromModifier(owner.player.CharacterId, contract.Acceptance, contract.Source,
                        contract.Modifier, contract.Choice, contract.CreatedUtc, changes, contract.Prerequisites);
                    if (contract.Selection != null) command = command.WithSelection(contract.Selection, contract.SelectionBaseline);
                    contractUnit = new(context, command, owner.contractRewardCatalog);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
            internal void EndContractSave(CharacterContext context)
            {
                if (contract == null) return;
                try { Bind(context); if (contractUnit == null) throw new InvalidOperationException("Contract frozen save did not begin."); contractUnit.Enroll(); }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
            internal bool FinishContract(bool success)
            {
                if (contract == null) return success;
                if (disposed || contractEnded) throw new InvalidOperationException("Contract frozen save already ended.");
                contractEnded = true;
                bool accepted = success && contractUnit?.TerminalCommitObserved == true && contractUnit.CandidateTracks != null
                    && ReferenceEquals(owner.contractCompletion, contract);
                if (accepted)
                {
                    try { owner.AdoptContractRewards(contractUnit.CandidateTracks); }
                    catch { accepted = false; }
                }
                if (accepted) owner.contractCompletion = null;
                else { owner.RequireNormalSaveReload(); contract.Quest.RetireRewardOwner(); }
                return accepted;
            }
            internal void Save(CharacterContext context)
            {
                lock (owner.questRewardGate)
                {
                    try
                    {
                        Bind(context);
                        if (contract != null && contractUnit == null) throw new InvalidOperationException("Contract frozen save requires atomic enrollment before ordinary Quest writes.");
                        foreach (var graph in graphs)
                        {
                            graph.Stage(context);
                            if (graph.Deleted) context.OnSaveSettled(committed =>
                            {
                                if (committed && graph.Owner.IsPersistenceDeleted)
                                    lock (owner.deletionGate) owner.committedDeletions.Add(graph.Owner);
                            });
                        }
                        stagePublicEventPublications?.Invoke(context);
                    }
                    catch (Exception error) { context.RejectSave(error); throw; }
                }
            }
            public void Dispose()
            {
                lock (owner.questRewardGate)
                {
                    if (disposed) return;
                    if (contract != null && !contractEnded) { owner.RequireNormalSaveReload(); contract.Quest.RetireRewardOwner(); }
                    disposed = true;
                    if (ReferenceEquals(owner.normalSave, this)) owner.normalSave = null;
                    scope.Dispose();
                }
            }
        }
    }
}
