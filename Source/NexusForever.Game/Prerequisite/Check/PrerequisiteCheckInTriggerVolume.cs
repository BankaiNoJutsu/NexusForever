using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 27: handler table <c>14049dc50</c>; live checks trigger-volume membership.
    /// NF proxies via <see cref="WorldLocation2Entry"/> horizontal radius + vertical clamp for
    /// <c>objectId0</c> (WorldLocation2 id used by volume triggers on the server).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InTriggerVolume)]
    public class PrerequisiteCheckInTriggerVolume : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckInTriggerVolume(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint worldLocationId = objectId != 0u ? objectId : value;
            float padding = player.HitRadius * 0.5f;
            bool inside = WorldLocationPrerequisiteHelper.IsPlayerInside(player, worldLocationId, gameTableManager, padding);
            uint insideVolume = inside ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, insideVolume, 1u);
        }
    }
}
