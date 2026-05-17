using System.Threading.Tasks;
using NexusForever.Game.Abstract.Group;
using NexusForever.Network.Internal.Message.Group;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupDisbandedHandler : IHandleMessages<GroupDisbandedMessage>
    {
        private readonly IGroupStateManager groupStateManager;

        public GroupDisbandedHandler(
            IGroupStateManager groupStateManager)
        {
            this.groupStateManager = groupStateManager;
        }

        public Task Handle(GroupDisbandedMessage message)
        {
            if (message.Group != null)
                groupStateManager.RemoveGroup(message.Group.Id);

            return Task.CompletedTask;
        }
    }
}
