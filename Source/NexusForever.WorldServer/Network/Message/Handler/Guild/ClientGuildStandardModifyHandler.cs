using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildStandardModifyHandler : IMessageHandler<IWorldSession, ClientGuildStandardModify>
    {
        private readonly ILogger<ClientGuildStandardModifyHandler> log;

        public ClientGuildStandardModifyHandler(ILogger<ClientGuildStandardModifyHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGuildStandardModify standardModify)
        {
            log.LogDebug("ClientGuildStandardModify: player={Player}", session.Player?.Guid);
        }
    }
}
