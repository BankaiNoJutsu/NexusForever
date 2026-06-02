using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 81: handler table <c>14049efc0</c> (<c>Prerequisite_CheckWrongSpellMechanic</c>);
    /// scans active spell target-mechanic flags on the unit (entity <c>+0x80</c> type filter for
    /// <c>0x14</c>/<c>0x17</c> in live helper). NF uses the same overlap probe as type 92;
    /// <see cref="PrerequisiteComparison"/> in data chooses required vs forbidden overlap.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.WrongSpellMechanic)]
    public class PrerequisiteCheckWrongSpellMechanic : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            uint hasOverlap = SpellPrerequisiteHelper.HasActiveSpellTargetMechanic(unit, value) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, hasOverlap, 1u);
        }
    }
}
