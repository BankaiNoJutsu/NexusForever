using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;

namespace NexusForever.Game.Combat
{
    public sealed class DamageCalculator : IDamageCalculator
    {
        #region Dependency Injection

        private readonly ILogger<DamageCalculator> log;
        private readonly IGameTableManager gameTableManager;

        public DamageCalculator(
            ILogger<DamageCalculator> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        /// <summary>
        /// Returns the calculated damage and updates the referenced <see cref="SpellTargetInfo.SpellTargetEffectInfo"/> appropriately.
        /// </summary>
        /// <remarks>
        /// The current combat log model records target-side damage; reflected damage is handled by separate effects/procs.
        /// </remarks>
        public void CalculateDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info);

            IDamageDescription damageDescription = new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType        = info.Entry.DamageType,
                ThreatMultiplier  = ResolveThreatMultiplier(spell, info.Entry),
                CombatResult      = CombatResult.Hit
            };

            var castData = new CombatLogCastData
            {
                CasterId     = attacker.Guid,
                TargetId     = victim.Guid,
                SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                CombatResult = CombatResult.Hit
            };

            if (CalculateDeflect(attacker, victim))
            {
                info.DropEffect = true;
                info.AddCombatLog(new CombatLogDeflect
                    {
                        BMultiHit = false,
                        CastData  = castData
                    });
                return;
            }

