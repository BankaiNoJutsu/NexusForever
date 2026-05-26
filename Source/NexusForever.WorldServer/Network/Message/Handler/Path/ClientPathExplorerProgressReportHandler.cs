using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PlayerPath;

namespace NexusForever.WorldServer.Network.Message.Handler.Path
{
    public class ClientPathExplorerProgressReportHandler : IMessageHandler<IWorldSession, ClientPathExplorerProgressReport>
    {
        private readonly ILogger<ClientPathExplorerProgressReportHandler> log;

        public ClientPathExplorerProgressReportHandler(ILogger<ClientPathExplorerProgressReportHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPathExplorerProgressReport progressReport)
        {
            log.LogDebug("ClientPathExplorerProgressReport: player={Player} missionId={MissionId} nodeIndex={NodeIndex}",
                session.Player?.Guid, progressReport.PathMissionId, progressReport.ExplorerNodeIndex);

            if (progressReport.PathMissionId <= ushort.MaxValue)
                session.Player?.PathManager.CompleteMission((ushort)progressReport.PathMissionId);
        }
    }
}
