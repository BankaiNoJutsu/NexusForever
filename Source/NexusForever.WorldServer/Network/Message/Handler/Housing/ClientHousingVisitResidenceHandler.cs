using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingVisitResidenceHandler : IMessageHandler<IWorldSession, ClientHousingVisitResidence>
    {
        private readonly ILogger<ClientHousingVisitResidenceHandler> log;

        public ClientHousingVisitResidenceHandler(ILogger<ClientHousingVisitResidenceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingVisitResidence visitResidence)
        {
            log.LogDebug("ClientHousingVisitResidence: player={Player} residenceId={ResidenceId}",
                session.Player?.Guid, visitResidence.TargetResidence.ResidenceId);
        }
    }
}
