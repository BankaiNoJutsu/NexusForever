using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientRecruitmentGuildSubscribeHandler : IMessageHandler<IWorldSession, ClientRecruitmentGuildSubscribe>
    {
        private readonly ILogger<ClientRecruitmentGuildSubscribeHandler> log;

        public ClientRecruitmentGuildSubscribeHandler(ILogger<ClientRecruitmentGuildSubscribeHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRecruitmentGuildSubscribe subscribe)
        {
            log.LogDebug("ClientRecruitmentGuildSubscribe: player={Player} subscribe={Subscribe}",
                session.Player?.Guid, subscribe.Subscribe);
        }
    }
}
