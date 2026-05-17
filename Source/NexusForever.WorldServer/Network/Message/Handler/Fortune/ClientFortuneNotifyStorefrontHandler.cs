using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneNotifyStorefrontHandler : IMessageHandler<IWorldSession, ClientFortuneNotifyStorefront>
    {
        private readonly ILogger<ClientFortuneNotifyStorefrontHandler> log;

        public ClientFortuneNotifyStorefrontHandler(ILogger<ClientFortuneNotifyStorefrontHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientFortuneNotifyStorefront _)
        {
            log.LogDebug("ClientFortuneNotifyStorefront: player={Player}", session.Player?.Guid);
        }
    }
}
