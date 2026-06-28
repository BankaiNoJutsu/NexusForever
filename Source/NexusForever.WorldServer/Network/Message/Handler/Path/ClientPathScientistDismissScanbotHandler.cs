using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathScientistDismissScanbotHandler : IMessageHandler<IWorldSession, ClientPathScientistDismissScanbot>
    {
        private readonly ILogger<ClientPathScientistDismissScanbotHandler> log;

        public ClientPathScientistDismissScanbotHandler(ILogger<ClientPathScientistDismissScanbotHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathScientistDismissScanbot _)
        {
            log.LogDebug("ClientPathScientistDismissScanbot: player={Player}", session.Player?.Guid);

            if (session.Player?.PathManager?.DismissScientistScanbot() != true)
                log.LogDebug("ClientPathScientistDismissScanbot ignored: player={Player}", session.Player?.Guid);
        }
    }
}
