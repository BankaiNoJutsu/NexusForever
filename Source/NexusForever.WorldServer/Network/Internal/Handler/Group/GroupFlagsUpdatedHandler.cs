using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupFlagsUpdatedHandler : IHandleMessages<GroupFlagsUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupFlagsUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupFlagsUpdatedMessage message)
        {
            var groupFlagsChanged = new ServerGroupFlagsChanged
            {
                GroupId = message.Group.Id,
                Flags   = message.Group.Flags
            };

            playerManager.EnqueueToOnlineMembers(message.Group.Members, groupFlagsChanged);

            return Task.CompletedTask;
        }
    }
}
