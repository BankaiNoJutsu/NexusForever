using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildSetStandardHandler : IMessageHandler<IWorldSession, ClientGuildSetStandard>
    {
        private readonly ILogger<ClientGuildSetStandardHandler> log;

        public ClientGuildSetStandardHandler(ILogger<ClientGuildSetStandardHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGuildSetStandard setStandard)
        {
            log.LogDebug("ClientGuildSetStandard: player={Player}", session.Player?.Guid);
        }
    }
}
