using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathExplorerPowerMapProgressHandler : IMessageHandler<IWorldSession, ClientPathExplorerPowerMapProgress>
    {
        private readonly ILogger<ClientPathExplorerPowerMapProgressHandler> log;

        public ClientPathExplorerPowerMapProgressHandler(ILogger<ClientPathExplorerPowerMapProgressHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathExplorerPowerMapProgress powerMapProgress)
        {
            log.LogDebug("ClientPathExplorerPowerMapProgress: player={Player} missionId={MissionId}",
                session.Player?.Guid, powerMapProgress.PathMissionId);
        }
    }
}
