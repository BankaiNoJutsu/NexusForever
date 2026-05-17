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
    public class GroupMemberAddedHandler : IHandleMessages<GroupMemberAddedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public GroupMemberAddedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(GroupMemberAddedMessage message)
        {
            groupStateManager.UpdateGroup(message.Group.ToGroupLootState());

            var serverGroupMemberAdd = new ServerGroupMemberAdd
            {
                GroupId     = message.Group.Id,
                AddedMember = message.AddedMember.ToNetworkGroupMember(),
            };

            ServerGroupMemberStatUpdate groupMemberStatUpdate = message.AddedMember.ToNetworkGroupMemberStatUpdate(message.Group.Id);

            foreach (GroupMember member in message.Group.Members)
            {
                if (member.Identity == message.AddedMember.Identity)
                    continue;

                IPlayer player = playerManager.GetPlayer(member.Identity.ToGameIdentity());
                if (player == null)
                    continue;

                player.Session.EnqueueMessageEncrypted(serverGroupMemberAdd);
                player.Session.EnqueueMessageEncrypted(groupMemberStatUpdate);
            }

            return Task.CompletedTask;
        }
    }
}
