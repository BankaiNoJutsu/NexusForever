using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private bool IsContractActorLive() => player.Session != null && player.Session is not NetworkSession { IsDisconnecting: true }
            && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

        private bool CanChooseContractTrack() => IsContractActorLive() && player is Player actor && ReferenceEquals(actor.QuestManager, this)
            && committedContractTracks != null && !questRewardReload && !questRewardInvalidated && normalSave == null
            && !HasPendingRewardOperation && !HasContractCompletionWork && !actor.IsNormalSaveInProgress
            && player.RewardTrackPresentation?.IsPublishedContractSnapshot(committedContractTracks) == true
            && player.ChallengeManager is ChallengeManager challenges && challenges.CanQueueContractChoice;

        public bool ChooseContractRewardTrack(uint trackId)
        {
            if (!CanChooseContractTrack()) return false;
            var before = committedContractTracks; var state = before.SingleOrDefault(row => row.TrackId == trackId);
            var definition = contractRewardCatalog?.GetTrack(trackId);
            if (state == null || definition == null || state.Active && state.SelectionInitialized
                || definition.PrerequisiteId != 0 && prerequisiteManager?.Meets(player, definition.PrerequisiteId) != true) return false;
            string operation = Guid.NewGuid().ToString("N"); ContractRewardSelection selection = null;
            var satisfied = new HashSet<uint>();
            if (definition.PrerequisiteId != 0) satisfied.Add(definition.PrerequisiteId);
            if (!state.SelectionInitialized)
            {
                if (prerequisiteManager == null) return false;
                foreach (uint id in definition.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Where(id => id != 0).Distinct())
                    if (prerequisiteManager.Meets(player, id)) satisfied.Add(id);
                if (definition.Slots.Any(slot => contractRewardCatalog.EligibleCandidates(trackId, slot.Ordinal, satisfied).Count == 0)) return false;
                selection = ContractRewardSelection.CaptureForChoose(contractRewardCatalog, trackId, state.CycleId, operation, satisfied);
            }
            // Prerequisite adapters can reenter the actor. Do not queue a command
            // for an owner/snapshot that changed while evaluating eligibility.
            if (!ReferenceEquals(before, committedContractTracks) || !CanChooseContractTrack()) return false;
            var command = CharacterChallengeRewardCommand.ChooseContract(player.CharacterId, trackId, operation, contractRewardCatalog, before, selection, satisfied);
            return ((ChallengeManager)player.ChallengeManager).QueueContractChoice(command);
        }
    }
}
