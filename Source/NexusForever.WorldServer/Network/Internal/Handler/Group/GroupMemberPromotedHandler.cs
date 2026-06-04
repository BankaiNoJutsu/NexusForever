using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMemberPromotedHandler : IHandleMessages<GroupMemberPromotedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public GroupMemberPromotedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(GroupMemberPromotedMessage message)
        {
            groupStateManager.UpdateGroup(message.Group.ToGroupLootState());

            var groupPromote = new ServerGroupPromote
            {
                GroupId     = message.Group.Id,
                LeaderIndex = message.Member.GroupIndex,
                NewLeader   = message.Member.Identity.ToNetworkIdentity()
            };

            playerManager.EnqueueToOnlineMembers(message.Group.Members, groupPromote);

            return Task.CompletedTask;
        }
    }
}
