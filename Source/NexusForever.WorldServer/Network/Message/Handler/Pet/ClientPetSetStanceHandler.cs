using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientPetSetStanceHandler : IMessageHandler<IWorldSession, ClientPetSetStance>
    {
        private readonly ILogger<ClientPetSetStanceHandler> log;

        public ClientPetSetStanceHandler(ILogger<ClientPetSetStanceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPetSetStance petSetStance)
        {
            log.LogDebug("ClientPetSetStance: player={Player} petUnitId={PetUnitId} stance={Stance}",
                session.Player?.Guid, petSetStance.PetUnitId, petSetStance.Stance);
        }
    }
}
