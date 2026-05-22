using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Matching
{
    public class ClientMatchingQueueLeaveAsGroupHandler : IMessageHandler<IWorldSession, ClientMatchingQueueLeaveAsGroup>
    {
        private readonly ILogger<ClientMatchingQueueLeaveAsGroupHandler> log;
        private readonly IMatchingManager matchingManager;
        private readonly IGroupStateManager groupStateManager;
        private readonly IPlayerManager playerManager;

        public ClientMatchingQueueLeaveAsGroupHandler(
            ILogger<ClientMatchingQueueLeaveAsGroupHandler> log,
            IMatchingManager matchingManager,
            IGroupStateManager groupStateManager,
            IPlayerManager playerManager)
        {
            this.log               = log;
            this.matchingManager   = matchingManager;
            this.groupStateManager = groupStateManager;
            this.playerManager     = playerManager;
        }

        public void HandleMessage(IWorldSession session, ClientMatchingQueueLeaveAsGroup leaveAsGroup)
        {
            log.LogDebug("ClientMatchingQueueLeaveAsGroup: player={Player} matchType={MatchType}",
                session.Player?.Guid, leaveAsGroup.MatchType);

            // Leave queue for the requesting player
            matchingManager.LeaveQueue(session.Player, leaveAsGroup.MatchType);

            // Fan out to all online group members to leave the same queue
            if (groupStateManager.TryGetGroupForCharacter(session.Player.Identity, out GroupLootState group))
            {
                foreach (GroupLootMember member in group.Members)
                {
                    if (member.Identity == session.Player.Identity)
                        continue;

                    IPlayer memberPlayer = playerManager.GetPlayer(member.Identity);
                    if (memberPlayer != null)
                        matchingManager.LeaveQueue(memberPlayer, leaveAsGroup.MatchType);
                }
            }
        }
    }
}
