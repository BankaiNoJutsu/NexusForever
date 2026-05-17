using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Vendor
{
    public class ClientRepairVendorStatusRequestHandler : IMessageHandler<IWorldSession, ClientRepairVendorStatusRequest>
    {
        private readonly ILogger<ClientRepairVendorStatusRequestHandler> log;

        public ClientRepairVendorStatusRequestHandler(ILogger<ClientRepairVendorStatusRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRepairVendorStatusRequest _)
        {
            log.LogDebug("Ignoring unsupported repair-vendor status request from player {PlayerGuid}.",
                session.Player?.Guid);
        }
    }

    public class ClientRepairItemVendorHandler : IMessageHandler<IWorldSession, ClientRepairItemVendor>
    {
        private readonly ILogger<ClientRepairItemVendorHandler> log;

        public ClientRepairItemVendorHandler(ILogger<ClientRepairItemVendorHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRepairItemVendor repairItemVendor)
        {
            log.LogDebug("Ignoring unsupported repair-vendor item request from player {PlayerGuid}: item identity {ItemIdentity}, optional field {OptionalField}, repair cost {RepairCost}.",
                session.Player?.Guid, repairItemVendor.ItemIdentity, repairItemVendor.OptionalField, repairItemVendor.RepairCost);
        }
    }
}
