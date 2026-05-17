using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Internal.Message.Group.Shared;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMemberJoinedHandler : IHandleMessages<GroupMemberJoinedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public GroupMemberJoinedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(GroupMemberJoinedMessage message)
        {
            groupStateManager.UpdateGroup(message.Group.ToGroupLootState());

            IPlayer player = playerManager.GetPlayer(message.AddedMember.Identity.ToGameIdentity());
            if (player == null)
                return Task.CompletedTask;

            player.Session.EnqueueMessageEncrypted(new ServerGroupJoin
            {
                Group        = message.Group.ToNetworkGroup(),
                TargetPlayer = message.AddedMember.Identity.ToNetworkIdentity()
            });

            foreach (GroupMember member in message.Group.Members)
            {
                if (message.AddedMember.Identity == member.Identity)
                    continue;

                ServerGroupMemberStatUpdate groupMemberStatUpdate = member.ToNetworkGroupMemberStatUpdate(message.Group.Id);
                player.Session.EnqueueMessageEncrypted(groupMemberStatUpdate);
            }

            return Task.CompletedTask;
        }
    }
}
