using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientSpline2NodeDataHandler : IMessageHandler<IWorldSession, ClientSpline2NodeData>
    {
        private readonly ILogger<ClientSpline2NodeDataHandler> log;

        public ClientSpline2NodeDataHandler(ILogger<ClientSpline2NodeDataHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSpline2NodeData data)
        {
            log.LogDebug("ClientSpline2NodeData: player={Player}, spline2Id={Spline2Id}, nodeId={NodeId}, mode={Mode}, nodeCount={NodeCount}.",
                session.Player?.Guid, data.Spline2Id, data.NodeId, data.Mode, data.Nodes.Count);
        }
    }

    public class ClientSpline2NodePositionDataHandler : IMessageHandler<IWorldSession, ClientSpline2NodePositionData>
    {
        private readonly ILogger<ClientSpline2NodePositionDataHandler> log;

        public ClientSpline2NodePositionDataHandler(ILogger<ClientSpline2NodePositionDataHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSpline2NodePositionData data)
        {
            log.LogDebug("ClientSpline2NodePositionData: player={Player}, spline2Id={Spline2Id}, nodeId={NodeId}, valueCount={ValueCount}.",
                session.Player?.Guid, data.Spline2Id, data.NodeId, data.Values.Count);
        }
    }

    public class ClientSpline2DataRequestHandler : IMessageHandler<IWorldSession, ClientSpline2DataRequest>
    {
        private readonly ILogger<ClientSpline2DataRequestHandler> log;

        public ClientSpline2DataRequestHandler(ILogger<ClientSpline2DataRequestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSpline2DataRequest request)
        {
            log.LogDebug("ClientSpline2DataRequest: player={Player}, spline2Id={Spline2Id}.",
                session.Player?.Guid, request.Spline2Id);
        }
    }
}
