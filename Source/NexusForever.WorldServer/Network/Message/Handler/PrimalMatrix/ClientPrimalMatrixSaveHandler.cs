using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PrimalMatrix;

namespace NexusForever.WorldServer.Network.Message.Handler.PrimalMatrix
{
    /// <summary>
    /// Applies a Primal Matrix save: the nodes and the number of allocations added to each since the last save. The request is validated against the allocation rules and the
    /// account's essence balances; the client then receives the saved state, which also resets its pending changes when the request was refused.
    /// See Decomp/Analysis/PRIMAL_MATRIX_RULES_20261110.md.
    /// </summary>
    public class ClientPrimalMatrixSaveHandler(ILogger<ClientPrimalMatrixSaveHandler> log)
        : IMessageHandler<IWorldSession, ClientPrimalMatrixSave>
    {
        public void HandleMessage(IWorldSession session, ClientPrimalMatrixSave message)
        {
            if (session.Player?.PrimalMatrixManager == null || message.NodeIds.Length != message.AllocationIncrements.Length)
                return;

            var request = new (uint NodeId, byte Increment)[message.NodeIds.Length];
            for (int i = 0; i < request.Length; i++)
                request[i] = (message.NodeIds[i], message.AllocationIncrements[i]);

            bool applied = session.Player.PrimalMatrixManager.TryAllocate(request);
            log.LogTrace("Primal Matrix save request with {NodeCount} node increments {Outcome}.", request.Length, applied ? "applied" : "refused");

            // always answer with the saved state: the client clears every node a snapshot does not list, so a refused first save needs an empty one
            session.Player.PrimalMatrixManager.SendSnapshot(force: true);
        }
    }
}
