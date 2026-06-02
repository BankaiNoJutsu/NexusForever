using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 105: handler table <c>14049fd50</c>; live case <c>0x69</c> walks active spell-effect
    /// nodes at entity <c>+0x15d0</c> for matching <see cref="SpellEffectType"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellEffectTypeOnUnit)]
    public class PrerequisiteCheckSpellEffectTypeOnUnit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            uint hasEffect = SpellPrerequisiteHelper.HasActiveSpellEffectType(unit, (SpellEffectType)value) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, hasEffect, 1u);
        }
    }
}
