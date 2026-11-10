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

        /// <remarks>
        /// Native 1404a18f0: for the local player, the saved allocation count of node ObjectId (PrimalMatrix_LookupAllocatedNode 1404d6a60) is compared with Value.
        /// The count is the character's saved allocation count of the node (<see cref="IPrimalMatrixManager"/>), 0 for a node without any.
        /// All 24 retail slots are "== 1". Evidence tier: native body. No retail parity claim.
        /// </remarks>
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return false;

            uint allocatedCount = player.PrimalMatrixManager?.GetAllocations(objectId) ?? 0u;
            return PrerequisiteCompare.Compare(comparison, allocatedCount, value);
        }
    }
}
