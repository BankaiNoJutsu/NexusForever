using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 4: handler table <c>14049d3f0</c> compares entity faction id (player
    /// <c>Faction1</c> proxy). Distinct from type 128 <see cref="PrerequisiteType.Faction128"/>
    /// (<c>Prerequisite_CheckFaction</c> <c>14049c720</c>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Faction)]
    public class PrerequisiteCheckFaction : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return PrerequisiteCompare.Compare(comparison, (uint)player.Faction1, value);
        }
    }
}
