using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMaxSizeUpdatedHandler : IHandleMessages<GroupMaxSizeUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupMaxSizeUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupMaxSizeUpdatedMessage message)
        {
            var groupMaxSizeChange = new ServerGroupMaxSizeChange
            {
                GroupId    = message.Group.Id,
                NewFlags   = message.Group.Flags,
                NewMaxSize = message.Group.MaxGroupSize
            };

            playerManager.EnqueueToOnlineMembers(message.Group.Members, groupMaxSizeChange);

            return Task.CompletedTask;
        }
    }
}
