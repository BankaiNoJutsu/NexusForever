using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Vehicle
{
    public class ClientVehicleEmbarkHandler : IMessageHandler<IWorldSession, ClientVehicleEmbark>
    {
        private readonly ILogger<ClientVehicleEmbarkHandler> log;

        public ClientVehicleEmbarkHandler(ILogger<ClientVehicleEmbarkHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientVehicleEmbark vehicleEmbark)
        {
            log.LogDebug("ClientVehicleEmbark: player={Player} vehicleUnitId={VehicleUnitId}",
                session.Player?.Guid, vehicleEmbark.VehicleUnitId);
        }
    }
}
