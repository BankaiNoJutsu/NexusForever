using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Account.Reward
{
    public interface IAccountLoyaltyManager : IDatabaseAuth
    {
        void SendInitialPackets(IPlayer player);
        bool ChooseTrack(IPlayer player, uint trackId);
        bool ClaimReward(IPlayer player, uint rewardRowId, uint choice);
    }
}
