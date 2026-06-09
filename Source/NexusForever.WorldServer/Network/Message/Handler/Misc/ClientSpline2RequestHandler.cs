using Microsoft.Extensions.Logging;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientSpline2RequestHandler : IMessageHandler<IWorldSession, ClientSpline2Request>
    {
        private readonly ILogger<ClientSpline2RequestHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientSpline2RequestHandler(
            ILogger<ClientSpline2RequestHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientSpline2Request request)
        {
            bool knownSpline = gameTableManager.Spline2?.GetEntry(request.Spline2Id) != null;

            log.LogDebug("ClientSpline2Request: player={Player}, spline2Id={Spline2Id}, known={Known}.",
                session.Player?.Guid, request.Spline2Id, knownSpline);
        }
    }
}
