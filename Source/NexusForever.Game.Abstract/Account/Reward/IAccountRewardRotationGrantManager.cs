using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Abstract.Account.Reward
{
    public interface IAccountRewardRotationGrantManager
    {
        ServerRewardRotationEntryStateArray BuildEntryState(uint rewardRotationIndex);

        void RecordGrant(uint rewardRotationIndex, uint contentId, uint rewardKeyId, byte rewardType, uint grantFlags);
    }
}
