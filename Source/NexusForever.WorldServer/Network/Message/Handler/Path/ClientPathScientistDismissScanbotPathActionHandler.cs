using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathScientistDismissScanbotPathActionHandler : IMessageHandler<IWorldSession, ClientPathScientistDismissScanbotPathAction>
    {
        private readonly ILogger<ClientPathScientistDismissScanbotPathActionHandler> log;

        public ClientPathScientistDismissScanbotPathActionHandler(ILogger<ClientPathScientistDismissScanbotPathActionHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathScientistDismissScanbotPathAction _)
        {
            log.LogDebug("ClientPathScientistDismissScanbotPathAction: player={Player}", session.Player?.Guid);

            if (session.Player?.PathManager?.DismissScientistScanbot() != true)
                log.LogDebug("ClientPathScientistDismissScanbotPathAction ignored: player={Player}", session.Player?.Guid);
        }
    }
}
