using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 185: live <c>Prerequisite_CheckGuildPerk</c> (<c>14049eae0</c>) tests guild perk
    /// bitmask at record <c>+0x42</c> for <c>value0</c> perk index. NF guild runtime does not model
    /// unlocked perks yet — returns false until <c>GuildData.UnlockedPerks</c> is wired on server.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.GuildPerk)]
    public class PrerequisiteCheckGuildPerk : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return PrerequisiteCompare.Compare(comparison, 0u, value);
        }
    }
}
