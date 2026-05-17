using Microsoft.Extensions.Logging;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientConvertResourceHandler : IMessageHandler<IWorldSession, ClientConvertResource>
    {
        private readonly ILogger<ClientConvertResourceHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientConvertResourceHandler(
            ILogger<ClientConvertResourceHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientConvertResource convertResource)
        {
            if (gameTableManager.ResourceConversion.GetEntry(convertResource.ConversionId) == null)
            {
                log.LogDebug("Ignoring resource conversion request from player {PlayerGuid}: unknown conversion {ConversionId}, selector {Selector}, resource field {ResourceField}.",
                    session.Player?.Guid, convertResource.ConversionId, convertResource.Selector, convertResource.ResourceField);
                return;
            }

            log.LogDebug("Ignoring unsupported resource conversion request from player {PlayerGuid}: conversion {ConversionId}, selector {Selector}, resource field {ResourceField}.",
                session.Player?.Guid, convertResource.ConversionId, convertResource.Selector, convertResource.ResourceField);
        }
    }

    public class ClientDashCastHandler : IMessageHandler<IWorldSession, ClientDashCast>
    {
        private readonly ILogger<ClientDashCastHandler> log;

        public ClientDashCastHandler(ILogger<ClientDashCastHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientDashCast dashCast)
        {
            log.LogDebug("Ignoring client dash-cast state request from player {PlayerGuid}: direction/state {DirectionOrState}.",
                session.Player?.Guid, dashCast.DirectionOrState);
        }
    }

    public class ClientMovementFallLandHandler : IMessageHandler<IWorldSession, ClientMovementFallLand>
    {
        private readonly ILogger<ClientMovementFallLandHandler> log;

        public ClientMovementFallLandHandler(ILogger<ClientMovementFallLandHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientMovementFallLand fallLand)
        {
            log.LogDebug("Ignoring client movement fall-land state request from player {PlayerGuid}: state {LandingState}, position {Position}.",
                session.Player?.Guid, fallLand.LandingState, fallLand.Position);
        }
    }
}
