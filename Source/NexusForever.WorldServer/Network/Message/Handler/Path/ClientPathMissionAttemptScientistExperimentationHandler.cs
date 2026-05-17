using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathMissionAttemptScientistExperimentationHandler : IMessageHandler<IWorldSession, ClientPathMissionAttemptScientistExperimentation>
    {
        private readonly ILogger<ClientPathMissionAttemptScientistExperimentationHandler> log;

        public ClientPathMissionAttemptScientistExperimentationHandler(ILogger<ClientPathMissionAttemptScientistExperimentationHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathMissionAttemptScientistExperimentation attemptExperimentation)
        {
            log.LogDebug("ClientPathMissionAttemptScientistExperimentation: player={Player}", session.Player?.Guid);
        }
    }
}
