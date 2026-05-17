using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientCinematicCameraSubjectUnitHandler : IMessageHandler<IWorldSession, ClientCinematicCameraSubjectUnit>
    {
        private readonly ILogger<ClientCinematicCameraSubjectUnitHandler> log;

        public ClientCinematicCameraSubjectUnitHandler(ILogger<ClientCinematicCameraSubjectUnitHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCinematicCameraSubjectUnit subjectUnit)
        {
            log.LogDebug("ClientCinematicCameraSubjectUnit: player={Player}", session.Player?.Guid);
        }
    }
}