            uint damage = CalculateBaseDamage(attacker, victim, effect);
            damageDescription.RawDamage       = damage;
            damageDescription.RawScaledDamage = damage;

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics damage-input spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} effectType={EffectType} attacker={AttackerId} victim={VictimId} multiplier={TypeMultiplier} baseValue={TypeBaseValue} parameters={Parameters}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    info.Entry.EffectType,
                    attacker.Guid,
                    victim.Guid,
                    effect.Damage?.TypeMultiplier,
                    effect.Damage?.TypeBaseValue,
                    effect.FormatParameters());
            }

            damage = CalculateBaseDamageVariance(damage);

            damage = GetDamageAfterArmorMitigation(victim, info.Entry.DamageType, damage);
            damage = ApplyDamageTakenMultiplier(victim, info.Entry.DamageType, damage);

            if (CalculateCrit(ref damage, attacker, victim))
                damageDescription.CombatResult = CombatResult.Critical;

            CalculateGlance(ref damage, attacker, victim);

            uint absorbedAmount = victim.ConsumeAbsorption(damage, info.Entry.DamageType);
            damage -= absorbedAmount;
            damageDescription.AbsorbedAmount = absorbedAmount;

            uint shieldedAmount = CalculateShieldAmount(damage, victim.Shield, victim.GetPropertyValue(Property.ShieldMitigationMax));
            damage -= shieldedAmount;
            damageDescription.ShieldAbsorbAmount = shieldedAmount;

            damageDescription.AdjustedDamage = damage;

            info.AddDamage(damageDescription);
            attacker.ProbeProcEvent("damage-dealt", ProcTriggerEventCandidate.DealDamage, attacker, victim, spell, info, damageDescription, "after-calculate-before-apply");
            victim.ProbeProcEvent("damage-received", ProcTriggerEventCandidate.ReceiveDamage, attacker, victim, spell, info, damageDescription, "after-calculate-before-apply");
            ProbeReceiveDamageSchoolVariant(attacker, victim, spell, info, damageDescription);

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics damage-output spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} rawDamage={RawDamage} adjustedDamage={AdjustedDamage} absorbed={Absorbed} shieldAbsorb={ShieldAbsorb} combatResult={CombatResult}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    damageDescription.RawDamage,
                    damageDescription.AdjustedDamage,
                    damageDescription.AbsorbedAmount,
                    damageDescription.ShieldAbsorbAmount,
                    damageDescription.CombatResult);
            }
        }

        public void CalculateHealing(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info);

            uint rawHeal = CalculateBaseDamage(caster, target, effect);
            rawHeal = (uint)(rawHeal
                * caster.GetPropertyValue(Property.HealingMultiplierOutgoing)
                * target.GetPropertyValue(Property.HealingMultiplierIncoming));

            long missingHealth = Math.Max(0L, (long)target.MaxHealth - target.Health);
            uint availableHealing = (uint)Math.Min(rawHeal, missingHealth);
            uint absorbedHeal = target.ConsumeHealingAbsorption(availableHealing);
            uint adjustedHeal = availableHealing - absorbedHeal;
            uint overheal = rawHeal - availableHealing;

            IDamageDescription healDescription = new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType       = DamageType.Heal,
                RawDamage        = rawHeal,
                RawScaledDamage  = rawHeal,
                AbsorbedAmount   = absorbedHeal,
                AdjustedDamage   = adjustedHeal,
                OverkillAmount   = overheal,
                ThreatMultiplier = 1f,
                CombatResult     = CombatResult.Hit
            };

            info.AddDamage(healDescription);
            info.AddCombatLog(new CombatLogHeal
            {
                HealAmount = adjustedHeal,
                Overheal   = overheal,
                Absorption = absorbedHeal,
                EffectType = info.Entry.EffectType,
                CastData   = new CombatLogCastData
                {
                    CasterId     = caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Hit
                }
            });
            if (absorbedHeal > 0u)
                info.AddCombatLog(new CombatLogHealingAbsorption
                {
                    Amount   = absorbedHeal,
                    CastData = CreateCastData(caster, target, spell)
                });

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics healing-output spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} rawHeal={RawHeal} adjustedHeal={AdjustedHeal} overheal={Overheal} absorbedHeal={AbsorbedHeal}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    healDescription.RawDamage,
                    healDescription.AdjustedDamage,
                    healDescription.OverkillAmount,
                    healDescription.AbsorbedAmount);
            }

            uint? triggerEvent = caster.Guid != target.Guid ? ProcTriggerEventCandidate.HealOther : null;
            caster.ProbeProcEvent(caster.Guid == target.Guid ? "heal-self" : "heal-other", triggerEvent, caster, target, spell, info, healDescription, "after-calculate-before-apply");
            if (caster.Guid != target.Guid)
                target.ProbeProcEvent("heal-received", null, caster, target, spell, info, healDescription, "after-calculate-before-apply");
        }

        public void CalculateShieldHealing(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info);

            uint rawHeal = CalculateBaseDamage(caster, target, effect);
            rawHeal = (uint)(rawHeal
                * caster.GetPropertyValue(Property.HealingMultiplierOutgoing)
                * target.GetPropertyValue(Property.HealingMultiplierIncoming));

            long missingShield = Math.Max(0L, (long)target.MaxShieldCapacity - target.Shield);
            uint adjustedHeal = (uint)Math.Min(rawHeal, missingShield);
            uint overheal = rawHeal - adjustedHeal;

            IDamageDescription healDescription = new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType      = DamageType.HealShields,
                RawDamage       = rawHeal,
                RawScaledDamage = rawHeal,
                AdjustedDamage  = adjustedHeal,
                OverkillAmount  = overheal,
                ThreatMultiplier = 1f,
                CombatResult    = CombatResult.Hit
            };

            info.AddDamage(healDescription);
            info.AddCombatLog(new CombatLogHeal
            {
                HealAmount = adjustedHeal,
                Overheal   = overheal,
                EffectType = info.Entry.EffectType,
                CastData   = CreateCastData(caster, target, spell)
            });

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics shield-healing-output spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} rawHeal={RawHeal} adjustedHeal={AdjustedHeal} overheal={Overheal}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    healDescription.RawDamage,
                    healDescription.AdjustedDamage,
                    healDescription.OverkillAmount);
            }

            uint? triggerEvent = caster.Guid != target.Guid ? ProcTriggerEventCandidate.HealOther : null;
            caster.ProbeProcEvent(caster.Guid == target.Guid ? "shield-heal-self" : "shield-heal-other", triggerEvent, caster, target, spell, info, healDescription, "after-calculate-before-apply");
            if (caster.Guid != target.Guid)
                target.ProbeProcEvent("shield-heal-received", null, caster, target, spell, info, healDescription, "after-calculate-before-apply");
        }

        public void CalculateShieldDamage(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info);

            uint rawDamage = CalculateBaseDamage(attacker, victim, effect);
            uint adjustedDamage = Math.Min(rawDamage, victim.Shield);
            uint overkill = rawDamage - adjustedDamage;

            IDamageDescription damageDescription = new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
            {
                DamageType         = info.Entry.DamageType,
                RawDamage          = rawDamage,
                RawScaledDamage    = rawDamage,
                ShieldAbsorbAmount = adjustedDamage,
                AdjustedDamage     = 0u,
                OverkillAmount     = overkill,
                ThreatMultiplier   = ResolveThreatMultiplier(spell, info.Entry),
                CombatResult       = CombatResult.Hit
            };

            info.AddDamage(damageDescription);
            info.AddCombatLog(new CombatLogDamageShield
            {
                MitigatedDamage = adjustedDamage,
                RawDamage       = rawDamage,
                Shield          = adjustedDamage,
                Overkill        = overkill,
                BPeriodic       = SpellHandler.IsPeriodicDamageLog(info.Entry),
                DamageType      = info.Entry.DamageType,
                EffectType      = info.Entry.EffectType,
                CastData        = CreateCastData(attacker, victim, spell)
            });

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics shield-damage-output spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} rawDamage={RawDamage} adjustedShieldDamage={AdjustedShieldDamage} overkill={Overkill}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    damageDescription.RawDamage,
                    damageDescription.ShieldAbsorbAmount,
                    damageDescription.OverkillAmount);
            }

            attacker.ProbeProcEvent("shield-damage-dealt", ProcTriggerEventCandidate.DealDamage, attacker, victim, spell, info, damageDescription, "after-calculate-before-apply");
            victim.ProbeProcEvent("shield-damage-received", ProcTriggerEventCandidate.ReceiveDamage, attacker, victim, spell, info, damageDescription, "after-calculate-before-apply");
            ProbeReceiveDamageSchoolVariant(attacker, victim, spell, info, damageDescription);
        }

        private static void ProbeReceiveDamageSchoolVariant(IUnitEntity attacker, IUnitEntity victim, ISpell spell, ISpellTargetEffectInfo info, IDamageDescription damageDescription)
        {
            if (spell == null)
                return;

            if (!ProcTriggerEventCandidate.TryGetReceiveDamageSchoolTriggerEvent(spell.Parameters.SpellInfo.BaseInfo.School, out uint triggerEvent)
                || !ProcTriggerEventCandidate.TryGetConservativeLabel(triggerEvent, out string eventName))
                return;

            victim.ProbeProcEvent(eventName, triggerEvent, attacker, victim, spell, info, damageDescription, "after-calculate-before-apply");
        }

        public uint CalculateAbsorption(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info);
            uint amount = CalculateBaseDamage(caster, target, effect);
            if (amount == 0u && effect.Absorption is { TypeBaseValue: > 0f })
                amount = (uint)MathF.Ceiling(effect.Absorption.TypeBaseValue);

            if (amount == 0u)
                return 0u;

            info.AddCombatLog(new CombatLogAbsorption
            {
                AbsorptionAmount = amount,
                CastData         = CreateCastData(caster, target, spell)
            });

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics absorption-output spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} amount={Amount} multiplier={TypeMultiplier} baseValue={TypeBaseValue} absorptionType={AbsorptionType} parameters={Parameters}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    amount,
                    effect.Absorption?.TypeMultiplier,
                    effect.Absorption?.TypeBaseValue,
                    effect.Absorption?.AbsorptionType,
                    effect.FormatParameters());
            }

            return amount;
        }

        public uint CalculateHealingAbsorption(IUnitEntity caster, IUnitEntity target, ISpell spell, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation effect = SpellEffectInterpreter.Interpret(info);
            uint amount = CalculateBaseDamage(caster, target, effect);
            if (amount == 0u && effect.HealingAbsorption is { TypeBaseValue: > 0f })
                amount = (uint)MathF.Ceiling(effect.HealingAbsorption.TypeBaseValue);

            if (amount == 0u)
                return 0u;

            info.AddCombatLog(new CombatLogHealingAbsorption
            {
                Amount   = amount,
                CastData = CreateCastData(caster, target, spell)
            });

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace(
                    "SpellDiagnostics healing-absorption-output spell4Id={Spell4Id} castingId={CastingId} spell4EffectId={Spell4EffectId} amount={Amount} multiplier={TypeMultiplier} baseValue={TypeBaseValue} mode={Mode} parameters={Parameters}",
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    info.Entry.Id,
                    amount,
                    effect.HealingAbsorption?.TypeMultiplier,
                    effect.HealingAbsorption?.TypeBaseValue,
                    effect.HealingAbsorption?.Mode,
                    effect.FormatParameters());
            }

            return amount;
        }

        private static CombatLogCastData CreateCastData(IUnitEntity caster, IUnitEntity target, ISpell spell)
        {
            return new CombatLogCastData
            {
                CasterId     = caster.Guid,
                TargetId     = target.Guid,
                SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                CombatResult = CombatResult.Hit
            };
        }

        private static float ResolveThreatMultiplier(ISpell spell, Spell4EffectsEntry damageEntry)
        {
            float multiplier = float.IsFinite(damageEntry.ThreatMultiplier) && damageEntry.ThreatMultiplier > 0f
                ? damageEntry.ThreatMultiplier
                : 1f;

            foreach (Spell4EffectsEntry effectEntry in spell.Parameters.SpellInfo.Effects)
            {
                if (effectEntry.EffectType != SpellEffectType.ThreatModification)
                    continue;

                if (effectEntry.DataBits00 != 130u)
                    continue;

                if (effectEntry.DelayTime != damageEntry.DelayTime)
                    continue;

                if (!float.IsFinite(effectEntry.ThreatMultiplier) || effectEntry.ThreatMultiplier < 0f)
                    continue;

                multiplier *= effectEntry.ThreatMultiplier;
            }

            return multiplier;
        }

        /// <summary>
        /// Get base damage value for the given <see cref="IUnitEntity"/> with the provided parameter data from the <see cref="Spell4EffectsEntry"/>.
        /// </summary>
        private uint CalculateBaseDamage(IUnitEntity caster, IUnitEntity target, SpellEffectInterpretation effect)
        {
            Spell4EffectsEntry entry = effect.Entry;

            float basePropertyDamage = CalculateBasePropertyDamage(caster, effect);
            float baseEntityDamage   = CalculateBaseEntityDamage(caster, target, effect);

            float typeMultiplier = 1f;
            float typeBaseDamage = 0;
            if (effect.Damage != null)
            {
                typeMultiplier = effect.Damage.TypeMultiplier;
                typeBaseDamage = effect.Damage.TypeBaseValue;
            }

            float baseDamage = basePropertyDamage + ((typeBaseDamage + baseEntityDamage) * typeMultiplier);

            float propertyMultiplier = caster.GetProperty((Property)(entry.DamageType + 140)).Value;

            return (uint)(propertyMultiplier * baseDamage);
        }

        private float CalculateBasePropertyDamage(IUnitEntity caster, SpellEffectInterpretation effect)
        {
            float GetProperty(Property property)
            {
                return caster.GetProperty(property)?.Value ?? 0f;
            }

            GameFormulaEntry forumulaEntry = gameTableManager.GameFormula.GetEntry(1266);

            float value = 0f;
            foreach (SpellEffectParameter parameter in effect.Parameters)
            {
                float intermediateValue = 0f;
                switch (parameter.Type)
                {
                    case SpellEffectParameterType.Brutality:
                        intermediateValue = GetProperty(Property.Strength);
                        break;
                    case SpellEffectParameterType.Finesse:
                        intermediateValue = GetProperty(Property.Dexterity);
                        break;
                    case SpellEffectParameterType.Tech:
                        intermediateValue = GetProperty(Property.Technology);
                        break;
                    case SpellEffectParameterType.Moxie:
                        intermediateValue = GetProperty(Property.Magic);
                        break;
                    case SpellEffectParameterType.Insight:
                        intermediateValue = GetProperty(Property.Wisdom);
                        break;
                    case SpellEffectParameterType.Grit:
                        intermediateValue = GetProperty(Property.Stamina);
                        break;
                    // client defaults to a value of 0.25f if the game table entry is missing
                    case SpellEffectParameterType.AssaultPower:
                        intermediateValue = ApplyPowerCoefficient(GetProperty(Property.AssaultRating), forumulaEntry?.Datafloat0);
                        break;
                    case SpellEffectParameterType.SupportPower:
                        intermediateValue = ApplyPowerCoefficient(GetProperty(Property.SupportRating), forumulaEntry?.Datafloat01);
                        break;
                }

                value += intermediateValue * parameter.Value;
            }

            if (value >= 0f)
                return MathF.Ceiling(value);
            else
                return MathF.Floor(value);
        }

        private float CalculateBaseEntityDamage(IUnitEntity caster, IUnitEntity target, SpellEffectInterpretation effect)
        {
            float value = 0f;
            foreach (SpellEffectParameter parameter in effect.Parameters)
            {
                float intermediateValue = 0f;
                switch (parameter.Type)
                {
                    case SpellEffectParameterType.TargetMaxHealth:
                        intermediateValue = target.MaxHealth;
                        break;
                    case SpellEffectParameterType.CasterMaxHealth:
                        intermediateValue = caster.MaxHealth;
                        break;
                    case SpellEffectParameterType.CasterShieldCapacity:
                        intermediateValue = caster.Shield;
                        break;
                    case SpellEffectParameterType.TargetShieldCapacity:
                        intermediateValue = target.Shield;
                        break;
                    case SpellEffectParameterType.CasterMaxShieldCapacity:
                        intermediateValue = caster.MaxShieldCapacity;
                        break;
                    case SpellEffectParameterType.TargetMaxShieldCapacity:
                        intermediateValue = target.MaxShieldCapacity;
                        break;
                    case SpellEffectParameterType.ItemBudget:
                        intermediateValue = parameter.Value;
                        break;
                    case SpellEffectParameterType.TargetCurrentHealth:
                        intermediateValue = target.Health;
                        break;
                    case SpellEffectParameterType.TargetMissingHealth:
                        intermediateValue = (target.MaxHealth - target.Health);
                        break;
                    case SpellEffectParameterType.TargetMissingShields:
                        intermediateValue = (target.MaxShieldCapacity - target.Shield);
                        break;
                    case SpellEffectParameterType.CasterCurrentHealth:
                        intermediateValue = caster.Health;
                        break;
                    case SpellEffectParameterType.CasterMissingHealth:
                        intermediateValue = (caster.MaxHealth - caster.Health);
                        break;
                    case SpellEffectParameterType.CasterMissingShields:
                        intermediateValue = (caster.MaxShieldCapacity - caster.Shield);
                        break;
                    case SpellEffectParameterType.PerLevel:
                        intermediateValue = caster.Level;
                        break;
                }

                value += intermediateValue * parameter.Value;
            }

            return value;
        }

        internal static float ApplyPowerCoefficient(float rating, float? coefficient)
        {
            return rating * (coefficient ?? 0.25f);
        }

        private uint CalculateBaseDamageVariance(uint damage)
        {
            return (uint)(damage * (Random.Shared.Next(95, 103) / 100f));
        }

        private uint GetDamageAfterArmorMitigation(IUnitEntity victim, DamageType damageType, uint damage)
        {
            GameFormulaEntry armorFormulaEntry = gameTableManager.GameFormula.GetEntry(1234);
            return ApplyArmorMitigation(
                damage,
                damageType,
                victim.Level,
                victim.GetPropertyValue(Property.Armor),
                victim.GetPropertyValue(Property.DamageMitigationPctOffsetPhysical),
                victim.GetPropertyValue(Property.DamageMitigationPctOffsetTech),
                victim.GetPropertyValue(Property.DamageMitigationPctOffsetMagic),
                armorFormulaEntry);
        }

        internal static uint ApplyArmorMitigation(
            uint damage,
            DamageType damageType,
            uint victimLevel,
            float victimArmor,
            float physicalOffset,
            float techOffset,
            float magicOffset,
            GameFormulaEntry armorFormulaEntry)
        {
            if (damage == 0u || armorFormulaEntry == null || victimLevel == 0u)
                return damage;

            float maximumArmorMitigation = armorFormulaEntry.Dataint01 * 0.01f;
            float mitigationPct = (armorFormulaEntry.Datafloat0 / victimLevel * armorFormulaEntry.Datafloat01) * victimArmor / 100f;
            mitigationPct += damageType switch
            {
                DamageType.Physical => physicalOffset,
                DamageType.Tech     => techOffset,
                DamageType.Magic    => magicOffset,
                _                   => 0f
            };

            if (!float.IsFinite(mitigationPct) || mitigationPct <= 0f || maximumArmorMitigation <= 0f)
                return damage;

            return (uint)Math.Round(damage * (1f - Math.Clamp(mitigationPct, 0f, maximumArmorMitigation)));
        }

        private static uint ApplyDamageTakenMultiplier(IUnitEntity victim, DamageType damageType, uint damage)
        {
            Property? property = damageType switch
            {
                DamageType.Physical => Property.DamageTakenMultiplierPhysical,
                DamageType.Tech     => Property.DamageTakenMultiplierTech,
                DamageType.Magic    => Property.DamageTakenMultiplierMagic,
                _                   => null
            };

            if (property == null)
                return damage;

            float multiplier = victim.GetPropertyValue(property.Value);
            if (!float.IsFinite(multiplier))
                return damage;

            double adjustedDamage = damage * Math.Max(0d, multiplier);
            if (adjustedDamage >= uint.MaxValue)
                return uint.MaxValue;

            return (uint)adjustedDamage;
        }

        private bool IsSuccessfulChance(float percentage)
        {
            return Random.Shared.Next(1, 10000) <= percentage * 10000f;
        }

        /// <summary>
        /// Calculates and returns the shielded amount of damage.
        /// </summary>
        internal static uint CalculateShieldAmount(uint damage, uint shield, float shieldMitigationMax)
        {
            if (damage == 0u || shield == 0u || !float.IsFinite(shieldMitigationMax) || shieldMitigationMax <= 0f)
                return 0u;

            double maxShieldAmount = damage * (double)shieldMitigationMax;
            uint cappedShieldAmount = maxShieldAmount >= uint.MaxValue
                ? uint.MaxValue
                : (uint)maxShieldAmount;

            uint shieldedAmount = Math.Min(shield, cappedShieldAmount);

            return Math.Min(damage, shieldedAmount);
        }

        /// <summary>
        /// Returns whether this attack was deflected.
        /// </summary>
        /// <remarks>Calculates chance to deflect an attack, avoiding all damage from that attack.</remarks>
        private bool CalculateDeflect(IUnitEntity attacker, IUnitEntity victim)
        {
            float deflectChance = CalculateEffectiveDeflectChance(attacker, victim);
            if (deflectChance <= 0f)
                return false;

            return IsSuccessfulChance(deflectChance);
        }

        internal float CalculateEffectiveDeflectChance(IUnitEntity attacker, IUnitEntity victim)
        {
            float deflectChance = MathF.Max(0f, GetRatingPercentMod(Property.RatingAvoidIncrease, victim));
            float strikethroughChance = MathF.Max(0f, GetRatingPercentMod(Property.RatingAvoidReduce, attacker));

            return MathF.Max(0f, deflectChance - strikethroughChance);
        }

        /// <summary>
        /// Returns whether this attack crit, and if so, modifies the referenced damage value appropriately.
        /// </summary>
        private bool CalculateCrit(ref uint damage, IUnitEntity attacker, IUnitEntity victim)
        {
            float critRate = GetRatingPercentMod(Property.RatingCritChanceIncrease, attacker);
            if (critRate <= 0f)
                return false;

            bool crit = IsSuccessfulChance(critRate);
            if (crit)
                damage = (uint)Math.Round(damage * GetRatingPercentMod(Property.RatingCritSeverityIncrease, attacker));

            return crit;
        }

        /// <summary>
        /// Returns whether this attack was glanced, and if so, modifies the referenced damage value appropriately.
        /// </summary>
        private bool CalculateGlance(ref uint damage, IUnitEntity attacker, IUnitEntity victim)
        {
            float glanceChance = GetRatingPercentMod(Property.RatingGlanceChance, victim);
            if (glanceChance <= 0f)
                return false;

            bool glance = IsSuccessfulChance(glanceChance);
            if (glance)
                damage = (uint)Math.Round((float)(damage * (1 - GetRatingPercentMod(Property.RatingGlanceAmount, victim))));

            return glance;
        }

        private float GetRatingPercentMod(Property property, IUnitEntity entity)
        {
            GameFormulaEntry gameFormula = null;

            switch (property)
            {
                case Property.Armor:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1234);
                    break;
                case Property.RatingArmorPierce:
                case Property.IgnoreArmorBase:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1269);
                    break;
                case Property.RatingAvoidReduce: // Strikethrough
                case Property.BaseAvoidReduceChance:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1230);
                    break;
                case Property.RatingAvoidIncrease: // Deflect
                case Property.BaseAvoidChance:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1235);
                    break;
                case Property.RatingCriticalMitigation:
                case Property.BaseCriticalMitigation:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1236);
                    break;
                case Property.RatingCritChanceDecrease: // Deflect Crit Chance
                case Property.BaseAvoidCritChance:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1236);
                    break;
                case Property.RatingCritChanceIncrease:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1231);
                    break;
                case Property.RatingCritSeverityIncrease:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1232);
                    break;
                case Property.CCDurationModifier:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1274);
                    break;
                case Property.RatingDamageReflectAmount:
                case Property.BaseDamageReflectAmount:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1272);
                    break;
                case Property.RatingDamageReflectChance:
                case Property.BaseDamageReflectChance:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1241);
                    break;
                case Property.RatingFocusRecovery:
                case Property.BaseFocusRecoveryInCombat:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1237);
                    break;
                case Property.RatingGlanceAmount:
                case Property.BaseGlanceAmount:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1271);
                    break;
                case Property.RatingGlanceChance:
                case Property.BaseGlanceChance:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1245);
                    break;
                case Property.RatingIntensity:
                case Property.BaseIntensity:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1243);
                    break;
                case Property.RatingLifesteal:
                case Property.BaseLifesteal:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1233);
                    break;
                case Property.RatingMultiHitAmount:
                case Property.BaseMultiHitAmount:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1270);
                    break;
                case Property.RatingMultiHitChance:
                case Property.BaseMultiHitChance:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1240);
                    break;
                case Property.RatingVigor:
                case Property.BaseVigor:
                    gameFormula = gameTableManager.GameFormula.GetEntry(1244);
                    break;
                default:
                    log.LogWarning($"Unhandled Property in calculating Percentage from Rating: {property}");
                    break;
            }

            // Return a decimal representing the % applied by this rating.
            float ratingMod = ((gameFormula.Datafloat0 / entity.Level) * gameFormula.Datafloat01) * entity.GetPropertyValue(property);
            return Math.Min((GetBasePercentMod(property, entity) + ratingMod) * 100f, gameFormula.Dataint01) / 100f;
        }

        private float GetBasePercentMod(Property property, IUnitEntity entity)
        {
            float baseValue = 0f;
            switch (property)
            {
                case Property.RatingArmorPierce:
                case Property.IgnoreArmorBase:
                    baseValue = entity.GetPropertyValue(Property.IgnoreArmorBase);
                    break;
                case Property.RatingAvoidReduce: // Strikethrough
                case Property.BaseAvoidReduceChance:
                    baseValue = entity.GetPropertyValue(Property.BaseAvoidReduceChance);
                    break;
                case Property.RatingAvoidIncrease: // Deflect
                case Property.BaseAvoidChance:
                    baseValue = entity.GetPropertyValue(Property.BaseAvoidChance);
                    break;
                case Property.RatingCriticalMitigation:
                case Property.BaseCriticalMitigation:
                    baseValue = entity.GetPropertyValue(Property.BaseCriticalMitigation);
                    break;
                case Property.RatingCritChanceDecrease: // Deflect Crit Chance
                case Property.BaseAvoidCritChance:
                    baseValue = entity.GetPropertyValue(Property.BaseAvoidCritChance);
                    break;
                case Property.RatingCritChanceIncrease:
                case Property.BaseCritChance:
                    baseValue = entity.GetPropertyValue(Property.BaseCritChance);
                    break;
                case Property.RatingCritSeverityIncrease:
                    baseValue = entity.GetPropertyValue(Property.CriticalHitSeverityMultiplier);
                    break;
                case Property.RatingDamageReflectAmount:
                case Property.BaseDamageReflectAmount:
                    baseValue = entity.GetPropertyValue(Property.BaseDamageReflectAmount);
                    break;
                case Property.RatingDamageReflectChance:
                case Property.BaseDamageReflectChance:
                    baseValue = entity.GetPropertyValue(Property.BaseDamageReflectChance);
                    break;
                case Property.RatingFocusRecovery:
                case Property.BaseFocusRecoveryInCombat:
                    baseValue = entity.GetPropertyValue(Property.BaseFocusRecoveryInCombat);
                    break;
                case Property.RatingGlanceAmount:
                case Property.BaseGlanceAmount:
                    baseValue = entity.GetPropertyValue(Property.BaseGlanceAmount);
                    break;
                case Property.RatingGlanceChance:
                case Property.BaseGlanceChance:
                    baseValue = entity.GetPropertyValue(Property.BaseGlanceChance);
                    break;
                case Property.RatingIntensity:
                case Property.BaseIntensity:
                    baseValue = entity.GetPropertyValue(Property.BaseIntensity);
                    break;
                case Property.RatingLifesteal:
                case Property.BaseLifesteal:
                    baseValue = entity.GetPropertyValue(Property.BaseLifesteal);
                    break;
                case Property.RatingMultiHitAmount:
                case Property.BaseMultiHitAmount:
                    baseValue = entity.GetPropertyValue(Property.BaseMultiHitAmount);
                    break;
                case Property.RatingMultiHitChance:
                case Property.BaseMultiHitChance:
                    baseValue = entity.GetPropertyValue(Property.BaseMultiHitChance);
                    break;
                case Property.RatingVigor:
                case Property.BaseVigor:
                    baseValue = entity.GetPropertyValue(Property.BaseVigor);
                    break;
                default:
                    log.LogWarning($"Unhandled Property in calculating Percentage from Base: {property}");
                    break;
            }

            return baseValue;
        }
    }
}
