using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Spell.Effect;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Spell
{
    public static partial class SpellHandler
    {
        [SpellEffectHandler(SpellEffectType.Damage)]
        public static void HandleEffectDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!spell.Caster.CanAttack(target))
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);
            if (info.DropEffect || info.Damage == null)
                return;

            target.TakeDamage(spell.Caster, info.Damage);
            AddDamageCombatLog(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.DistanceDependentDamage)]
        public static void HandleEffectDistanceDependentDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.DistributedDamage)]
        public static void HandleEffectDistributedDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        private static void AddDamageCombatLog(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IDamageDescription damage = info.Damage;
            if (damage == null)
                return;

            info.AddCombatLog(new CombatLogDamage
            {
                MitigatedDamage   = damage.AdjustedDamage,
                RawDamage         = damage.RawDamage,
                Shield            = damage.ShieldAbsorbAmount,
                Absorption        = damage.AbsorbedAmount,
                Overkill          = damage.OverkillAmount,
                Glance            = 0u,
                BTargetVulnerable = false,
                BKilled           = damage.KilledTarget,
                BPeriodic         = IsPeriodicDamageLog(info.Entry),
                DamageType        = info.Entry.DamageType,
                EffectType        = info.Entry.EffectType,
                CastData          = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = damage.CombatResult
                }
            });
        }

        internal static bool IsPeriodicDamageLog(Spell4EffectsEntry entry)
        {
            return entry.TickTime > 0u && entry.DurationTime > 0u;
        }

        [SpellEffectHandler(SpellEffectType.Transference)]
        public static void HandleEffectTransference(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectTransferenceSemantics transference = SpellEffectInterpreter.Interpret(info).Transference;
            if (transference == null)
                return;

            if (!target.CanAttack(spell.Caster))
            {
                SpellEffectDiagnostics.TraceTransference(spell, target, transference, 0u, 0u, 0u, 0u, false, "target-cannot-attack-caster");
                return;
            }

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);
            if (info.DropEffect || info.Damage == null)
                return;

            target.TakeDamage(spell.Caster, info.Damage);

            uint damageAmount = info.Damage.AdjustedDamage;
            uint rawHeal = CalculateTransferenceHealAmount(damageAmount, ResolveTransferenceRate(transference));
            uint appliedHeal = 0u;
            uint overheal = rawHeal;
            bool applied = false;
            string skippedReason = null;
            var healedUnits = new List<CombatLogTransference.CombatHealData>();

            if (rawHeal == 0u)
            {
                skippedReason = "zero-heal";
            }
            else if (spell.Caster.TryModifyVital(transference.HealedVital, rawHeal, out float appliedAmount, spell.Caster))
            {
                appliedHeal = appliedAmount > 0f ? (uint)MathF.Round(appliedAmount) : 0u;
                overheal = rawHeal - Math.Min(rawHeal, appliedHeal);
                applied = appliedHeal > 0u;
                skippedReason = applied ? null : "fully-overheal";
                healedUnits.Add(new CombatLogTransference.CombatHealData
                {
                    HealedUnitId = spell.Caster.Guid,
                    HealAmount   = appliedHeal,
                    Vital        = transference.HealedVital,
                    Overheal     = overheal,
                    Absorption   = 0u
                });
            }
            else
            {
                skippedReason = "unknown-heal-vital";
            }

            info.AddCombatLog(new CombatLogTransference
            {
                DamageAmount      = damageAmount,
                DamageType        = info.Entry.DamageType,
                Shield            = info.Damage.ShieldAbsorbAmount,
                Absorption        = info.Damage.AbsorbedAmount,
                Overkill          = info.Damage.OverkillAmount,
                GlanceAmount      = 0u,
                BTargetVulnerable = false,
                HealedUnits       = healedUnits
            });

            SpellEffectDiagnostics.TraceTransference(spell, target, transference, damageAmount, rawHeal, appliedHeal, overheal, applied, skippedReason);
        }

        [SpellEffectHandler(SpellEffectType.Heal)]
        public static void HandleEffectHeal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateHealing(spell.Caster, target, spell, info);

            target.ModifyHealth(info.Damage.AdjustedDamage, DamageType.Heal, spell.Caster);
        }

        [SpellEffectHandler(SpellEffectType.HealShields)]
        public static void HandleEffectHealShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateShieldHealing(spell.Caster, target, spell, info);

            target.Shield += info.Damage.AdjustedDamage;
        }

        [SpellEffectHandler(SpellEffectType.DamageShields)]
        public static void HandleEffectDamageShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.CanAttack(spell.Caster))
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateShieldDamage(spell.Caster, target, spell, info);

            target.Shield = target.Shield > info.Damage.ShieldAbsorbAmount
                ? target.Shield - info.Damage.ShieldAbsorbAmount
                : 0u;
        }

        [SpellEffectHandler(SpellEffectType.Absorption)]
        public static void HandleEffectAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectAbsorptionSemantics absorption = SpellEffectInterpreter.Interpret(info).Absorption;
            if (absorption == null || !target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            uint amount = damageCalculator.CalculateAbsorption(spell.Caster, target, spell, info);
            if (amount == 0u)
                return;

            target.AddAbsorption(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, amount, absorption.AbsorptionType);
            SpellEffectDiagnostics.TraceAbsorption(spell, target, absorption, amount, false);
        }

        [SpellEffectHandler(SpellEffectType.HealingAbsorption)]
        public static void HandleEffectHealingAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectHealingAbsorptionSemantics absorption = SpellEffectInterpreter.Interpret(info).HealingAbsorption;
            if (absorption == null || !target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            uint amount = damageCalculator.CalculateHealingAbsorption(spell.Caster, target, spell, info);
            if (amount == 0u)
                return;

            target.AddHealingAbsorption(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, amount);
            SpellEffectDiagnostics.TraceHealingAbsorption(spell, target, absorption, amount, false);
        }
    }
}
