using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientRecruitmentGuildGetDetailedGuildInfoHandler : IMessageHandler<IWorldSession, ClientRecruitmentGuildGetDetailedGuildInfo>
    {
        private readonly ILogger<ClientRecruitmentGuildGetDetailedGuildInfoHandler> log;

        public ClientRecruitmentGuildGetDetailedGuildInfoHandler(ILogger<ClientRecruitmentGuildGetDetailedGuildInfoHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientRecruitmentGuildGetDetailedGuildInfo getDetailedInfo)
        {
            log.LogDebug("ClientRecruitmentGuildGetDetailedGuildInfo: player={Player}", session.Player?.Guid);
        }
    }
}
