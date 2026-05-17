using Microsoft.Extensions.Logging;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Map;

namespace NexusForever.WorldServer.Network.Message.Handler.Map
{
    public class ClientGenericMapNodeRequestHandler : IMessageHandler<IWorldSession, ClientGenericMapNodeRequest>
    {
        private readonly ILogger<ClientGenericMapNodeRequestHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientGenericMapNodeRequestHandler(
            ILogger<ClientGenericMapNodeRequestHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientGenericMapNodeRequest genericMapNodeRequest)
        {
            if (gameTableManager.GenericMapNode.GetEntry(genericMapNodeRequest.GenericMapNodeId) == null)
            {
                log.LogDebug("Ignoring generic map node request from player {PlayerGuid}: unknown node {GenericMapNodeId}.",
                    session.Player?.Guid, genericMapNodeRequest.GenericMapNodeId);
                return;
            }

            log.LogTrace("Sending generic map node state to player {PlayerGuid}: node {GenericMapNodeId}.",
                session.Player?.Guid, genericMapNodeRequest.GenericMapNodeId);

            session.EnqueueMessageEncrypted(new ServerGenericMapNode
            {
                Node = new GenericMapNode
                {
                    GenericMapModeId = genericMapNodeRequest.GenericMapNodeId,
                    Enabled          = true
                }
            });
        }
    }

    public class ClientGenericMapNodeChosenHandler : IMessageHandler<IWorldSession, ClientGenericMapNodeChosen>
    {
        private readonly ILogger<ClientGenericMapNodeChosenHandler> log;

        public ClientGenericMapNodeChosenHandler(ILogger<ClientGenericMapNodeChosenHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGenericMapNodeChosen genericMapNodeChosen)
        {
            log.LogDebug("Ignoring unsupported generic map node choice from player {PlayerGuid}: node {GenericMapNodeId}.",
                session.Player?.Guid, genericMapNodeChosen.GenericMapNodeId);
        }
    }
}
