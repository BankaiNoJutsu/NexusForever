using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private IReadOnlyList<ContractTrackState> committedContractTracks;
        private bool contractRewardPacketsRequested;
        internal IReadOnlyList<ContractTrackState> CommittedContractTracks => committedContractTracks;

        internal void ExpectContractRewardTracks(bool packets = false)
        {
            if (questRewardReload || questRewardInvalidated || !ReferenceEquals(player.QuestManager, this)) return;
            player.RewardTrackPresentation?.ExpectContractTracks();
            contractRewardPacketsRequested |= packets;
            if (packets && committedContractTracks != null) PublishContractRewards();
        }

        internal void AdoptContractRewards(IReadOnlyList<ContractTrackState> tracks)
        {
            if (tracks == null) return; // a custom execution did not supply this owner
            if (questRewardReload || questRewardInvalidated || !ReferenceEquals(player.QuestManager, this))
                throw new InvalidOperationException("Contract reward snapshot lost its current Quest owner.");
            if (tracks.Count != 0) contractRewardCatalog ??= new ContractRewardCatalog(gameTableManager);
            var copy = ContractRewardSnapshot.Validate(tracks, player.CharacterId, contractRewardCatalog);
            foreach (var before in committedContractTracks ?? [])
            {
                var after = copy.SingleOrDefault(track => track.TrackId == before.TrackId);
                if (after == null || after.Revision < before.Revision || after.Revision == before.Revision && !ContractRewardSnapshot.Same(after, before))
                    throw new InvalidOperationException("Contract snapshot cannot replace newer or conflicting committed intent.");
            }
            if (committedContractTracks == null || committedContractTracks.Count != copy.Count || !committedContractTracks.Zip(copy).All(pair => ContractRewardSnapshot.Same(pair.First, pair.Second)))
                committedContractTracks = copy;
            PublishContractRewards(reload: false);
        }

        internal bool PublishContractRewards(bool reload = true)
        {
            if (!contractRewardPacketsRequested || committedContractTracks == null || questRewardReload || questRewardInvalidated
                || !ReferenceEquals(player.QuestManager, this)) return false;
            // Only durable selected rows are transmitted. Legacy unselected
            // cycles remain empty; claims/unlock history is never inferred.
            var rows = committedContractTracks.Select(track =>
            {
                var claimed = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
                foreach (var slot in track.Slots.Where(slot => slot.Claimed)) claimed.SetBit(slot.Ordinal, true);
                return new RewardTrack
                {
                    RewardTrackId = track.TrackId, RewardPointsEarned = track.Points, IsActive = track.Active,
                    RewardTrackRewardsIds = track.Slots.Select(slot => slot.RewardRowId).ToList(), ClaimedRewards = claimed
                };
            }).ToArray();
            return player.RewardTrackPresentation?.PublishContractTracks(rows, committedContractTracks, reload) == true;
        }
    }
}
