using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 188: live <c>Prerequisite_CheckWarplotCirclePerk</c> (<c>14049ebf0</c>) circle-guild
    /// perk bitmask; handler table[188] is <c>_purecall</c>. NF trace stub until warplot circle perk runtime exists.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.WarplotPermission)]
    public class PrerequisiteCheckWarplotPermission : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return PrerequisiteCompare.Compare(comparison, 0u, value);
        }
    }
}
