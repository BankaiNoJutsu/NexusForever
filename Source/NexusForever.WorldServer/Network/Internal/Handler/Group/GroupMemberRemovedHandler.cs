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
    public class GroupMemberRemovedHandler : IHandleMessages<GroupMemberRemovedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public GroupMemberRemovedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(GroupMemberRemovedMessage message)
        {
            groupStateManager.UpdateGroup(message.Group.ToGroupLootState());
            groupStateManager.RemoveMember(message.Group.Id, message.RemovedMember.Identity.ToGameIdentity());

            var groupRemove = new ServerGroupRemove
            {
                GroupId      = message.Group.Id,
                Reason       = message.Reason,
                TargetPlayer = message.RemovedMember.Identity.ToNetworkIdentity(),
            };

            foreach (GroupMember member in message.Group.Members)
            {
                IPlayer player = playerManager.GetPlayer(member.Identity.ToGameIdentity());
                player?.Session.EnqueueMessageEncrypted(groupRemove);
            }

            return Task.CompletedTask;
        }
    }
}
