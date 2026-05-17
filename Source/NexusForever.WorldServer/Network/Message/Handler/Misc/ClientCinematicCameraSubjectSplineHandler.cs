using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Cinematic;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientCinematicCameraSubjectSplineHandler : IMessageHandler<IWorldSession, ClientCinematicCameraSplineSubject>
    {
        private readonly ILogger<ClientCinematicCameraSubjectSplineHandler> log;

        public ClientCinematicCameraSubjectSplineHandler(ILogger<ClientCinematicCameraSubjectSplineHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCinematicCameraSplineSubject splineSubject)
        {
            log.LogDebug("ClientCinematicCameraSubjectSpline: player={Player} splineId={SplineId}",
                session.Player?.Guid, splineSubject.SplineId);
        }
    }
}
