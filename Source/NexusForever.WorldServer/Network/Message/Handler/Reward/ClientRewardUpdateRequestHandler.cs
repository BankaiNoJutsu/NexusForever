using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Reward
{
    public class ClientRewardUpdateRequestHandler : IMessageHandler<IWorldSession, ClientRewardUpdateRequest>
    {
        private readonly ILogger<ClientRewardUpdateRequestHandler> log;

        public ClientRewardUpdateRequestHandler(
            ILogger<ClientRewardUpdateRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRewardUpdateRequest rewardUpdateRequest)
        {
            session.Account.RewardPropertyManager.SendInitialPackets();

            log.LogDebug("Refreshed reward properties for player {PlayerGuid}: reward rotation index {RewardRotationIndex}.",
                session.Player?.Guid, rewardUpdateRequest.RewardRotationIndex);
        }
    }
}
