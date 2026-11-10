using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 241: live vtable <c>+0x638</c> checks NPC target and active housing
    /// residence identity at world client <c>+0x7500/+0x7508</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.HousingResidenceLoaded)]
    public class PrerequisiteCheckHousingResidenceLoaded : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Native (evaluator case 0xf1) only requires the primary unit to be a player (0x14) or ghost (0x17) and never receives the second unit (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // The previous gate required a non-player target, which is the type test read backwards.
            if (player == null)
                return false;

            IResidence residence = player.ResidenceManager?.Residence;
            bool loaded = residence != null;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => loaded,
                PrerequisiteComparison.NotEqual => !loaded,
                _                               => false
            };
        }
    }
}
