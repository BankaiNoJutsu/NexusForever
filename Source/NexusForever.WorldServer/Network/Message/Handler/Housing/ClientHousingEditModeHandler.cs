using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingEditModeHandler : IMessageHandler<IWorldSession, ClientHousingEditMode>
    {
        private readonly ILogger<ClientHousingEditModeHandler> log;
        private readonly IGlobalResidenceManager globalResidenceManager;

        public ClientHousingEditModeHandler(
            ILogger<ClientHousingEditModeHandler> log,
            IGlobalResidenceManager globalResidenceManager)
        {
            this.log = log;
            this.globalResidenceManager = globalResidenceManager;
        }

        public void HandleMessage(IWorldSession session, ClientHousingEditMode housingEditMode)
        {
            if (session.Player.Map is not IResidenceMapInstance residenceMap)
                throw new InvalidPacketValueException();

            IResidence targetResidence = globalResidenceManager.GetResidenceByOwner(housingEditMode.TargetPlayerIdentity.Id);
            if (targetResidence == null || !targetResidence.CanModifyResidence(session.Player))
                throw new InvalidPacketValueException();

            residenceMap.SetEditMode(session.Player, targetResidence, housingEditMode.Enabled);

            log.LogDebug("ClientHousingEditMode: player={PlayerGuid}, targetRealm={TargetRealm}, targetId={TargetId}, enabled={Enabled}",
                session.Player?.Guid,
                housingEditMode.TargetPlayerIdentity.RealmId,
                housingEditMode.TargetPlayerIdentity.Id,
                housingEditMode.Enabled);
        }
    }
}
