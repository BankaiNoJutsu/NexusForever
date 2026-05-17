using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Spell;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastGuildBossTokenHandler : IMessageHandler<IWorldSession, ClientCastGuildBossToken>
    {
        private readonly ILogger<ClientCastGuildBossTokenHandler> log;

        public ClientCastGuildBossTokenHandler(ILogger<ClientCastGuildBossTokenHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCastGuildBossToken request)
        {
            log.LogDebug("Rejecting guild boss token cast from player {PlayerGuid}: guild identity {GuildIdentity}, item {Item2Id}, context token {ContextToken}.",
                session.Player?.Guid, request.GuildIdentity, request.Item2Id, request.ContextToken);

            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                Spell4Id   = request.Item2Id,
                CastResult = CastResult.BossTokenNotReady
            });
        }
    }
}
