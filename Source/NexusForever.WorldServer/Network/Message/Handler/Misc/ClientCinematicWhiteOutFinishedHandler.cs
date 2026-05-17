using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientCinematicWhiteOutFinishedHandler : IMessageHandler<IWorldSession, ClientCinematicWhiteOutFinished>
    {
        private readonly ILogger<ClientCinematicWhiteOutFinishedHandler> log;

        public ClientCinematicWhiteOutFinishedHandler(ILogger<ClientCinematicWhiteOutFinishedHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCinematicWhiteOutFinished _)
        {
            log.LogDebug("ClientCinematicWhiteOutFinished: player={Player}", session.Player?.Guid);
        }
    }
}
