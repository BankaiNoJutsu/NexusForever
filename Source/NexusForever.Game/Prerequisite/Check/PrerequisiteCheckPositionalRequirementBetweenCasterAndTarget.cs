using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 159: client case <c>0x9f</c> dispatches vtable <c>+0x1e0</c> to <c>14049d940</c>.
    /// The native body resolves <see cref="PositionalRequirementEntry"/> from <c>objectId0</c>, checks the angle
    /// between caster and target, and swaps the facing source when <c>Flags &amp; 1</c> is set.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PositionalRequirementBetweenCasterAndTarget)]
    public class PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget : IPrerequisiteCheck
    {
        private readonly ILogger<PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget> log;
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget(
            ILogger<PrerequisiteCheckPositionalRequirementBetweenCasterAndTarget> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            PositionalRequirementEntry entry = objectId == 0u ? null : gameTableManager.PositionalRequirement.GetEntry(objectId);
            if (parameters.Target is not IUnitEntity target || entry == null)
                return comparison == PrerequisiteComparison.NotEqual;

            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                case PrerequisiteComparison.NotEqual:
                    IUnitEntity facingEntity = (entry.Flags & 1u) != 0u ? player : target;
                    IUnitEntity checkedEntity = (entry.Flags & 1u) != 0u ? target : player;
                    return PositionalPrerequisiteHelper.MeetsDirectionalRequirement(comparison, facingEntity, checkedEntity, entry);
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.PositionalRequirementBetweenCasterAndTarget}!");
                    return false;
            }
        }
    }
}
