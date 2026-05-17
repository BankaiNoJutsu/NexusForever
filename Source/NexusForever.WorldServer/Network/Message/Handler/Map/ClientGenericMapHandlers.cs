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
        private readonly IGameTableManager gameTableManager;

        public ClientGenericMapNodeChosenHandler(
            ILogger<ClientGenericMapNodeChosenHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientGenericMapNodeChosen genericMapNodeChosen)
        {
            var node = gameTableManager.GenericMapNode.GetEntry(genericMapNodeChosen.GenericMapNodeId);
            if (node == null)
            {
                log.LogDebug("Ignoring generic map node choice from player {PlayerGuid}: unknown node {GenericMapNodeId}.",
                    session.Player?.Guid, genericMapNodeChosen.GenericMapNodeId);
                return;
            }

            if (node.WorldLocation2Id == 0u)
            {
                log.LogDebug("Acknowledging generic map node choice from player {PlayerGuid}: node {GenericMapNodeId} has no destination.",
                    session.Player?.Guid, genericMapNodeChosen.GenericMapNodeId);
                session.EnqueueMessageEncrypted(new ServerGenericMapNode
                {
                    Node = new GenericMapNode
                    {
                        GenericMapModeId = genericMapNodeChosen.GenericMapNodeId,
                        Enabled          = true
                    }
                });
                return;
            }

            var location = gameTableManager.WorldLocation2.GetEntry(node.WorldLocation2Id);
            if (location == null)
            {
                log.LogWarning("Unable to process generic map node choice from player {PlayerGuid}: node {GenericMapNodeId} references missing world location {WorldLocation2Id}.",
                    session.Player?.Guid, genericMapNodeChosen.GenericMapNodeId, node.WorldLocation2Id);
                return;
            }

            log.LogDebug("Teleporting player {PlayerGuid} from generic map node choice: node {GenericMapNodeId}, world location {WorldLocation2Id}.",
                session.Player?.Guid, genericMapNodeChosen.GenericMapNodeId, node.WorldLocation2Id);
            session.Player.TeleportTo((ushort)location.WorldId, location.Position0, location.Position1, location.Position2);
        }
    }
}
