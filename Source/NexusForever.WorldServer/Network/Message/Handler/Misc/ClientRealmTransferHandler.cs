using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Server;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientRealmTransferHandler : IMessageHandler<IWorldSession, ClientRealmTransfer>
    {
        private readonly IServerManager serverManager;
        private readonly ILogger<ClientRealmTransferHandler> log;

        public ClientRealmTransferHandler(
            IServerManager serverManager,
            ILogger<ClientRealmTransferHandler> log)
        {
            this.serverManager = serverManager;
            this.log           = log;
        }

        public void HandleMessage(IWorldSession session, ClientRealmTransfer message)
        {
            CharacterModifyResult result;
            if (!IsKnownSessionCharacter(session, message.CharacterId))
            {
                result = CharacterModifyResult.RealmTransferFailed_InvalidCharacter;
            }
            else
            {
                IServerInfo server = serverManager.Servers.SingleOrDefault(s => s.Model.Id == message.TargetRealmId);
                result = server == null
                    ? CharacterModifyResult.RealmTransferFailed_InvalidRealm
                    : server.IsOnline
                        ? CharacterModifyResult.RealmTransferFailed_Internal
                        : CharacterModifyResult.RealmTransferFailed_ServerDown;
            }

            log.LogDebug("ClientRealmTransfer: player={PlayerGuid} characterId={CharacterId} targetRealmId={TargetRealmId} transferFlag={TransferFlag} result={Result}.",
                session.Player?.Guid, message.CharacterId, message.TargetRealmId, message.TransferFlag, result);

            session.EnqueueMessageEncrypted(new ServerRealmTransferResult
            {
                Result = result
            });
        }

        private static bool IsKnownSessionCharacter(IWorldSession session, ulong characterId)
        {
            var player = session.Player;
            if (player != null)
                return player.CharacterId == characterId;

            if (session.Characters != null)
                return session.Characters.Any(c => c.Id == characterId);

            return true;
        }
    }
}
