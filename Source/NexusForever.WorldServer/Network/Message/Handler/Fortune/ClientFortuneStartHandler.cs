using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneStartHandler : IMessageHandler<IWorldSession, ClientFortuneStart>
    {
        private readonly ILogger<ClientFortuneStartHandler> log;

        public ClientFortuneStartHandler(ILogger<ClientFortuneStartHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientFortuneStart _)
        {
            log.LogDebug("ClientFortuneStart: player={Player}", session.Player?.Guid);
        }
    }
}
