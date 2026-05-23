using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingEditModeHandler : IMessageHandler<IWorldSession, ClientHousingEditMode>
    {
        private readonly ILogger<ClientHousingEditModeHandler> log;

        public ClientHousingEditModeHandler(ILogger<ClientHousingEditModeHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientHousingEditMode housingEditMode)
        {
            if (session.Player.Map is not IResidenceMapInstance)
                throw new InvalidPacketValueException();

            log.LogDebug("ClientHousingEditMode: player={PlayerGuid}, targetRealm={TargetRealm}, targetId={TargetId}, enabled={Enabled}",
                session.Player?.Guid,
                housingEditMode.TargetPlayerIdentity.RealmId,
                housingEditMode.TargetPlayerIdentity.Id,
                housingEditMode.Enabled);
        }
    }
}
