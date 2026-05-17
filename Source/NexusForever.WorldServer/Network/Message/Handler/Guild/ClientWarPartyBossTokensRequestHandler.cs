using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientWarPartyBossTokensRequestHandler : IMessageHandler<IWorldSession, ClientWarPartyBossTokensRequest>
    {
        private readonly ILogger<ClientWarPartyBossTokensRequestHandler> log;

        public ClientWarPartyBossTokensRequestHandler(ILogger<ClientWarPartyBossTokensRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientWarPartyBossTokensRequest request)
        {
            log.LogDebug("Returning empty war-party boss token list for player {PlayerGuid}: guild identity {GuildIdentity}.",
                session.Player?.Guid, request.GuildIdentity);

            session.EnqueueMessageEncrypted(new ServerWarPartyBossTokens
            {
                GuildIdentity = request.GuildIdentity
            });
        }
    }
}
