using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Server;
using NexusForever.Network;
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
            IServerInfo server = serverManager.Servers.SingleOrDefault(s => s.Model.Id == message.TargetRealmId);
            if (server == null)
                throw new InvalidPacketValueException();

            CharacterModifyResult result = server.IsOnline
                ? CharacterModifyResult.RealmTransferFailed_Internal
                : CharacterModifyResult.RealmTransferFailed_ServerDown;

            log.LogDebug("ClientRealmTransfer: player={PlayerGuid} characterId={CharacterId} targetRealmId={TargetRealmId} transferFlag={TransferFlag} result={Result}.",
                session.Player?.Guid, message.CharacterId, message.TargetRealmId, message.TransferFlag, result);

            session.EnqueueMessageEncrypted(new ServerRealmTransferResult
            {
                Result = result
            });
        }
    }
}