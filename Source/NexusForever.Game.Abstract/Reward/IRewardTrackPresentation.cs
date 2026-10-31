using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Abstract.Reward
{
    /// <summary>
    /// Actor-owned composition for 07D3, which replaces every track type on the
    /// client. Owners publish snapshots; presentation never calls back into them.
    /// </summary>
    public interface IRewardTrackPresentation
    {
        bool IsCurrent { get; }
        void ExpectCharacterTracks();
        bool PublishAccountTracks(IReadOnlyList<RewardTrack> tracks, bool reload = true);
        bool PublishCharacterTracks(IReadOnlyList<RewardTrack> tracks, bool reload = true);
        bool PublishCharacterTracks(IReadOnlyList<RewardTrack> tracks, object snapshot, bool reload = true) => false;
        bool IsPublishedCharacterSnapshot(object snapshot) => false;
        void ExpectContractTracks() { }
        bool PublishContractTracks(IReadOnlyList<RewardTrack> tracks, object snapshot, bool reload = true) => false;
        bool IsPublishedContractSnapshot(object snapshot) => false;
        void Invalidate();
    }
}
