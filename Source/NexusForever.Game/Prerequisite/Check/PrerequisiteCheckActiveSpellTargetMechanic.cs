using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 92: live <c>Prerequisite_CheckActiveSpellTargetMechanic</c> (<c>14049d5b0</c>,
    /// vtable <c>+0x168</c>); handler table <c>14049f5b0</c>. Scans active spell effects for
    /// <see cref="Spell4TargetMechanicsEntry"/> flag overlap with <c>value0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActiveSpellTargetMechanic)]
    public class PrerequisiteCheckActiveSpellTargetMechanic : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            uint hasMechanic = SpellPrerequisiteHelper.HasActiveSpellTargetMechanic(unit, value) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, hasMechanic, 1u);
        }
    }
}
