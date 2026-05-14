using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Game.Abstract
{
    public interface IDamageCalculator
    {
        void CalculateDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info);
        void CalculateHealing(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info);
        void CalculateShieldHealing(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info);
        void CalculateShieldDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info);
        uint CalculateAbsorption(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info);
        uint CalculateHealingAbsorption(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info);
    }
}
