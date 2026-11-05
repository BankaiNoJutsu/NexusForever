using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private ContractClaimOperation contractClaimOperation;

        // Internal until account draining/relog and retail reset are connected.
        // Production07CE must never expose only a subset of canonical choices.
        internal bool QueueContractRewardClaim(uint rewardRow, uint choice)
        {
            if (!CanChooseContractTrack() || player.CurrencyManager is not CurrencyManager currencies
                || currencies.HasPendingContractCurrency || currencies.RequiresContractCurrencyReload) return false;
            var before = committedContractTracks; var catalog = new ContractRewardAwardCatalog(contractRewardCatalog);
            var award = catalog.GetAward(rewardRow, choice);
            var track = award == null ? null : before.SingleOrDefault(row => row.TrackId == award.TrackId);
            if (track == null || !track.Active || !track.SelectionInitialized || track.Points < award.RequiredPoints
                || track.Slots[award.Ordinal].RewardRowId != rewardRow || track.Slots[award.Ordinal].Claimed) return false;
            var definition = catalog.Catalog.GetTrack(track.TrackId);
            var reward = definition.Slots[award.Ordinal].Candidates.Single(row => row.Id == rewardRow);
            var satisfied = new HashSet<uint>();
            foreach (uint prerequisite in new[] { definition.PrerequisiteId, reward.PrerequisiteId }.Where(id => id != 0).Distinct())
                if (prerequisiteManager?.Meets(player, prerequisite) == true) satisfied.Add(prerequisite); else return false;
            if (!ReferenceEquals(before, committedContractTracks) || !CanChooseContractTrack()) return false;
            var quote = ContractRewardClaimQuote.Capture(catalog, player.CharacterId, before, track.TrackId, track.CycleId, track.Revision, rewardRow, choice);
            var challenges = (ChallengeManager)player.ChallengeManager; string database = challenges.NormalSaveIdentity.ExecutionDatabase;
            if (!QuestSaveScope.TryAcquireReward(this, database, player.CharacterId, out var scope)) return false;
            ContractClaimOperation operation = null; IInventoryRewardReservation delivery = null;
            CurrencyManager.ContractCreditReservation credit = null; bool queued = false;
            try
            {
                operation = new(this, challenges, catalog, before, quote, database, scope, RewardCensus().Values.ToArray());
                scope = null; contractClaimOperation = operation;
                if (award.Item != null)
                {
                    var info = challenges.GetContractRewardItem(award.Item.ItemId);
                    if (info == null || !operation.IsPreparingOwner()) return false;
                    if (!player.Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, info, award.Item.Count,
                        out delivery, out var error, ItemUpdateReason.Challenge))
                    { player.SendGenericError(error); return false; }
                }
                string claim = Guid.NewGuid().ToString("N");
                if (award.CharacterCurrency != null && !currencies.TryReserveContractCredit(catalog, quote, claim, database, out credit)) return false;
                if (!operation.IsPreparingOwner()) return false;
                var command = CharacterChallengeRewardCommand.ClaimContract(player.CharacterId, player.Account.Id, player.Identity.RealmId,
                    claim, DateTime.UtcNow, catalog, before, quote, credit?.Before, satisfied);
                operation.Prepare(command, delivery, credit); credit = null;
                queued = challenges.QueueContractClaim(command, operation, delivery); return queued;
            }
            finally
            {
                scope?.Dispose();
                if (!queued) { delivery?.Dispose(); credit?.Cancel(); operation?.Dispose(); }
            }
        }

        internal sealed class ContractClaimOperation : IDisposable
        {
            private readonly QuestManager owner;
            private readonly ChallengeManager challenges;
            private readonly ContractRewardAwardCatalog catalog;
            private readonly IReadOnlyList<ContractTrackState> before;
            private readonly ContractRewardClaimQuote quote;
            private readonly string database;
            private readonly QuestRewardGraph[] census;
            private readonly IAccount account;
            private readonly uint accountId;
            private readonly IGameSession session;
            private readonly EventQueue events;
            private IDisposable scope;
            private CurrencyManager.ContractCreditReservation credit;
            private IInventoryRewardReservation delivery;
            private CharacterChallengeRewardCommand command;
            private object queue;
            private int phase; //0 preparing,1 prepared,2 queued,3 executing,4 adopted,5 completed,6 abandoned
            private bool invalidated, terminal;
            internal bool HasAccountAward => quote.Award.AccountCurrency != null;
            internal ContractClaimOperation(QuestManager owner, ChallengeManager challenges, ContractRewardAwardCatalog catalog,
                IReadOnlyList<ContractTrackState> before, ContractRewardClaimQuote quote, string database, IDisposable scope, QuestRewardGraph[] census)
            {
                this.owner = owner; this.challenges = challenges; this.catalog = catalog; this.before = before; this.quote = quote;
                this.database = database; this.scope = scope; this.census = census;
                account = owner.player.Account; accountId = account.Id; session = owner.player.Session;
                events = session.Events;
            }
            private bool Live(bool published = false) => owner.IsContractActorLive() && !invalidated && !owner.questRewardReload && !owner.questRewardInvalidated
                && ReferenceEquals(owner.contractClaimOperation, this) && ReferenceEquals(owner.player.QuestManager, owner)
                && ReferenceEquals(owner.player.ChallengeManager, challenges) && ReferenceEquals(owner.player.Account, account)
                && account.Id == accountId && ReferenceEquals(account.Session, session) && ReferenceEquals(owner.player.Session, session)
                && ReferenceEquals(session.Events, events) && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, owner.player))
                && owner.player.CharacterId == quote.CharacterId && (published || ReferenceEquals(owner.committedContractTracks, before));
            internal bool IsPreparingOwner() => phase == 0 && Live() && !challenges.HasPendingRewardOperation;
            internal void Prepare(CharacterChallengeRewardCommand command, IInventoryRewardReservation delivery, CurrencyManager.ContractCreditReservation credit)
            {
                if (!IsPreparingOwner() || command.ContractQuote.Json != quote.Json || command.ContractAccountId != accountId)
                    throw new InvalidOperationException("Contract claim preparation lost its actor snapshot.");
                this.command = command; this.delivery = delivery; this.credit = credit; phase = 1;
            }
            internal bool TryQueue(object queueOwner, IPlayer player, EventQueue queueEvents)
            {
                if (phase != 1 || queue != null || !ReferenceEquals(owner.player, player) || !ReferenceEquals(events, queueEvents) || !Live()) return false;
                queue = queueOwner; phase = 2; return true;
            }
            internal void Begin(object queueOwner, CharacterChallengeRewardCommand next, string executionDatabase, IInventoryRewardReservation items)
            {
                if (phase != 2 || !Live() || !ReferenceEquals(queue, queueOwner) || !ReferenceEquals(command, next)
                    || database != executionDatabase || !ReferenceEquals(items, delivery))
                    throw new InvalidOperationException("Contract claim changed its retained queue/execution owner.");
                credit?.Begin(command, delivery); phase = 3;
            }
            private bool Proof(CharacterChallengeRewardExecution result) => result?.OriginalUnitEnded == true
                && CharacterChallengeRewardExecutor.IsCompletionOf(result, command, database, delivery);
            internal bool Adopt(CharacterChallengeRewardExecution result)
            {
                terminal |= Proof(result);
                try
                {
                    if (phase != 3 || !Live() || !terminal || result.Outcome != CharacterChallengeRewardExecutionOutcome.Committed
                        || result.Operation is not ContractClaimStage stage || stage.Claim.ClaimId != command.Identity
                        || stage.Claim.AccountId != accountId || stage.Claim.QuoteJson != quote.Json)
                        throw new InvalidOperationException("Contract claim has no matching terminal aggregate proof.");
                    stage.Claim.Validate(catalog, quote.CharacterId, quote.Award.TrackId, quote.CycleId, quote.Award.Ordinal, quote.Award.RewardRowId, quote.SelectionJson);
                    var expected = before.Select(track => track.TrackId == quote.Award.TrackId ? track with
                    {
                        Revision = checked(track.Revision + 1), Slots = Array.AsReadOnly(track.Slots.Select(slot => slot.Ordinal == quote.Award.Ordinal
                            ? slot with { Claim = stage.Claim } : slot).ToArray())
                    } : track).ToArray();
                    var actual = ContractRewardSnapshot.Validate(result.State.ContractTracks, quote.CharacterId, catalog.Catalog);
                    if (actual.Count != expected.Length || !actual.Zip(expected).All(pair => ContractRewardSnapshot.Same(pair.First, pair.Second))
                        || credit != null && !credit.Adopt(result)) throw new InvalidOperationException("Contract claim result changed its complete frozen aggregate or currency.");
                    phase = 4; return true;
                }
                catch { Quarantine(); return false; }
            }
            internal bool CanPublish() => phase == 4 && Live();
            internal bool PublishCurrency() => CanPublish() && (credit?.Publish() ?? true) && CanPublish();
            internal bool Finish(CharacterChallengeRewardExecution result)
            {
                if (phase != 4 || !Live(published: true) || !Proof(result) || owner.committedContractTracks == null
                    || owner.committedContractTracks.Count != result.State.ContractTracks.Count
                    || !owner.committedContractTracks.Zip(result.State.ContractTracks).All(pair => ContractRewardSnapshot.Same(pair.First, pair.Second))) return false;
                phase = 5; Release(); return true;
            }
            internal void Abandon(CharacterChallengeRewardExecution result)
            {
                terminal |= Proof(result); Quarantine();
                if (phase < 3 || terminal) { phase = 6; Release(); }
            }
            internal void Invalidate() { invalidated = true; Quarantine(); if (phase < 3) { phase = 6; Release(); } }
            private void Quarantine()
            {
                lock (owner.questRewardGate)
                {
                    owner.questRewardReload = true; credit?.Quarantine();
                    foreach (var graph in census) graph.Retire();
                }
            }
            private void Release()
            {
                lock (owner.questRewardGate)
                {
                    if (ReferenceEquals(owner.contractClaimOperation, this)) owner.contractClaimOperation = null;
                    scope?.Dispose(); scope = null;
                }
            }
            public void Dispose()
            {
                if (phase is 5 or 6) return;
                if (phase < 3 && !invalidated)
                {
                    if (credit?.Cancel() == false) Quarantine();
                    phase = 6; Release();
                }
                else { Quarantine(); if (terminal || phase < 3) { phase = 6; Release(); } }
            }
        }
    }
}
