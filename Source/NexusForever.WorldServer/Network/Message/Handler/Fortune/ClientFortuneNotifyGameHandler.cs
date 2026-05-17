using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneNotifyGameHandler : IMessageHandler<IWorldSession, ClientFortuneNotifyGame>
    {
        private readonly ILogger<ClientFortuneNotifyGameHandler> log;

        public ClientFortuneNotifyGameHandler(ILogger<ClientFortuneNotifyGameHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientFortuneNotifyGame _)
        {
            log.LogDebug("ClientFortuneNotifyGame: player={Player}", session.Player?.Guid);
        }
    }
}
