using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupReadyCheckStartedHandler : IHandleMessages<GroupReadyCheckStartedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupReadyCheckStartedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupReadyCheckStartedMessage message)
        {
            var readyCheckMessage = new ServerGroupSendReadyCheck
            {
                GroupId = message.Group.Id,
                Invoker = message.Member.Identity.ToNetworkIdentity(),
                Message = message.Message,
            };

            playerManager.EnqueueToOnlineMembers(message.Group.Members, readyCheckMessage);

            return Task.CompletedTask;
        }
    }
}
