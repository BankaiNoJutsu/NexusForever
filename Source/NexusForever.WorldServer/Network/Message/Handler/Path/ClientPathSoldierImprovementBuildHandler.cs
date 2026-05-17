using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathSoldierImprovementBuildHandler : IMessageHandler<IWorldSession, ClientPathSoldierImprovementBuild>
    {
        private readonly ILogger<ClientPathSoldierImprovementBuildHandler> log;

        public ClientPathSoldierImprovementBuildHandler(ILogger<ClientPathSoldierImprovementBuildHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathSoldierImprovementBuild soldierBuild)
        {
            log.LogDebug("ClientPathSoldierImprovementBuild: player={Player}", session.Player?.Guid);
        }
    }
}
