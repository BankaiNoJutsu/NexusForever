using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.PositionalRequirement)]
    public class PrerequisiteCheckPositionalRequirement : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckPositionalRequirement> log;
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPositionalRequirement(
            ILogger<PrerequisiteCheckPositionalRequirement> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Native (evaluator case 0x6c, vtable +0x1e0): with no second unit, ObjectId 0 or no table entry, NotEqual is true and every other comparison false
            // (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md). The entry flag that swaps the two units and wrapped angle ranges are not modelled.
            if (parameters.Target is not IUnitEntity facingEntity || objectId == 0)
                return comparison == PrerequisiteComparison.NotEqual;

            PositionalRequirementEntry entry = gameTableManager.PositionalRequirement.GetEntry(objectId);
            if (entry == null)
                return comparison == PrerequisiteComparison.NotEqual;

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                case PrerequisiteComparison.NotEqual:
                    return PositionalPrerequisiteHelper.MeetsDirectionalRequirement(comparison, facingEntity, subject, entry);
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.PositionalRequirement}!");
                    return false;
            }
        }
    }
}
