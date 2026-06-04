using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 246: retail rows compare <c>value0</c> against an Item2 id already present on the character.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.DoesNotOwnAccountItemOnCharacter)]
    public class PrerequisiteCheckDoesNotOwnAccountItemOnCharacter : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint item2Id = value != 0u ? value : objectId;
            uint current = AccountItemOwnedCount.IsItem2OnCharacter(player, item2Id) ? item2Id : 0u;
            return PrerequisiteCompare.Compare(comparison, current, item2Id);
        }
    }
}
