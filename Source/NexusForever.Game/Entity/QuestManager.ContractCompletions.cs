using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Quest;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private ContractCompletionTicket contractCompletion;
        private ContractRewardCatalog contractRewardCatalog;
        private readonly Queue<DeferredContractCompletion> deferredContractCompletions = [];
        internal bool HasContractCompletionWork => contractCompletion != null;
        internal bool IsContractCompletionMutationInProgress => contractCompletion is { Ready: false };
        private sealed record DeferredContractCompletion(ushort Id, IQuest Quest, string Acceptance, ushort Reward, bool Communicator);
        internal sealed class ContractCompletionTicket(Quest.Quest quest, ContractRewardSource source, ContractRewardModifierSnapshot modifier,
            ushort choice, uint[] prerequisites)
        {
            internal readonly Quest.Quest Quest = quest;
            internal readonly string Acceptance = quest.ContractAcceptanceId;
            internal readonly ContractRewardSource Source = source with { };
            internal readonly ContractRewardModifierSnapshot Modifier = modifier;
            internal readonly ushort Choice = choice;
            internal readonly uint[] Prerequisites = prerequisites;
            internal readonly DateTime CreatedUtc = DateTime.UtcNow;
            internal readonly object Token = new();
            internal ContractRewardSelection Selection;
            internal ContractTrackState SelectionBaseline;
            internal string Database;
            internal IDisposable Scope;
            internal bool Ready;
        }

        private bool ContractCompletionBusy(Player actor) => contractCompletion != null || normalSave != null || actor.IsNormalSaveInProgress
            || questRewardCaptureDepth != 0 || questRewards.Count != 0 || player.ChallengeManager?.HasPendingRewardOperation == true
            || player.Inventory?.SupportsTransactionalSoulboundDelivery == true && player.Inventory.IsSaveBlocked;

        // Defer before reclaiming items or running ordinary rewards/scripts. A
        // retained request can never complete a replacement accepted generation.
        private bool TryDeferContractCompletion(IQuestInfo info, ushort reward, bool communicator)
        {
            if (!info.IsContract() || player is not Player actor || !ReferenceEquals(actor.QuestManager, this)) return false;
            if (questRewardReload || questRewardInvalidated || player.ChallengeManager?.RequiresAuthoritativeReload == true)
                throw new InvalidOperationException("Contract completion requires authoritative reload.");
            if (!ContractCompletionBusy(actor) && deferredContractCompletions.Count == 0) return false;
            ushort id = (ushort)info.Entry.Id;
            var quest = GetQuest(id);
            string acceptance = (quest as Quest.Quest)?.ContractAcceptanceId;
            if (deferredContractCompletions.Any(pending => pending.Id == id && ReferenceEquals(pending.Quest, quest) && pending.Acceptance == acceptance)) return true;
            var request = new DeferredContractCompletion(id, quest, acceptance, reward, communicator);
            deferredContractCompletions.Enqueue(request);
            player.Session.Events.EnqueueEvent(new PredicateEvent(
                () => questRewardReload || questRewardInvalidated || player.ChallengeManager?.RequiresAuthoritativeReload == true
                    || deferredContractCompletions.TryPeek(out var head) && ReferenceEquals(head, request) && !ContractCompletionBusy(actor),
                () =>
                {
                    if (questRewardReload || questRewardInvalidated || player.ChallengeManager?.RequiresAuthoritativeReload == true)
                    { deferredContractCompletions.Clear(); return; }
                    if (!ReferenceEquals(deferredContractCompletions.Dequeue(), request)) throw new InvalidOperationException("Contract completion queue lost its head.");
                    var current = GetQuest(id);
                    if (!ReferenceEquals(current, request.Quest) || (current as Quest.Quest)?.ContractAcceptanceId != request.Acceptance
                        || current != null && current.State != QuestState.Achieved) return;
                    QuestComplete(id, request.Reward, request.Communicator, admitted: true);
                }));
            return true;
        }

        private ContractCompletionTicket BeginContractCompletion(IQuest quest, ushort choice)
        {
            if (player is not Player actor || !ReferenceEquals(actor.QuestManager, this) || quest is not Quest.Quest concrete
                || !Quest.Quest.ValidContractAcceptanceId(concrete.ContractAcceptanceId)
                || player.ChallengeManager is not ChallengeManager { NormalSaveIdentity: not null }
                || contractManager?.TryGetRewardSource(quest.Info, out var source) != true
                || player.Account?.RewardPropertyManager?.CaptureContractModifier(player, source.TrackId, source.TrackType, out var modifier) != true
                || !modifier.TryGetPoints(source.BasePoints, out _)) return null;
            if (ContractCompletionBusy(actor)) throw new InvalidOperationException("Contract completion lost its exclusive actor window.");
            uint[] prerequisites = source.TrackPrerequisiteId == 0 ? []
                : GetPrerequisiteManager().Meets(player, source.TrackPrerequisiteId) ? [source.TrackPrerequisiteId]
                : throw new InvalidOperationException("Contract completion track prerequisite is not satisfied.");
            contractRewardCatalog ??= new(gameTableManager);
            var track = contractRewardCatalog.GetTrack(source.TrackId);
            if (track == null || track.Type != source.TrackType || track.PrerequisiteId != source.TrackPrerequisiteId)
                throw new InvalidOperationException("Contract completion catalog changed its owner.");
            var pending = new ContractCompletionTicket(concrete, source, modifier, choice, prerequisites)
            { Database = ((ChallengeManager)player.ChallengeManager).NormalSaveIdentity.ExecutionDatabase };
            pending.Scope = QuestSaveScope.AcquireFrozen(this, pending.Token, pending.Database, player.CharacterId);
            try { ((ChallengeManager)player.ChallengeManager).BeginContractCompanion(pending.Token); }
            catch { pending.Scope.Dispose(); throw; }
            contractCompletion = pending;
            try
            {
                if (prerequisiteManager != null && committedContractTracks != null)
                {
                    var baseline = committedContractTracks.SingleOrDefault(row => row.TrackId == source.TrackId);
                    if (baseline?.SelectionInitialized != true)
                    {
                        var satisfied = track.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId)
                            .Append(track.PrerequisiteId).Where(id => id != 0).Distinct()
                            .Where(id => GetPrerequisiteManager().Meets(player, id)).ToHashSet();
                        // Missing eligibility must not consume ordinary rewards or
                        // invent a class/rank fallback. A later eligible completion
                        // can initialize this still-unselected cycle.
                        if (track.Slots.All(slot => contractRewardCatalog.EligibleCandidates(track.Id, slot.Ordinal, satisfied).Count != 0))
                        {
                            pending.Selection = ContractRewardSelection.Capture(contractRewardCatalog, track.Id, baseline?.CycleId ?? 0, pending.Acceptance, satisfied);
                            pending.SelectionBaseline = baseline;
                        }
                    }
                }
                return pending;
            }
            catch { EndContractCompletion(pending, false); throw; }
        }

        private void EndContractCompletion(ContractCompletionTicket ticket, bool completed)
        {
            if (ticket == null) return; // historical NULL/custom/unsupported owners retain ordinary rewards
            if (!completed || !ReferenceEquals(ticket, contractCompletion) || questRewardReload
                || player.ChallengeManager?.RequiresAuthoritativeReload == true)
            {
                ((ChallengeManager)player.ChallengeManager).AbortContractCompanion(ticket.Token);
                RequireNormalSaveReload(); player.Session?.ForceDisconnect();
                if (completed) throw new InvalidOperationException("Contract ordinary rewards require authoritative reload.");
                return;
            }
            ticket.Ready = true;
            // Item/reward continuations still run on Events while normal client
            // requests wait, including if inventory temporarily delays the save.
            player.Session.CanProcessIncomingPackets = false;
            ((Player)player).Save();
        }
    }
}
