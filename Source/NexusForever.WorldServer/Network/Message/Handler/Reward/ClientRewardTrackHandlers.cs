using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Challenges.Reward;
using NexusForever.GameTable;

namespace NexusForever.WorldServer.Network.Message.Handler.Reward
{
    public sealed class ClientRewardTrackChooseTrackHandler(IGameTableManager tables) : IMessageHandler<IWorldSession, ClientRewardTrackChooseTrack>
    {
        public void HandleMessage(IWorldSession session, ClientRewardTrackChooseTrack message)
        {
            if (session.Player == null || !ReferenceEquals(session.Player.Account, session.Account)
                || !ReferenceEquals(session.Player.Session, session)) return;
            switch (tables.RewardTrack?.GetEntry(message.RewardTrackId)?.RewardTrackTypeEnum)
            {
                case 0:
                case 1: session.Player.QuestManager?.ChooseContractRewardTrack(message.RewardTrackId); break;
                case LoyaltyCatalog.LoyaltyTrackType: session.Account.LoyaltyManager?.ChooseTrack(session.Player, message.RewardTrackId); break;
                case CharacterChallengeRewardCatalog.ChallengeTrackType: session.Player.ChallengeManager?.ChooseRewardTrack(message.RewardTrackId); break;
            }
        }
    }

    public sealed class ClientRewardTrackClaimRewardPointHandler(IGameTableManager tables) : IMessageHandler<IWorldSession, ClientRewardTrackClaimRewardPoint>
    {
        public void HandleMessage(IWorldSession session, ClientRewardTrackClaimRewardPoint message)
        {
            if (session.Player == null || !ReferenceEquals(session.Player.Account, session.Account)
                || !ReferenceEquals(session.Player.Session, session)) return;
            var row = tables.RewardTrackRewards?.GetEntry(message.RewardTrackRewardsId);
            switch (row == null ? null : tables.RewardTrack?.GetEntry(row.RewardTrackId)?.RewardTrackTypeEnum)
            {
                case LoyaltyCatalog.LoyaltyTrackType: session.Account.LoyaltyManager?.ClaimReward(session.Player, message.RewardTrackRewardsId, message.ChoiceIndex); break;
                case CharacterChallengeRewardCatalog.ChallengeTrackType: session.Player.ChallengeManager?.ClaimRewardTrack(message.RewardTrackRewardsId, message.ChoiceIndex); break;
            }
        }
    }
}
