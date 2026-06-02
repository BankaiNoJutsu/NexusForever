using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.PrimalMatrixNode)]
    public class PrerequisiteCheckPrimalMatrixNode : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<PrerequisiteCheckPrimalMatrixNode> log;

        public PrerequisiteCheckPrimalMatrixNode(
            IGameTableManager gameTableManager,
            ILogger<PrerequisiteCheckPrimalMatrixNode> log)
        {
            this.gameTableManager = gameTableManager;
            this.log = log;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (gameTableManager.PrimalMatrixNode.GetEntry(objectId) == null)
                return false;

            log.LogTrace(
                "PrimalMatrixNode prerequisite is not fully implemented (nodeId={NodeId}); comparison={Comparison} value={Value}.",
                objectId, comparison, value);

            return false;
        }
    }
}
