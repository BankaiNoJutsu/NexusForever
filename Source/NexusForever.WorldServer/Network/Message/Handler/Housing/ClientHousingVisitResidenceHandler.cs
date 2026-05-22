using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingVisitResidenceHandler : IMessageHandler<IWorldSession, ClientHousingVisitResidence>
    {
        private readonly ILogger<ClientHousingVisitResidenceHandler> log;
        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IMapLockManager mapLockManager;

        public ClientHousingVisitResidenceHandler(
            ILogger<ClientHousingVisitResidenceHandler> log,
            IGlobalResidenceManager globalResidenceManager,
            IMapLockManager mapLockManager)
        {
            this.log                    = log;
            this.globalResidenceManager = globalResidenceManager;
            this.mapLockManager         = mapLockManager;
        }

        public void HandleMessage(IWorldSession session, ClientHousingVisitResidence visitResidence)
        {
            log.LogDebug("ClientHousingVisitResidence: player={Player} residenceId={ResidenceId}",
                session.Player?.Guid, visitResidence.TargetResidence.ResidenceId);

            if (!HousingVisitHelper.CanProcessVisit(session))
                return;

            IResidence residence = globalResidenceManager.GetResidence(visitResidence.TargetResidence.ResidenceId);
            HousingVisitHelper.VisitResidence(
                session,
                residence,
                visitResidence.TargetResidence.RealmId,
                visitResidence.TargetResidence.ResidenceId,
                string.Empty,
                globalResidenceManager,
                mapLockManager);
        }
    }
}
