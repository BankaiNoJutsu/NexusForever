using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemContextActionHandler : IMessageHandler<IWorldSession, ClientItemContextAction>
    {
        private readonly ILogger<ClientItemContextActionHandler> log;

        public ClientItemContextActionHandler(ILogger<ClientItemContextActionHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientItemContextAction itemContextAction)
        {
            log.LogDebug("ClientItemContextAction: player={Player} itemGuid={ItemGuid} selectedBranch={SelectedBranch}",
                session.Player?.Guid, itemContextAction.ItemGuid, itemContextAction.SelectedBranch);
        }
    }
}
