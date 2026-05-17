using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathSettlerImprovementBuildTierHandler : IMessageHandler<IWorldSession, ClientPathSettlerImprovementBuildTier>
    {
        private readonly ILogger<ClientPathSettlerImprovementBuildTierHandler> log;

        public ClientPathSettlerImprovementBuildTierHandler(ILogger<ClientPathSettlerImprovementBuildTierHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathSettlerImprovementBuildTier buildTier)
        {
            log.LogDebug("ClientPathSettlerImprovementBuildTier: player={Player}", session.Player?.Guid);
        }
    }
}
