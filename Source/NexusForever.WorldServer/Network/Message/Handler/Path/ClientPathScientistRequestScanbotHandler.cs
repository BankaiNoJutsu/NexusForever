using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathScientistRequestScanbotHandler : IMessageHandler<IWorldSession, ClientPathScientistRequestScanbot>
    {
        private readonly ILogger<ClientPathScientistRequestScanbotHandler> log;

        public ClientPathScientistRequestScanbotHandler(ILogger<ClientPathScientistRequestScanbotHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathScientistRequestScanbot requestScanbot)
        {
            log.LogDebug("ClientPathScientistRequestScanbot: player={Player} profile={Profile} isGameCommand={IsGameCommand}",
                session.Player?.Guid, requestScanbot.ScanbotProfile, requestScanbot.IsGameCommand);

            if (session.Player?.PathManager?.TryDeployScientistScanbot(requestScanbot.ScanbotProfile) != true)
                log.LogDebug("ClientPathScientistRequestScanbot rejected: player={Player} profile={Profile}",
                    session.Player?.Guid, requestScanbot.ScanbotProfile);
        }
    }
}
