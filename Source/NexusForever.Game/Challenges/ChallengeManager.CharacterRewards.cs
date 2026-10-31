using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private CharacterChallengeRewardCatalog characterRewardCatalog;
        private CharacterChallengeRewardsState committedCharacterRewards;
        private bool characterRewardsLoadRequested;
        private bool characterRewardLoadRequired;
        private bool characterRewardPacketsRequested;
        private Action characterRewardsReady;
        private readonly IPrerequisiteManager rewardPrerequisites;
        private readonly IItemManager rewardItems;

        internal CharacterChallengeRewardsState CommittedCharacterRewards => committedCharacterRewards;
        internal bool CharacterRewardsReady => !characterRewardLoadRequired || committedCharacterRewards != null
            && NormalSaveIdentity != null && !rewardOwnerInvalidated && !RequiresAuthoritativeReload;

        private void InitialiseCharacterRewards(Func<CharacterContext> contexts)
        {
            contractCharacterContexts = contexts;
            characterRewardLoadRequired = true;
            characterRewardCatalog = new(gameTableManager);
            var session = player.Session;
            rewardQueue = new(this, contexts, new CharacterChallengeRewardExecutor(contexts, characterRewardCatalog, gameTableManager),
                player.Session?.Events, AdoptCharacterRewards, () =>
                {
                    try { session?.ForceDisconnect(); } catch { }
                });
        }

        private void LoadCharacterRewards()
        {
            if (rewardQueue == null || characterRewardsLoadRequested) return;
            try
            {
                if (QueueContractAccounts()) { characterRewardsLoadRequested = true; return; }
            }
            catch { RequireRewardReload(); player.Session?.ForceDisconnect(); return; }
            characterRewardsLoadRequested = rewardQueue.Enqueue([], _ => CharacterChallengeRewardCommand.Load(characterId));
        }

        // Character selection calls this before map admission. No timer, kill,
        // zone/script or normal-save producer can run ahead of the durable load.
        public void PrepareForWorld(Action ready)
        {
            ArgumentNullException.ThrowIfNull(ready);
            if (rewardQueue == null) { ready(); return; }
            if (rewardOwnerInvalidated || RequiresAuthoritativeReload) return;
            player.RewardTrackPresentation?.ExpectCharacterTracks();
            if (player.QuestManager is Entity.QuestManager quests) quests.ExpectContractRewardTracks();
            var session = player.Session;
            var account = player.Account;
            ulong character = player.CharacterId;
            characterRewardsReady += () =>
            {
                if (!ReferenceEquals(player.Session, session) || !ReferenceEquals(player.Account, account)
                    || !ReferenceEquals(account?.Session, session) || player.CharacterId != character
                    || session is NetworkSession { IsDisconnecting: true }
                    || player.LogoutManager?.State is LogoutState.Logout or LogoutState.Finished)
                    return;
                ready();
            };
            if (committedCharacterRewards != null) PublishWorldReadiness();
            else LoadCharacterRewards();
        }

        private void PublishWorldReadiness()
        {
            var ready = characterRewardsReady; characterRewardsReady = null;
            if (ready == null) return;
            // Run after the current queue owner releases its inventory/SQL lease.
            player.Session.Events.EnqueueEvent(new PredicateEvent(() => !CharacterRewardsReady || !HasPendingRewardOperation, () =>
            {
                if (!CharacterRewardsReady) return;
                try { ready(); }
                catch { player.Session?.ForceDisconnect(); throw; }
            }));
        }

        private void AdoptCharacterRewards(CharacterChallengeRewardExecution result)
        {
            committedCharacterRewards = result.State;
            if (player.QuestManager is Entity.QuestManager quests) quests.AdoptContractRewards(result.State.ContractTracks);
            PublishWorldReadiness();
            if (!characterRewardPacketsRequested) return;
            PublishCharacterRewards();
        }

        private void PublishCharacterRewards(bool reload = true)
        {
            // Only durable cycles have sampled slot identities. Never invent a
            // selection during presentation or resample one on every reload.
            var rows = committedCharacterRewards.Tracks.Select(track =>
            {
                var definition = characterRewardCatalog.GetTrack(track.TrackId)
                    ?? throw new InvalidOperationException("Committed Character track has no catalog.");
                var claimed = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
                foreach (var slot in track.Slots.Where(slot => slot.Claimed)) claimed.SetBit(slot.Ordinal, true);
                return new RewardTrack
                {
                    RewardTrackId = track.TrackId, RewardPointsEarned = track.Points,
                    ClaimedRewards = claimed, Unlocked = track.Points >= definition.MaximumPoints ? 1u : 0u,
                    RewardTrackRewardsIds = track.Slots.OrderBy(slot => slot.Ordinal).Select(slot => slot.RewardRowId).ToList(),
                    IsActive = track.Active
                };
            }).ToArray();
            player.RewardTrackPresentation?.PublishCharacterTracks(rows, committedCharacterRewards, reload);
        }

        private bool CanRequestCharacterReward() => contractCompanion == null && rewardQueue != null && committedCharacterRewards != null
            && !HasPendingRewardOperation && !RequiresAuthoritativeReload
            && player.Inventory?.SupportsTransactionalSoulboundDelivery == true && !player.Inventory.IsSaveBlocked
            && player.RewardTrackPresentation?.IsPublishedCharacterSnapshot(committedCharacterRewards) == true;

        private uint[] CaptureRewardPrerequisites() => characterRewardCatalog.Tracks.Select(track => track.PrerequisiteId)
            .Where(id => id != 0).Distinct().Where(id => rewardPrerequisites?.Meets(player, id) == true).ToArray();

        public bool ChooseRewardTrack(uint trackId)
        {
            if (!CanRequestCharacterReward() || !characterRewardCatalog.TryGetTrack(trackId, out var definition)) return false;
            uint[] satisfied = CaptureRewardPrerequisites();
            if (definition.PrerequisiteId != 0 && !satisfied.Contains(definition.PrerequisiteId)) return false;
            var versions = committedCharacterRewards.Tracks.ToDictionary(track => track.TrackId, track => track.Revision);
            var command = CharacterChallengeRewardCommand.Choose(characterId, trackId, versions, satisfied);
            return rewardQueue.Enqueue([], _ => command);
        }

        internal bool CanQueueContractChoice => CanRequestCharacterReward();
        internal IItemInfo GetContractRewardItem(uint id) => rewardItems?.GetItemInfo(id);

        internal bool QueueContractClaim(CharacterChallengeRewardCommand command, Entity.QuestManager.ContractClaimOperation claim,
            IInventoryRewardReservation delivery)
            => claim != null && command?.Kind == CharacterChallengeRewardCommandKind.ClaimContract && command.CharacterId == characterId
                && rewardQueue != null && committedCharacterRewards != null && !rewardOwnerInvalidated && !RequiresAuthoritativeReload
                && rewardQueue.Enqueue([], _ => command, delivery, contract: claim);

        internal bool QueueContractChoice(CharacterChallengeRewardCommand command)
            => command?.Kind == CharacterChallengeRewardCommandKind.ChooseContract && command.CharacterId == characterId
                && CanRequestCharacterReward() && rewardQueue.Enqueue([], _ => command);

        public bool ClaimRewardTrack(uint rewardRowId, uint choice)
        {
            if (!CanRequestCharacterReward() || !characterRewardCatalog.TryGetReward(rewardRowId, out var reference)) return false;
            var track = committedCharacterRewards.Tracks.SingleOrDefault(track => track.TrackId == reference.Track.Id);
            var slot = track?.Slots.SingleOrDefault(slot => slot.Ordinal == reference.Slot.Position);
            var award = reference.Reward.GetChoice(choice);
            if (track == null || slot == null || slot.RewardRowId != rewardRowId || slot.Claimed
                || track.Points < slot.RequiredPoints || award == null) return false;
            uint[] satisfied = CaptureRewardPrerequisites();
            if (reference.Track.PrerequisiteId != 0 && !satisfied.Contains(reference.Track.PrerequisiteId)) return false;
            var info = rewardItems?.GetItemInfo(award.ItemId);
            if (info == null) return false;
            if (!player.Inventory.TryReserveSoulboundCreation(InventoryLocation.Inventory, info, award.Count,
                out var delivery, out var error, ItemUpdateReason.Challenge))
            {
                player.SendGenericError(error); return false;
            }
            // 07CE has no cycle nonce. Bind this request to the exact latest
            // snapshot actually included in a full reload, then retain its
            // immutable cycle/revision/claim identity through all retries.
            var command = CharacterChallengeRewardCommand.Claim(characterId, track.TrackId, track.CycleId,
                track.Revision, rewardRowId, choice, Guid.NewGuid().ToString("N"), DateTime.UtcNow, satisfied);
            try
            {
                if (rewardQueue.Enqueue([], _ => command, delivery)) return true;
                delivery.Dispose(); return false;
            }
            catch { delivery.Dispose(); throw; }
        }
    }
}
