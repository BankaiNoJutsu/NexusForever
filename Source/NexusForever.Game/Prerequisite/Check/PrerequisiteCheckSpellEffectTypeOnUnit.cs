using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 105: evaluator case <c>0x69</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>), handled inline.
    /// </summary>
    /// <remarks>
    /// Native body (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_105_SPELL_EFFECT_TYPE_20261110.md): walks the node list at entity <c>+0x15d0</c> and
    /// compares each node's virtual <c>+0x38</c> result with the slot's <b>ObjectId</b> word, not its Value word. Comparison 1 returns whether a node matched,
    /// 2 its negation, any other comparison false; a missing entity is always false. All 7 retail slots carry the id in ObjectId (0, 3, 31, 83, 100, 112) with Value 0.
    /// That the compared id is a <see cref="SpellEffectType"/> is correlated from those values (ForcedMove, Disguise, Stealth, MimicDisguise, DisguiseOutfit), not read natively.
    /// <c>14049fd50</c>, previously cited here, is the type 52 Path handler. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.SpellEffectTypeOnUnit)]
    public class PrerequisiteCheckSpellEffectTypeOnUnit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IUnitEntity unit, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            if (unit == null)
                return false;

            return Evaluate(comparison, objectId, effectType => SpellPrerequisiteHelper.HasActiveSpellEffectType(unit, effectType));
        }

        /// <summary>
        /// Native comparison rule for type 105: the effect type comes from ObjectId; only Equal and NotEqual can be met.
        /// </summary>
        internal static bool Evaluate(PrerequisiteComparison comparison, uint objectId, Func<SpellEffectType, bool> hasActiveEffectType)
        {
            bool hasEffect = hasActiveEffectType((SpellEffectType)objectId);
            return comparison switch
            {
                PrerequisiteComparison.Equal    => hasEffect,
                PrerequisiteComparison.NotEqual => !hasEffect,
                _                               => false
            };
        }
    }
}
