using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PrimalMatrix;

namespace NexusForever.WorldServer.Network.Message.Handler.PrimalMatrix
{
    /// <summary>
    /// Diagnostic dispatch only. Authoritative allocation/reward rules and
    /// durable settlement are required before this request can mutate state.
    /// An empty snapshot would clear client state, so none is fabricated here.
    /// </summary>
    public class ClientPrimalMatrixSaveHandler(ILogger<ClientPrimalMatrixSaveHandler> log)
        : IMessageHandler<IWorldSession, ClientPrimalMatrixSave>
    {
        public void HandleMessage(IWorldSession session, ClientPrimalMatrixSave message)
        {
            log.LogTrace("Primal Matrix save request decoded with {NodeCount} node increments; allocation service remains unavailable.", message.NodeIds.Length);
        }
    }
}
