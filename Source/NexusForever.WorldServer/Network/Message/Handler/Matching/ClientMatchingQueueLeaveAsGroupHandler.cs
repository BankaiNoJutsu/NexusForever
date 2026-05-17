using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingQueueLeaveAsGroupHandler : IMessageHandler<IWorldSession, ClientMatchingQueueLeaveAsGroup>
    {
        private readonly ILogger<ClientMatchingQueueLeaveAsGroupHandler> log;

        public ClientMatchingQueueLeaveAsGroupHandler(ILogger<ClientMatchingQueueLeaveAsGroupHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingQueueLeaveAsGroup leaveAsGroup)
        {
            log.LogDebug("ClientMatchingQueueLeaveAsGroup: player={Player} matchType={MatchType}",
                session.Player?.Guid, leaveAsGroup.MatchType);
        }
    }
}
