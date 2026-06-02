using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 46: handler table <c>14049e680</c>; live waypoint-direction check.
    /// NF proxies via <see cref="PositionalRequirementEntry"/> when <c>objectId0</c> resolves to a row
    /// (same angular cone semantics as type 108).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Waypoint)]
    public class PrerequisiteCheckWaypoint : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckWaypoint(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target is not IUnitEntity facingEntity)
                return false;

            PositionalRequirementEntry entry = gameTableManager.PositionalRequirement.GetEntry(objectId);
            if (entry == null)
                return false;

            return PositionalPrerequisiteHelper.MeetsDirectionalRequirement(player, comparison, facingEntity, entry);
        }
    }
}
