using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Internal.Message.Group.Shared;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMemberStatsUpdatedHandler : IHandleMessages<GroupMemberStatsUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupMemberStatsUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupMemberStatsUpdatedMessage message)
        {
            ServerGroupMemberStatUpdate groupMemberStatUpdate = message.Member.ToNetworkGroupMemberStatUpdate(message.Group.Id);
            ServerGroupRosterUpdate rosterUpdate               = message.Member.ToNetworkGroupRosterUpdate(message.Group.Id);

            playerManager.EnqueueToOnlineMembers(message.Group.Members, member => message.Member.Identity != member.Identity, groupMemberStatUpdate);
            playerManager.EnqueueToOnlineMembers(message.Group.Members, member => message.Member.Identity != member.Identity, rosterUpdate);

            return Task.CompletedTask;
        }
    }
}
