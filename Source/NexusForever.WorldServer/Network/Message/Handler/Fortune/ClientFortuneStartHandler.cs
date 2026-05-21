using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneStartHandler : IMessageHandler<IWorldSession, ClientFortuneStart>
    {
        private readonly ILogger<ClientFortuneStartHandler> log;
        private readonly IFortuneSessionManager fortuneSessionManager;

        public ClientFortuneStartHandler(
            ILogger<ClientFortuneStartHandler> log,
            IFortuneSessionManager fortuneSessionManager)
        {
            this.log                   = log;
            this.fortuneSessionManager = fortuneSessionManager;
        }

        public void HandleMessage(IWorldSession session, ClientFortuneStart _)
        {
            log.LogDebug("ClientFortuneStart: player={Player}", session.Player?.Guid);
            fortuneSessionManager.Start(session);
        }
    }
}
