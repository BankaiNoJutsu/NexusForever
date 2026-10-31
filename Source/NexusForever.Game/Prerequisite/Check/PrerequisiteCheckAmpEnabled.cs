using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Build16042 live type227 passes objectId to the active-spec AMP lookup
    /// (14049d760 -> 1403cdf50), independently of unlocked-AMP type231.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.EldanAugmentation227)]
    public class PrerequisiteCheckAmpEnabled : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player?.SpellManager == null || parameters?.Target != null && !ReferenceEquals(parameters.Target, player))
                return false;

            bool enabled = objectId <= ushort.MaxValue && player.SpellManager.IsAmpEnabled((ushort)objectId);
            return comparison switch
            {
                PrerequisiteComparison.Equal => enabled,
                PrerequisiteComparison.NotEqual => !enabled,
                _ => false
            };
        }
    }
}
