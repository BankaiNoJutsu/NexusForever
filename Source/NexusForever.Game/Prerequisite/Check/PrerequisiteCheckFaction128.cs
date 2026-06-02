using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 128: live vtable <c>+0x68</c> <c>Prerequisite_CheckFaction</c> (<c>14049c720</c>)
    /// uses <c>PlayerFactionService_IsFactionOrAncestor</c>; handler table[128] is stub.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Faction128)]
    public class PrerequisiteCheckFaction128 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint matches = FactionPrerequisiteHelper.IsFactionOrAncestor(player.Faction1, value) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, matches, objectId);
        }
    }
}
