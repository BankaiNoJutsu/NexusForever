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
            log.LogDebug("Ignoring unsupported reward update request from player {PlayerGuid}: reward rotation index {RewardRotationIndex}.",
                session.Player?.Guid, rewardUpdateRequest.RewardRotationIndex);
        }
    }
}
