using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientCinematicCameraSubjectPosVelHandler : IMessageHandler<IWorldSession, ClientCinematicCameraSubjectPosVel>
    {
        private readonly ILogger<ClientCinematicCameraSubjectPosVelHandler> log;

        public ClientCinematicCameraSubjectPosVelHandler(ILogger<ClientCinematicCameraSubjectPosVelHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCinematicCameraSubjectPosVel posVel)
        {
            log.LogDebug("ClientCinematicCameraSubjectPosVel: player={Player}", session.Player?.Guid);
        }
    }
}
