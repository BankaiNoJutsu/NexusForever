using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 195: "mounts can be used in the player's zone". Equal returns the flag, NotEqual its negation, any other comparison false; ObjectId and Value are not read.
    /// The rule and its evidence are on <see cref="ZoneVehicleUsage"/>. The check previously returned true unconditionally. Status: Implemented, inferred for the flag meaning. No retail parity claim.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HoverboardUsage)]
    public class PrerequisiteCheckHoverboardUsage : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckHoverboardUsage(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return comparison == PrerequisiteComparison.NotEqual;

            return PrerequisiteCompare.CompareFlag(comparison, ZoneVehicleUsage.IsAllowed(player.Zone, ZoneVehicleUsage.DenyMount, gameTableManager));
        }
    }
}
