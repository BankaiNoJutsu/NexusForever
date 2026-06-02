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
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
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
