using NexusForever.Game.Abstract.Reward;
using NexusForever.Game.Reward;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private RewardTrackPresentation rewardTrackPresentation;
        private int rewardTrackPresentationInvalidated;
        public IRewardTrackPresentation RewardTrackPresentation
        {
            get
            {
                RewardTrackPresentation presentation = System.Threading.LazyInitializer.EnsureInitialized(
                    ref rewardTrackPresentation, () => new RewardTrackPresentation(this));
                if (System.Threading.Volatile.Read(ref rewardTrackPresentationInvalidated) != 0)
                    presentation.Invalidate();
                return presentation;
            }
        }

        private void InvalidateRewardTrackPresentation()
        {
            System.Threading.Interlocked.Exchange(ref rewardTrackPresentationInvalidated, 1);
            rewardTrackPresentation?.Invalidate();
        }
    }
}
