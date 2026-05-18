using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Combat;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Spell
{
    public static class SpellEffectDiagnostics
    {
        private static readonly ILogger log = LogManager.GetLogger("SpellEffectDiagnostics");

        public static void TraceTargetSelection(ISpell spell, IReadOnlyCollection<ISpellTargetInfo> targets, int telegraphCount)
        {
            SpellRuntimeEvidenceCollector.RecordTargetSelection(spell, targets, telegraphCount);

            if (!log.IsTraceEnabled)
                return;

            Spell4AoeTargetConstraintsEntry aoe = spell.Parameters.SpellInfo.AoeTargetConstraints;
            log.Trace(
                "SpellDiagnostics target-selection spell4Id={0} baseSpell4Id={1} castingId={2} caster={3} targetCount={4} telegraphCount={5} aoeTargetLimit={6} aoeSelection={7} aoeRange={8:R}-{9:R} aoeAngle={10:R} targetMechanic={11}/{12} validTargetMask={13} targets=[{14}]",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                spell.Caster.Guid,
                targets.Count,
                telegraphCount,
                aoe?.TargetCount ?? 0u,
                aoe?.TargetSelection ?? 0u,
                aoe?.MinRange ?? 0f,
                aoe?.MaxRange ?? 0f,
                aoe?.Angle ?? 0f,
                spell.Parameters.SpellInfo.BaseInfo.TargetMechanics?.TargetType ?? 0u,
                spell.Parameters.SpellInfo.BaseInfo.TargetMechanics?.Flags ?? 0u,
                spell.Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u,
                string.Join("; ", targets.Select(t => $"{t.Entity.Guid}:{t.Flags}")));
        }

        public static void TracePrimaryTargetValidation(ISpell spell, uint? targetGuid, CastResult result, float horizontalRange, float effectiveRange, float verticalDelta)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics primary-target-validation spell4Id={0} baseSpell4Id={1} castingId={2} caster={3} target={4} result={5} range={6:R} effectiveRange={7:R} verticalDelta={8:R} allowedRange={9:R}-{10:R} allowedVertical={11:R} targetAngle={12:R} targetMechanic={13}/{14} validTargetMask={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                spell.Caster.Guid,
                targetGuid?.ToString() ?? "unknown",
                result,
                horizontalRange,
                effectiveRange,
                verticalDelta,
                spell.Parameters.SpellInfo.Entry.TargetMinRange,
                spell.Parameters.SpellInfo.Entry.TargetMaxRange,
                spell.Parameters.SpellInfo.Entry.TargetVerticalRange,
                spell.Parameters.SpellInfo.BaseInfo.TargetAngle?.TargetAngle ?? 0f,
                spell.Parameters.SpellInfo.BaseInfo.TargetMechanics?.TargetType ?? 0u,
                spell.Parameters.SpellInfo.BaseInfo.TargetMechanics?.Flags ?? 0u,
                spell.Parameters.SpellInfo.BaseInfo.ValidTargets?.TargetBitmask ?? 0u);
        }

        public static void TraceTelegraphAnchorResolution(ISpell spell, string source, Vector3 position, uint? primaryTargetGuid)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics telegraph-anchor spell4Id={0} baseSpell4Id={1} castingId={2} caster={3} targetType={4} primaryTarget={5} source={6} position={7:R}/{8:R}/{9:R} hasExplicitPosition={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                spell.Caster.Guid,
                spell.Parameters.SpellInfo.BaseInfo.TargetMechanics?.TargetType ?? 0u,
                primaryTargetGuid?.ToString() ?? "none",
                source,
                position.X,
                position.Y,
                position.Z,
                spell.Parameters.Position != null);
        }

        public static void TraceEffectDispatch(ISpell spell, SpellEffectInterpretation effect, int targetCount, bool hasHandler)
        {
            SpellRuntimeEvidenceCollector.RecordEffectDispatch(spell, effect, targetCount, hasHandler);

            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics effect-dispatch spell4Id={0} baseSpell4Id={1} castingId={2} spell4EffectId={3} orderIndex={4} effectType={5} targetFlags={6} targetCount={7} hasHandler={8} knownFamily={9} delayMs={10} tickMs={11} durationMs={12} dataBits=\"{13}\" parameters=\"{14}\"",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                effect.Entry.Id,
                effect.Entry.OrderIndex,
                effect.Entry.EffectType,
                effect.Entry.TargetFlags,
                targetCount,
                hasHandler,
                effect.HasKnownFamilySemantics,
                effect.Timing.DelayTime,
                effect.Timing.TickTime,
                effect.Timing.DurationTime,
                effect.FormatDataBits(),
                effect.FormatParameters());
        }

        public static void TraceEffectSchedule(ISpell spell, SpellEffectInterpretation effect, uint firstDelayMs, uint tickMs, uint durationMs, bool repeats)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics effect-schedule spell4Id={0} baseSpell4Id={1} castingId={2} spell4EffectId={3} orderIndex={4} effectType={5} firstDelayMs={6} tickMs={7} durationMs={8} repeats={9}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                effect.Entry.Id,
                effect.Entry.OrderIndex,
                effect.Entry.EffectType,
                firstDelayMs,
                tickMs,
                durationMs,
                repeats);
        }

        public static void TraceEffectLifetime(ISpell spell, SpellEffectInterpretation effect, uint targetGuid, uint durationMs)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics effect-lifetime spell4Id={0} baseSpell4Id={1} castingId={2} spell4EffectId={3} orderIndex={4} effectType={5} target={6} durationMs={7}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                effect.Entry.Id,
                effect.Entry.OrderIndex,
                effect.Entry.EffectType,
                targetGuid,
                durationMs);
        }

        public static void TraceEffectResult(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellRuntimeEvidenceCollector.RecordEffectResult(spell, target, info);

            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics effect-result spell4Id={0} castingId={1} spell4EffectId={2} effectUniqueId={3} target={4} dropEffect={5} combatLogCount={6} createdEntityCount={7} damageRaw={8} damageAdjusted={9} damageShieldAbsorb={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                info.Entry.Id,
                info.EffectId,
                target.Guid,
                info.DropEffect,
                info.CombatLogs.Count,
                info.CreatedEntities.Count,
                info.Damage?.RawDamage,
                info.Damage?.AdjustedDamage,
                info.Damage?.ShieldAbsorbAmount);
        }

        public static void TraceVitalModifier(ISpell spell, IUnitEntity target, SpellEffectVitalModifierSemantics vitalModifier, string mode, float resolvedAmount, float appliedAmount, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics vital-modifier spell4Id={0} castingId={1} target={2} vital={3} mode={4} resolvedAmount={5} appliedAmount={6} applied={7} skippedReason={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataFloat05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                vitalModifier.Vital,
                mode,
                resolvedAmount,
                appliedAmount,
                applied,
                skippedReason,
                vitalModifier.DataBits01,
                vitalModifier.DataBits02,
                vitalModifier.DataBits03,
                vitalModifier.DataBits04,
                vitalModifier.DataFloat05);
        }

        public static void TraceSapVital(ISpell spell, IUnitEntity target, SpellEffectSapVitalSemantics sapVital, string mode, string amountSource, float resolvedAmount, float appliedAmount, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics sap-vital spell4Id={0} castingId={1} target={2} vital={3} mode={4} amountSource={5} resolvedAmount={6:R} appliedAmount={7:R} applied={8} skippedReason={9} dataFloat01={10:R} dataFloat02={11:R} dataBits04={12} dataFloat05={13:R} dataBits06={14} dataBits07={15} dataBits08={16} dataBits09={17}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                sapVital.Vital,
                mode,
                amountSource,
                resolvedAmount,
                appliedAmount,
                applied,
                skippedReason,
                sapVital.DataFloat01,
                sapVital.DataFloat02,
                sapVital.DataBits04,
                sapVital.DataFloat05,
                sapVital.DataBits06,
                sapVital.DataBits07,
                sapVital.DataBits08,
                sapVital.DataBits09);
        }

        public static void TraceTransference(ISpell spell, IUnitEntity target, SpellEffectTransferenceSemantics transference, uint damageAmount, uint rawHeal, uint appliedHeal, uint overheal, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics transference spell4Id={0} castingId={1} target={2} caster={3} healedVital={4} sourceVital={5} damageAmount={6} rawHeal={7} appliedHeal={8} overheal={9} applied={10} skippedReason={11} damageMultiplier={12} baseValue={13} dataBits04={14} transferRate={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                spell.Caster.Guid,
                transference.HealedVital,
                transference.SourceVital,
                damageAmount,
                rawHeal,
                appliedHeal,
                overheal,
                applied,
                skippedReason,
                transference.DamageMultiplier,
                transference.BaseValue,
                transference.DataBits04,
                transference.TransferRate);
        }

        public static void TraceSummonCreature(ISpell spell, IUnitEntity target, SpellEffectSummonCreatureSemantics summonCreature, Vector3 position, bool created, uint summonedGuid, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics summon-creature spell4Id={0} castingId={1} target={2} creatureId={3} created={4} summonedGuid={5} skippedReason={6} position={7:R}/{8:R}/{9:R} dataBits01={10} dataBits02={11} dataBits03={12} dataBits04={13} dataBits05={14} dataBits06={15} dataBits07={16} dataBits08={17} dataBits09={18}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                summonCreature.CreatureId,
                created,
                summonedGuid,
                skippedReason,
                position.X,
                position.Y,
                position.Z,
                summonCreature.DataBits01,
                summonCreature.DataBits02,
                summonCreature.DataBits03,
                summonCreature.DataBits04,
                summonCreature.DataBits05,
                summonCreature.DataBits06,
                summonCreature.DataBits07,
                summonCreature.DataBits08,
                summonCreature.DataBits09);
        }

        public static void TraceSummonVehicle(ISpell spell, IUnitEntity target, SpellEffectSummonVehicleSemantics summonVehicle, Vector3 position, bool created, bool boarded, uint vehicleGuid, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics summon-vehicle spell4Id={0} castingId={1} target={2} creatureId={3} unitVehicleId={4} boardMode={5} created={6} boarded={7} vehicleGuid={8} skippedReason={9} position={10:R}/{11:R}/{12:R} dataBits03={13} dataBits04={14} dataBits05={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                summonVehicle.CreatureId,
                summonVehicle.UnitVehicleId,
                summonVehicle.BoardMode,
                created,
                boarded,
                vehicleGuid,
                skippedReason,
                position.X,
                position.Y,
                position.Z,
                summonVehicle.DataBits03,
                summonVehicle.DataBits04,
                summonVehicle.DataBits05);
        }

        public static void TraceSummonTrap(ISpell spell, IUnitEntity target, SpellEffectSummonTrapSemantics summonTrap, Vector3 position, bool created, uint trapGuid, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics summon-trap spell4Id={0} castingId={1} target={2} creatureId={3} triggerSpell4Id={4} armTimeMs={5} radius={6:R} created={7} trapGuid={8} skippedReason={9} position={10:R}/{11:R}/{12:R} dataBits03={13} dataBits05={14} dataBits06={15} dataBits07={16}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                summonTrap.CreatureId,
                summonTrap.TriggerSpell4Id,
                summonTrap.ArmTimeMs,
                summonTrap.Radius,
                created,
                trapGuid,
                skippedReason,
                position.X,
                position.Y,
                position.Z,
                summonTrap.DataBits03,
                summonTrap.DataBits05,
                summonTrap.DataBits06,
                summonTrap.DataBits07);
        }

        public static void TraceNpcExecutionDelay(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, SpellEffectNpcExecutionDelaySemantics executionDelay)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics npc-execution-delay spell4Id={0} castingId={1} target={2} delayMs={3} tickMs={4} durationMs={5} dataBits00={6} dataBits01={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11} dataBits06={12} dataBits07={13} dataBits08={14} dataBits09={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                info.Entry.DelayTime,
                info.Entry.TickTime,
                info.Entry.DurationTime,
                executionDelay.DataBits00,
                executionDelay.DataBits01,
                executionDelay.DataBits02,
                executionDelay.DataBits03,
                executionDelay.DataBits04,
                executionDelay.DataBits05,
                executionDelay.DataBits06,
                executionDelay.DataBits07,
                executionDelay.DataBits08,
                executionDelay.DataBits09);
        }

        public static void TraceRavelSignal(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info, SpellEffectRavelSignalSemantics ravelSignal, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics ravel-signal spell4Id={0} castingId={1} target={2} spell4EffectId={3} mode={4} signalId={5} skippedReason={6} dataBits02={7} dataBits03={8} dataBits04={9} dataBits05={10} dataBits06={11} dataBits07={12} dataBits08={13} dataBits09={14}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                info.Entry.Id,
                ravelSignal.Mode,
                ravelSignal.SignalId,
                skippedReason,
                ravelSignal.DataBits02,
                ravelSignal.DataBits03,
                ravelSignal.DataBits04,
                ravelSignal.DataBits05,
                ravelSignal.DataBits06,
                ravelSignal.DataBits07,
                ravelSignal.DataBits08,
                ravelSignal.DataBits09);
        }

        public static void TraceModifyInterruptArmor(ISpell spell, IUnitEntity target, SpellEffectModifyInterruptArmorSemantics interruptArmor, uint appliedAmount, bool removed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics modify-interrupt-armor spell4Id={0} castingId={1} target={2} amount={3} removeOnInterrupt={4} appliedAmount={5} removed={6} currentInterruptArmor={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11} handlerCandidate={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                interruptArmor.Amount,
                interruptArmor.RemoveOnInterrupt,
                appliedAmount,
                removed,
                target.InterruptArmor,
                interruptArmor.DataBits02,
                interruptArmor.DataBits03,
                interruptArmor.DataBits04,
                interruptArmor.DataBits05,
                CombatLogHandlerCandidate.ModifyInterruptArmor);
        }

        public static void TraceAbsorption(ISpell spell, IUnitEntity target, SpellEffectAbsorptionSemantics absorption, uint amount, bool removed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics absorption spell4Id={0} castingId={1} target={2} amount={3} removed={4} currentAbsorption={5} multiplier={6} baseValue={7} absorptionType={8} dataBits02={9} dataBits03={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                amount,
                removed,
                target.CurrentAbsorption,
                absorption.TypeMultiplier,
                absorption.TypeBaseValue,
                absorption.AbsorptionType,
                absorption.DataBits02,
                absorption.DataBits03,
                absorption.DataBits05);
        }

        public static void TraceHealingAbsorption(ISpell spell, IUnitEntity target, SpellEffectHealingAbsorptionSemantics absorption, uint amount, bool removed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics healing-absorption spell4Id={0} castingId={1} target={2} amount={3} removed={4} currentHealingAbsorption={5} multiplier={6} baseValue={7} mode={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                amount,
                removed,
                target.CurrentHealingAbsorption,
                absorption.TypeMultiplier,
                absorption.TypeBaseValue,
                absorption.Mode,
                absorption.DataBits03,
                absorption.DataBits04,
                absorption.DataBits05);
        }

        public static void TraceThreatModification(ISpell spell, IUnitEntity target, SpellEffectThreatModificationSemantics threat, string action, uint ownerGuid, uint hatedGuid, uint beforeThreat, uint afterThreat, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics threat-modification spell4Id={0} castingId={1} target={2} mode={3} action={4} owner={5} hated={6} beforeThreat={7} afterThreat={8} skippedReason={9} ratioOrPercent={10} dataBits02={11} threatValue={12} dataBits04={13} dataBits05={14}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                threat.Mode,
                action,
                ownerGuid,
                hatedGuid,
                beforeThreat,
                afterThreat,
                skippedReason,
                threat.RatioOrPercent,
                threat.DataBits02,
                threat.ThreatValue,
                threat.DataBits04,
                threat.DataBits05);
        }

        public static void TraceThreatTransfer(ISpell spell, IUnitEntity target, SpellEffectThreatTransferSemantics threatTransfer)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics threat-transfer spell4Id={0} castingId={1} target={2} mode={3} ratioOrPercent={4} dataBits02={5} dataBits03={6} dataBits04={7} dataBits05={8}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                threatTransfer.Mode,
                threatTransfer.RatioOrPercent,
                threatTransfer.DataBits02,
                threatTransfer.DataBits03,
                threatTransfer.DataBits04,
                threatTransfer.DataBits05);
        }

        public static void TraceSettlerCampfire(ISpell spell, IUnitEntity target, SpellEffectSettlerCampfireSemantics campfire, uint backInActionSpell4Id, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics settler-campfire spell4Id={0} baseSpell4Id={1} castingId={2} caster={3} target={4} tierIndex={5} backInActionSpell4Id={6} applied={7} skippedReason={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataBits05={13} dataBits06={14} dataBits07={15} dataBits08={16} dataBits09={17}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                spell.Caster.Guid,
                target.Guid,
                campfire.TierIndex,
                backInActionSpell4Id,
                applied,
                skippedReason,
                campfire.DataBits01,
                campfire.DataBits02,
                campfire.DataBits03,
                campfire.DataBits04,
                campfire.DataBits05,
                campfire.DataBits06,
                campfire.DataBits07,
                campfire.DataBits08,
                campfire.DataBits09);
        }

        public static void TraceDispel(ISpell spell, IUnitEntity target, SpellEffectDispelSemantics dispel, uint maxCount, int removedCount)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics dispel spell4Id={0} castingId={1} target={2} maxCount={3} removedCount={4} countA={5} countB={6} dataBits02={7} spellClass={8} dataBits04={9} priority={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                maxCount,
                removedCount,
                dispel.CountA,
                dispel.CountB,
                dispel.DataBits02,
                dispel.SpellClass,
                dispel.DataBits04,
                dispel.Priority);
        }

        public static void TraceModifyAbilityCharges(ISpell spell, IUnitEntity target, SpellEffectModifyAbilityChargesSemantics charges, uint beforeCharges, uint afterCharges, string action, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics modify-ability-charges spell4Id={0} castingId={1} target={2} targetSpell4Id={3} count={4} mode={5} action={6} beforeCharges={7} afterCharges={8} skippedReason={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                charges.Spell4Id,
                charges.Count,
                charges.Mode,
                action,
                beforeCharges,
                afterCharges,
                skippedReason,
                charges.DataBits03,
                charges.DataBits04,
                charges.DataBits05);
        }

        public static void TraceCooldownReset(ISpell spell, IUnitEntity target, SpellEffectCooldownResetSemantics cooldownReset, string action, uint resolvedSpell4Id, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics cooldown-reset spell4Id={0} castingId={1} target={2} action={3} resolvedSpell4Id={4} skippedReason={5} dataBits00={6} dataBits01={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                action,
                resolvedSpell4Id,
                skippedReason,
                cooldownReset.DataBits00,
                cooldownReset.Spell4Id,
                cooldownReset.DataBits02,
                cooldownReset.DataBits03,
                cooldownReset.DataBits04,
                cooldownReset.DataBits05);
        }

        public static void TraceModifySpellCooldown(ISpell spell, IUnitEntity target, SpellEffectModifySpellCooldownSemantics cooldown, string action, uint resolvedSpell4Id, double beforeCooldown, double afterCooldown, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics modify-spell-cooldown spell4Id={0} castingId={1} target={2} mode={3} operation={4} action={5} resolvedSpell4Id={6} beforeCooldown={7} afterCooldown={8} skippedReason={9} targetSpell4Id={10} dataFloat03={11} dataBits04={12} dataBits05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                cooldown.Mode,
                cooldown.Operation,
                action,
                resolvedSpell4Id,
                beforeCooldown,
                afterCooldown,
                skippedReason,
                cooldown.Spell4Id,
                cooldown.DataFloat03,
                cooldown.DataBits04,
                cooldown.DataBits05);
        }

        public static void TraceActivateSpellCooldown(ISpell spell, IUnitEntity target, SpellEffectActivateSpellCooldownSemantics cooldown, string action, uint resolvedSpell4Id, double cooldownSeconds, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics activate-spell-cooldown spell4Id={0} castingId={1} target={2} action={3} resolvedSpell4Id={4} cooldownSeconds={5} skippedReason={6} dataBits00={7} targetSpell4Id={8} mode={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                action,
                resolvedSpell4Id,
                cooldownSeconds,
                skippedReason,
                cooldown.DataBits00,
                cooldown.Spell4Id,
                cooldown.Mode,
                cooldown.DataBits03,
                cooldown.DataBits04,
                cooldown.DataBits05);
        }

        public static void TraceSpellEffectImmunity(ISpell spell, IUnitEntity target, SpellEffectImmunitySemantics immunity, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics spell-effect-immunity spell4Id={0} castingId={1} target={2} immuneEffectType={3} immuneEffectTypeRaw={4} applied={5} removed={6} skippedReason={7} dataBits01={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                immunity.EffectType,
                immunity.EffectTypeRaw,
                applied,
                removed,
                skippedReason,
                immunity.DataBits01,
                immunity.DataBits02,
                immunity.DataBits03,
                immunity.DataBits04,
                immunity.DataBits05);
        }

        public static void TraceSpellEffectImmunityBlocked(ISpell spell, IUnitEntity target, SpellEffectInterpretation blockedEffect)
        {
            SpellRuntimeEvidenceCollector.RecordBlockedEffect(spell, target, blockedEffect, "spell-effect-immunity", blockedEffect.Entry.EffectType.ToString());

            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics spell-effect-immune-blocked spell4Id={0} castingId={1} target={2} blockedSpell4EffectId={3} blockedEffectType={4}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                blockedEffect.Entry.Id,
                blockedEffect.Entry.EffectType);
        }

        public static void TraceSpellImmunity(ISpell spell, IUnitEntity target, SpellImmunitySemantics immunity, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics spell-immunity spell4Id={0} castingId={1} target={2} mode={3} immuneSpell4Id={4} applied={5} removed={6} skippedReason={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                immunity.Mode,
                immunity.Spell4Id,
                applied,
                removed,
                skippedReason,
                immunity.DataBits02,
                immunity.DataBits03,
                immunity.DataBits04,
                immunity.DataBits05);
        }

        public static void TraceSpellImmunityBlocked(ISpell spell, IUnitEntity target, SpellEffectInterpretation blockedEffect, uint immuneSpell4Id)
        {
            SpellRuntimeEvidenceCollector.RecordBlockedEffect(spell, target, blockedEffect, "spell-immunity", $"immuneSpell4Id={immuneSpell4Id}");

            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics spell-immune-blocked spell4Id={0} castingId={1} target={2} immuneSpell4Id={3} blockedSpell4EffectId={4} blockedEffectType={5}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                immuneSpell4Id,
                blockedEffect.Entry.Id,
                blockedEffect.Entry.EffectType);
        }

        public static void TraceUnitStateImmuneBlocked(ISpell spell, IUnitEntity target, SpellEffectInterpretation blockedEffect, uint blockingStateId)
        {
            SpellRuntimeEvidenceCollector.RecordBlockedEffect(spell, target, blockedEffect, "unit-state-immunity", $"blockingStateId={blockingStateId}");

            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics unit-state-immune-blocked spell4Id={0} castingId={1} target={2} blockingStateId={3} blockingStateName={4} blockedSpell4EffectId={5} blockedEffectType={6}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                blockingStateId,
                UnitStateSetRules.DescribeState(blockingStateId) ?? "unknown",
                blockedEffect.Entry.Id,
                blockedEffect.Entry.EffectType);
        }

        public static void TraceScale(ISpell spell, IUnitEntity target, SpellEffectScaleSemantics scale, float previousScale, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics scale spell4Id={0} castingId={1} target={2} targetScale={3} previousScale={4} currentScale={5} applyTimeMs={6} restoreTimeMs={7} applied={8} removed={9} skippedReason={10} dataBits03={11} dataFloat04={12} dataBits05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                scale.TargetScale,
                previousScale,
                target.MovementManager.GetScale(),
                scale.ApplyTimeMs,
                scale.RestoreTimeMs,
                applied,
                removed,
                skippedReason,
                scale.DataBits03,
                scale.DataFloat04,
                scale.DataBits05);
        }

        public static void TraceFactionSet(ISpell spell, IUnitEntity target, SpellEffectFactionSetSemantics factionSet, uint previousFactionId, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics faction-set spell4Id={0} castingId={1} target={2} factionId={3} previousFactionId={4} currentFactionId={5} applied={6} removed={7} skippedReason={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataBits05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                factionSet.FactionId,
                previousFactionId,
                (uint)target.Faction1,
                applied,
                removed,
                skippedReason,
                factionSet.DataBits01,
                factionSet.DataBits02,
                factionSet.DataBits03,
                factionSet.DataBits04,
                factionSet.DataBits05);
        }

        public static void TraceAddSpell(ISpell spell, IUnitEntity target, SpellEffectAddSpellSemantics addSpell, uint resolvedSpell4BaseId, string action, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics add-spell spell4Id={0} castingId={1} target={2} addSpell4Id={3} resolvedSpell4BaseId={4} action={5} skippedReason={6} dataBits01={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                addSpell.Spell4Id,
                resolvedSpell4BaseId,
                action,
                skippedReason,
                addSpell.DataBits01,
                addSpell.DataBits02,
                addSpell.DataBits03,
                addSpell.DataBits04,
                addSpell.DataBits05);
        }

        public static void TraceKill(ISpell spell, IUnitEntity target, SpellEffectKillSemantics kill, uint healthBefore, uint healthAfter, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics kill spell4Id={0} castingId={1} target={2} healthBefore={3} healthAfter={4} applied={5} skippedReason={6} dataBits00={7} dataBits01={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                healthBefore,
                healthAfter,
                applied,
                skippedReason,
                kill.DataBits00,
                kill.DataBits01,
                kill.DataBits02,
                kill.DataBits03,
                kill.DataBits04,
                kill.DataBits05);
        }

        public static void TraceDelayDeath(ISpell spell, IUnitEntity target, SpellEffectDelayDeathSemantics delayDeath, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics delay-death spell4Id={0} castingId={1} target={2} mode={3} triggerSpell4Id={4} triggerDelayMs={5} applied={6} removed={7} skippedReason={8} dataBits03={9} dataBits04={10} dataBits05={11} dataBits06={12} dataBits07={13} dataBits08={14} dataBits09={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                delayDeath.Mode,
                delayDeath.TriggerSpell4Id,
                delayDeath.TriggerDelayMs,
                applied,
                removed,
                skippedReason,
                delayDeath.DataBits03,
                delayDeath.DataBits04,
                delayDeath.DataBits05,
                delayDeath.DataBits06,
                delayDeath.DataBits07,
                delayDeath.DataBits08,
                delayDeath.DataBits09);
        }

        public static void TraceProc(ISpell spell, IUnitEntity target, SpellEffectProcSemantics proc, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics proc spell4Id={0} castingId={1} target={2} triggerEvent={3} triggerSpell4Id={4} chance={5:R} targetData={6} cooldownMsOrSentinel={7} applied={8} removed={9} skippedReason={10} dataBits05={11} dataBits06={12} dataBits07={13} dataBits08={14} dataBits09={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                proc.TriggerEvent,
                proc.TriggerSpell4Id,
                proc.Chance,
                proc.TargetData,
                proc.CooldownMsOrSentinel,
                applied,
                removed,
                skippedReason,
                proc.DataBits05,
                proc.DataBits06,
                proc.DataBits07,
                proc.DataBits08,
                proc.DataBits09);
        }

        public static void TraceProcProbe(IUnitEntity holder, string eventName, string phase, uint? observedTriggerEvent, uint sourceGuid, uint targetGuid, uint triggerSpell4Id, uint triggerCastingId, uint triggerSpell4EffectId, IDamageDescription damage, uint procEffectId, uint procSpell4Id, uint procCastingId, uint procTriggerEvent, uint procTriggerSpell4Id, float procChance, uint procTargetData, uint procCooldownMsOrSentinel, uint dataBits05, uint dataBits06, uint dataBits07, uint dataBits08, uint dataBits09)
        {
            if (!log.IsTraceEnabled)
                return;

            bool triggerEventMatches = observedTriggerEvent.HasValue && observedTriggerEvent.Value == procTriggerEvent;
            log.Trace(
                "SpellDiagnostics proc-probe event={0} phase={1} holder={2} source={3} target={4} observedTriggerEvent={5} triggerEventMatches={6} procEffectId={7} procSpell4Id={8} procCastingId={9} procTriggerEvent={10} procTriggerSpell4Id={11} procChance={12:R} procTargetData={13} procCooldownMsOrSentinel={14} triggerSpell4Id={15} triggerCastingId={16} triggerSpell4EffectId={17} rawAmount={18} adjustedAmount={19} absorbed={20} shieldAbsorb={21} overkill={22} killedTarget={23} combatResult={24} dataBits05={25} dataBits06={26} dataBits07={27} dataBits08={28} dataBits09={29}",
                eventName,
                phase,
                holder.Guid,
                sourceGuid,
                targetGuid,
                observedTriggerEvent?.ToString() ?? "unknown",
                triggerEventMatches,
                procEffectId,
                procSpell4Id,
                procCastingId,
                procTriggerEvent,
                procTriggerSpell4Id,
                procChance,
                procTargetData,
                procCooldownMsOrSentinel,
                triggerSpell4Id,
                triggerCastingId,
                triggerSpell4EffectId,
                damage?.RawDamage ?? 0u,
                damage?.AdjustedDamage ?? 0u,
                damage?.AbsorbedAmount ?? 0u,
                damage?.ShieldAbsorbAmount ?? 0u,
                damage?.OverkillAmount ?? 0u,
                damage?.KilledTarget ?? false,
                damage?.CombatResult.ToString() ?? "unknown",
                dataBits05,
                dataBits06,
                dataBits07,
                dataBits08,
                dataBits09);
        }

        public static void TraceProcDispatch(IUnitEntity holder, string eventName, string phase, uint? observedTriggerEvent, uint sourceGuid, uint targetGuid, uint resolvedTargetGuid, uint procEffectId, uint procSpell4Id, uint procCastingId, uint procTriggerEvent, uint procTriggerSpell4Id, float procChance, uint procTargetData, uint procCooldownMsOrSentinel, double procCooldownRemainingSeconds, string action, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics proc-dispatch event={0} phase={1} holder={2} source={3} target={4} observedTriggerEvent={5} resolvedTarget={6} procEffectId={7} procSpell4Id={8} procCastingId={9} procTriggerEvent={10} procTriggerSpell4Id={11} procChance={12:R} procTargetData={13} procCooldownMsOrSentinel={14} procCooldownRemainingSeconds={15:R} action={16} skippedReason={17}",
                eventName,
                phase,
                holder.Guid,
                sourceGuid,
                targetGuid,
                observedTriggerEvent?.ToString() ?? "unknown",
                resolvedTargetGuid,
                procEffectId,
                procSpell4Id,
                procCastingId,
                procTriggerEvent,
                procTriggerSpell4Id,
                procChance,
                procTargetData,
                procCooldownMsOrSentinel,
                procCooldownRemainingSeconds,
                action,
                skippedReason);
        }

        public static void TraceDelayDeathTriggered(IUnitEntity target, uint spell4Id, uint castingId, uint mode, uint triggerSpell4Id, uint triggerDelayMs, uint dataBits03, uint dataBits04, uint dataBits05, uint dataBits06, uint dataBits07, uint sourceGuid, uint preventedDamage)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics delay-death-triggered spell4Id={0} castingId={1} target={2} source={3} mode={4} triggerSpell4Id={5} triggerDelayMs={6} preventedDamage={7} dataBits03={8} dataBits04={9} dataBits05={10} dataBits06={11} dataBits07={12}",
                spell4Id,
                castingId,
                target.Guid,
                sourceGuid,
                mode,
                triggerSpell4Id,
                triggerDelayMs,
                preventedDamage,
                dataBits03,
                dataBits04,
                dataBits05,
                dataBits06,
                dataBits07);
        }

        public static void TraceDelayDeathTriggerCast(IUnitEntity target, uint triggerSpell4Id, uint sourceGuid)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics delay-death-trigger-cast target={0} triggerSpell4Id={1} source={2}",
                target.Guid,
                triggerSpell4Id,
                sourceGuid);
        }

        public static void TraceClampVital(ISpell spell, IUnitEntity target, SpellEffectClampVitalSemantics clampVital, Vital vital, uint valueBefore, uint valueAfter, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics clamp-vital spell4Id={0} castingId={1} target={2} vital={3} mode={4} vitalMode={5} ratio={6:R} valueBefore={7} valueAfter={8} applied={9} removed={10} skippedReason={11} dataBits03={12} dataBits04={13} dataBits05={14} dataBits06={15} dataBits07={16} dataBits08={17} dataBits09={18}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                vital,
                clampVital.Mode,
                clampVital.VitalMode,
                clampVital.Ratio,
                valueBefore,
                valueAfter,
                applied,
                removed,
                skippedReason,
                clampVital.DataBits03,
                clampVital.DataBits04,
                clampVital.DataBits05,
                clampVital.DataBits06,
                clampVital.DataBits07,
                clampVital.DataBits08,
                clampVital.DataBits09);
        }

        public static void TraceShieldOverload(ISpell spell, IUnitEntity target, SpellEffectShieldOverloadSemantics shieldOverload, uint shieldBefore, uint shieldAfter, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics shield-overload spell4Id={0} castingId={1} target={2} shieldBefore={3} shieldAfter={4} applied={5} removed={6} skippedReason={7} dataBits00={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataBits05={13} dataBits06={14} dataBits07={15} dataBits08={16} dataBits09={17}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                shieldBefore,
                shieldAfter,
                applied,
                removed,
                skippedReason,
                shieldOverload.DataBits00,
                shieldOverload.DataBits01,
                shieldOverload.DataBits02,
                shieldOverload.DataBits03,
                shieldOverload.DataBits04,
                shieldOverload.DataBits05,
                shieldOverload.DataBits06,
                shieldOverload.DataBits07,
                shieldOverload.DataBits08,
                shieldOverload.DataBits09);
        }

        public static void TraceUnitStateSet(ISpell spell, IUnitEntity target, SpellEffectUnitStateSetSemantics unitState, bool changed, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics unit-state-set spell4Id={0} castingId={1} target={2} stateId={3} stateName={4} changed={5} applied={6} removed={7} skippedReason={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataBits05={13} dataBits06={14} dataBits07={15} dataBits08={16} dataBits09={17}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                unitState.StateId,
                UnitStateSetRules.DescribeState(unitState.StateId) ?? "unknown",
                changed,
                applied,
                removed,
                skippedReason,
                unitState.DataBits01,
                unitState.DataBits02,
                unitState.DataBits03,
                unitState.DataBits04,
                unitState.DataBits05,
                unitState.DataBits06,
                unitState.DataBits07,
                unitState.DataBits08,
                unitState.DataBits09);
        }

        public static void TraceSetBusy(ISpell spell, IWorldEntity target, SpellEffectSetBusySemantics setBusy, bool changed, bool applied, bool removed, uint removedCount, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics set-busy spell4Id={0} castingId={1} target={2} busy={3} mode={4} contextId={5} changed={6} applied={7} removed={8} removedCount={9} skippedReason={10} dataBits02={11} dataBits03={12} dataBits04={13} dataBits05={14} dataBits06={15} dataBits07={16} dataBits08={17} dataBits09={18}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                setBusy.Busy,
                setBusy.Mode,
                setBusy.ContextId,
                changed,
                applied,
                removed,
                removedCount,
                skippedReason,
                setBusy.DataBits02,
                setBusy.DataBits03,
                setBusy.DataBits04,
                setBusy.DataBits05,
                setBusy.DataBits06,
                setBusy.DataBits07,
                setBusy.DataBits08,
                setBusy.DataBits09);
        }

        public static void TraceGrantXp(ISpell spell, IUnitEntity target, SpellEffectGrantXpSemantics grantXp, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics grant-xp spell4Id={0} castingId={1} target={2} amount={3} applied={4} skippedReason={5} dataBits01={6} dataBits02={7} dataBits03={8} dataBits04={9} dataBits05={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                grantXp.Amount,
                applied,
                skippedReason,
                grantXp.DataBits01,
                grantXp.DataBits02,
                grantXp.DataBits03,
                grantXp.DataBits04,
                grantXp.DataBits05);
        }

        public static void TracePathXpModify(ISpell spell, IUnitEntity target, SpellEffectPathXpModifySemantics pathXp, string action, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics path-xp-modify spell4Id={0} castingId={1} target={2} amount={3} mode={4} action={5} applied={6} skippedReason={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                pathXp.Amount,
                pathXp.Mode,
                action,
                applied,
                skippedReason,
                pathXp.DataBits02,
                pathXp.DataBits03,
                pathXp.DataBits04,
                pathXp.DataBits05);
        }

        public static void TraceGrantLevelScaledXp(ISpell spell, IUnitEntity target, SpellEffectGrantLevelScaledXpSemantics levelScaledXp, uint resolvedAmount, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics grant-level-scaled-xp spell4Id={0} castingId={1} target={2} percentOfLevel={3:R} maxLevel={4} mode={5} resolvedAmount={6} applied={7} skippedReason={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                levelScaledXp.PercentOfLevel,
                levelScaledXp.MaxLevel,
                levelScaledXp.Mode,
                resolvedAmount,
                applied,
                skippedReason,
                levelScaledXp.DataBits03,
                levelScaledXp.DataBits04,
                levelScaledXp.DataBits05);
        }

        public static void TraceModifyRestedXp(ISpell spell, IUnitEntity target, SpellEffectModifyRestedXpSemantics modifyRestedXp, uint previousRestBonusXp, uint currentRestBonusXp, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics modify-rested-xp spell4Id={0} castingId={1} target={2} levelSpanMultiplier={3:R} previousRestBonusXp={4} currentRestBonusXp={5} applied={6} skippedReason={7} dataBits01={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                modifyRestedXp.LevelSpanMultiplier,
                previousRestBonusXp,
                currentRestBonusXp,
                applied,
                skippedReason,
                modifyRestedXp.DataBits01,
                modifyRestedXp.DataBits02,
                modifyRestedXp.DataBits03,
                modifyRestedXp.DataBits04,
                modifyRestedXp.DataBits05);
        }

        public static void TraceGiveAugmentPowerToPlayer(ISpell spell, IUnitEntity target, SpellEffectGiveAugmentPowerToPlayerSemantics augmentPower, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics give-augment-power-to-player spell4Id={0} castingId={1} target={2} amount={3} applied={4} skippedReason={5} dataBits01={6} dataBits02={7} dataBits03={8} dataBits04={9} dataBits05={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                augmentPower.Amount,
                applied,
                skippedReason,
                augmentPower.DataBits01,
                augmentPower.DataBits02,
                augmentPower.DataBits03,
                augmentPower.DataBits04,
                augmentPower.DataBits05);
        }

        public static void TraceQuestAdvanceObjective(ISpell spell, IUnitEntity target, SpellEffectQuestAdvanceObjectiveSemantics questAdvance, uint playerGuid, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics quest-advance-objective spell4Id={0} castingId={1} target={2} player={3} objectiveId={4} progress={5} applied={6} skippedReason={7} dataBits01={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                questAdvance.ObjectiveId,
                questAdvance.Progress,
                applied,
                skippedReason,
                questAdvance.DataBits01,
                questAdvance.DataBits03,
                questAdvance.DataBits04,
                questAdvance.DataBits05);
        }

        public static void TraceAchievementAdvance(ISpell spell, IUnitEntity target, SpellEffectAchievementAdvanceSemantics achievementAdvance, uint playerGuid, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics achievement-advance spell4Id={0} castingId={1} target={2} player={3} achievementId={4} count={5} applied={6} skippedReason={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                achievementAdvance.AchievementId,
                achievementAdvance.Count,
                applied,
                skippedReason,
                achievementAdvance.DataBits02,
                achievementAdvance.DataBits03,
                achievementAdvance.DataBits04,
                achievementAdvance.DataBits05);
        }

        public static void TraceReputationModify(ISpell spell, IUnitEntity target, SpellEffectReputationModifySemantics reputationModify, uint playerGuid, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics reputation-modify spell4Id={0} castingId={1} target={2} player={3} factionId={4} amount={5} applied={6} skippedReason={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                reputationModify.FactionId,
                reputationModify.Amount,
                applied,
                skippedReason,
                reputationModify.DataBits02,
                reputationModify.DataBits03,
                reputationModify.DataBits04,
                reputationModify.DataBits05);
        }

        public static void TraceGiveItemToPlayer(ISpell spell, IUnitEntity target, SpellEffectGiveItemToPlayerSemantics giveItem, uint playerGuid, uint count, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics give-item-to-player spell4Id={0} castingId={1} target={2} player={3} item2Id={4} count={5} applied={6} skippedReason={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                giveItem.Item2Id,
                count,
                applied,
                skippedReason,
                giveItem.DataBits02,
                giveItem.DataBits03,
                giveItem.DataBits04,
                giveItem.DataBits05);
        }

        public static void TraceGiveSchematic(ISpell spell, IUnitEntity target, SpellEffectGiveSchematicSemantics giveSchematic, uint playerGuid, uint tradeskillId, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics give-schematic spell4Id={0} castingId={1} target={2} player={3} tradeskillSchematic2Id={4} tradeskillId={5} applied={6} skippedReason={7} dataBits01={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                giveSchematic.TradeskillSchematic2Id,
                tradeskillId,
                applied,
                skippedReason,
                giveSchematic.DataBits01,
                giveSchematic.DataBits02,
                giveSchematic.DataBits03,
                giveSchematic.DataBits04,
                giveSchematic.DataBits05);
        }

        public static void TraceRewardPropertyModifier(ISpell spell, IUnitEntity target, SpellEffectRewardPropertyModifierSemantics rewardProperty, uint playerGuid, float value, bool applied, bool removed, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics reward-property-modifier spell4Id={0} castingId={1} target={2} player={3} rewardPropertyId={4} data={5} value={6} applied={7} removed={8} skippedReason={9} valueFloat02={10} valueFloat03={11} dataBits04={12} dataBits05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                rewardProperty.RewardPropertyId,
                rewardProperty.Data,
                value,
                applied,
                removed,
                skippedReason,
                rewardProperty.ValueFloat02,
                rewardProperty.ValueFloat03,
                rewardProperty.DataBits04,
                rewardProperty.DataBits05);
        }

        public static void TraceItemVisualSwap(ISpell spell, IUnitEntity target, SpellEffectItemVisualSwapSemantics itemVisualSwap, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics item-visual-swap spell4Id={0} castingId={1} target={2} visualSlot={3} displayId={4} colourSetId={5} dyeData={6} applied={7} skippedReason={8} dataBits02={9} dataBits03={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                itemVisualSwap.VisualSlot,
                itemVisualSwap.DisplayId,
                itemVisualSwap.ColourSetId,
                itemVisualSwap.DyeData,
                applied,
                skippedReason,
                itemVisualSwap.DataBits02,
                itemVisualSwap.DataBits03);
        }

        public static void TraceDisguiseOutfit(
            ISpell spell,
            IUnitEntity target,
            SpellEffectDisguiseOutfitSemantics disguiseOutfit,
            ushort previousOutfitInfo,
            bool appliedOutfit,
            bool appliedPrimaryVisual,
            bool appliedSecondaryVisual,
            bool removed,
            string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics disguise-outfit spell4Id={0} castingId={1} target={2} outfitInfoId={3} previousOutfitInfo={4} currentOutfitInfo={5} primaryItemDisplayId={6} secondaryItemDisplayId={7} appliedOutfit={8} appliedPrimaryVisual={9} appliedSecondaryVisual={10} removed={11} skippedReason={12} dataBits03={13} dataFloat04={14} dataFloat05={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                disguiseOutfit.OutfitInfoId,
                previousOutfitInfo,
                target.OutfitInfo,
                disguiseOutfit.PrimaryItemDisplayId,
                disguiseOutfit.SecondaryItemDisplayId,
                appliedOutfit,
                appliedPrimaryVisual,
                appliedSecondaryVisual,
                removed,
                skippedReason,
                disguiseOutfit.DataBits03,
                disguiseOutfit.DataFloat04,
                disguiseOutfit.DataFloat05);
        }

        public static void TraceMimicDisguise(
            ISpell spell,
            IUnitEntity target,
            SpellEffectMimicDisguiseSemantics mimicDisguise,
            uint sourceGuid,
            uint previousDisplayInfo,
            ushort previousOutfitInfo,
            uint sourceDisplayInfo,
            ushort sourceOutfitInfo,
            bool appliedDisplay,
            bool appliedOutfit,
            bool appliedVisuals,
            bool removed,
            string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics mimic-disguise spell4Id={0} castingId={1} target={2} source={3} previousDisplayInfo={4} currentDisplayInfo={5} sourceDisplayInfo={6} previousOutfitInfo={7} currentOutfitInfo={8} sourceOutfitInfo={9} appliedDisplay={10} appliedOutfit={11} appliedVisuals={12} removed={13} skippedReason={14} dataBits00={15} dataBits01={16} dataBits02={17} dataBits03={18} dataBits04={19} dataBits05={20}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                sourceGuid,
                previousDisplayInfo,
                target.DisplayInfo,
                sourceDisplayInfo,
                previousOutfitInfo,
                target.OutfitInfo,
                sourceOutfitInfo,
                appliedDisplay,
                appliedOutfit,
                appliedVisuals,
                removed,
                skippedReason,
                mimicDisguise.DataBits00,
                mimicDisguise.DataBits01,
                mimicDisguise.DataBits02,
                mimicDisguise.DataBits03,
                mimicDisguise.DataBits04,
                mimicDisguise.DataBits05);
        }

        public static void TracePersonalDmgHealMod(
            ISpell spell,
            IUnitEntity target,
            SpellEffectPersonalDmgHealModSemantics personalMod,
            Property? property,
            bool applied,
            string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics personal-dmg-heal-mod spell4Id={0} castingId={1} target={2} modifierType={3} priority={4} property={5} multiplier={6} applied={7} skippedReason={8} dataFloat03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                personalMod.ModifierType,
                personalMod.Priority,
                property?.ToString() ?? "unknown",
                personalMod.Multiplier,
                applied,
                skippedReason,
                personalMod.DataFloat03,
                personalMod.DataBits04,
                personalMod.DataBits05);
        }

        public static void TraceDisembark(ISpell spell, IUnitEntity target, SpellEffectDisembarkSemantics disembark, uint playerGuid, bool wasMounted, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics disembark spell4Id={0} castingId={1} target={2} player={3} mode={4} wasMounted={5} applied={6} skippedReason={7} dataBits01={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                disembark.Mode,
                wasMounted,
                applied,
                skippedReason,
                disembark.DataBits01,
                disembark.DataBits02,
                disembark.DataBits03,
                disembark.DataBits04,
                disembark.DataBits05);
        }

        public static void TraceHousingTeleport(ISpell spell, IUnitEntity target, SpellEffectHousingTeleportSemantics housingTeleport, bool escapeVariant, uint playerGuid, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics housing-teleport spell4Id={0} castingId={1} target={2} player={3} escapeVariant={4} mode={5} destinationMode={6} applied={7} skippedReason={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                escapeVariant,
                housingTeleport.Mode,
                housingTeleport.DestinationMode,
                applied,
                skippedReason,
                housingTeleport.DataBits02,
                housingTeleport.DataBits03,
                housingTeleport.DataBits04,
                housingTeleport.DataBits05);
        }

        public static void TraceSupportStuck(ISpell spell, IUnitEntity target, SpellEffectSupportStuckSemantics supportStuck, uint healthBefore, uint healthAfter, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics support-stuck spell4Id={0} castingId={1} target={2} durabilityMode={3:R} dataBits00={4} healthBefore={5} healthAfter={6} applied={7} skippedReason={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataBits05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                supportStuck.DurabilityMode,
                supportStuck.DataBits00,
                healthBefore,
                healthAfter,
                applied,
                skippedReason,
                supportStuck.DataBits01,
                supportStuck.DataBits02,
                supportStuck.DataBits03,
                supportStuck.DataBits04,
                supportStuck.DataBits05);
        }

        public static void TraceActionBarSet(ISpell spell, IUnitEntity target, SpellEffectActionBarSetSemantics actionBarSet, uint playerGuid, uint associatedUnitId, ShortcutSet shortcutSet, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics action-bar-set spell4Id={0} castingId={1} target={2} player={3} associatedUnit={4} shortcutSet={5} actionBarShortcutSetId={6} applied={7} skippedReason={8} dataBits01={9} dataBits02={10} dataBits03={11} dataBits04={12} dataBits05={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                associatedUnitId,
                shortcutSet,
                actionBarSet.ActionBarShortcutSetId,
                applied,
                skippedReason,
                actionBarSet.DataBits01,
                actionBarSet.DataBits02,
                actionBarSet.DataBits03,
                actionBarSet.DataBits04,
                actionBarSet.DataBits05);
        }

        public static void TraceForceFacing(ISpell spell, IUnitEntity target, SpellEffectForceFacingSemantics forceFacing, bool npcVariant, float previousYaw, float requestedYaw, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics force-facing spell4Id={0} castingId={1} target={2} npcVariant={3} usesAngleOffset={4} angleDegrees={5:R} turnDurationMs={6} previousYaw={7:R} requestedYaw={8:R} applied={9} skippedReason={10} dataBits01={11} dataBits02={12} dataBits04={13} dataBits05={14} dataBits06={15} dataBits07={16} dataBits08={17} dataBits09={18}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                npcVariant,
                forceFacing.UsesAngleOffset,
                forceFacing.AngleDegrees,
                forceFacing.TurnDurationMs,
                previousYaw,
                requestedYaw,
                applied,
                skippedReason,
                forceFacing.DataBits01,
                forceFacing.DataBits02,
                forceFacing.DataBits04,
                forceFacing.DataBits05,
                forceFacing.DataBits06,
                forceFacing.DataBits07,
                forceFacing.DataBits08,
                forceFacing.DataBits09);
        }

        public static void TraceForcedMove(ISpell spell, IUnitEntity target, SpellEffectForcedMoveSemantics forcedMove, float speed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics forced-move spell4Id={0} castingId={1} target={2} movementType={3} durationMs={4} flags={5} dataFloat01={6} dataFloat02={7} gravity={8} dataFloat06={9} dataFloat07={10} dataFloat08={11} dataBits09={12} appliedSpeed={13}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                forcedMove.MovementType,
                forcedMove.DurationTime,
                forcedMove.Flags,
                forcedMove.DataFloat01,
                forcedMove.DataFloat02,
                forcedMove.Gravity,
                forcedMove.DataFloat06,
                forcedMove.DataFloat07,
                forcedMove.DataFloat08,
                forcedMove.DataBits09,
                speed);
        }

        public static void TraceCCStateBreak(ISpell spell, IUnitEntity target, SpellEffectCCStateBreakSemantics ccStateBreak, uint beforeMask, uint afterMask, IReadOnlyCollection<(CCState State, uint EffectId)> removedStates)
        {
            if (!log.IsTraceEnabled)
                return;

            removedStates ??= Array.Empty<(CCState State, uint EffectId)>();

            log.Trace(
                "SpellDiagnostics cc-state-break spell4Id={0} castingId={1} target={2} stateMask={3} beforeMask={4} beforeStates={5} afterMask={6} afterStates={7} dataBits01={8} dataBits02={9} dataBits03={10} dataBits04={11} dataBits05={12} removedCount={13} removedStates={14} handlerCandidate={15}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                ccStateBreak.StateMask,
                beforeMask,
                FormatCCStateMask(beforeMask),
                afterMask,
                FormatCCStateMask(afterMask),
                ccStateBreak.DataBits01,
                ccStateBreak.DataBits02,
                ccStateBreak.DataBits03,
                ccStateBreak.DataBits04,
                ccStateBreak.DataBits05,
                removedStates.Count,
                FormatRemovedCCStates(removedStates),
                CombatLogHandlerCandidate.CCStateBreak);
        }

        private static string FormatCCStateMask(uint stateMask)
        {
            string[] states = Enum.GetValues<CCState>()
                .Where(state => (stateMask & (1u << (int)state)) != 0u)
                .Select(state => state.ToString())
                .ToArray();

            return states.Length > 0 ? string.Join(", ", states) : "none";
        }

        private static string FormatRemovedCCStates(IEnumerable<(CCState State, uint EffectId)> removedStates)
        {
            string[] values = removedStates.Select(removed => $"{removed.State}:{removed.EffectId}").ToArray();
            return values.Length > 0 ? string.Join(", ", values) : "none";
        }

        public static void TracePlayerCollection(ISpell spell, IUnitEntity target, string family, uint playerGuid, uint objectId, uint resolvedId, bool applied, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics player-collection spell4Id={0} baseSpell4Id={1} castingId={2} target={3} family={4} player={5} objectId={6} resolvedId={7} applied={8} skippedReason={9}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                family,
                playerGuid,
                objectId,
                resolvedId,
                applied,
                skippedReason);
        }

        public static void TraceForceRemove(ISpell spell, IUnitEntity target, SpellEffectForceRemoveSemantics forceRemove, string removeScope, int removedCount, bool removedProperties, int removedCCStates, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics force-remove spell4Id={0} castingId={1} target={2} removeType={3} removeTargetId={4} removeScope={5} dataBits02={6} dataBits03={7} dataBits04={8} dataBits05={9} dataBits06={10} removedCount={11} removedProperties={12} removedCCStates={13} skippedReason={14}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                forceRemove.RemoveType,
                forceRemove.Spell4Id,
                removeScope,
                forceRemove.DataBits02,
                forceRemove.DataBits03,
                forceRemove.DataBits04,
                forceRemove.DataBits05,
                forceRemove.DataBits06,
                removedCount,
                removedProperties,
                removedCCStates,
                skippedReason);
        }

        public static void TraceForceRemove(ISpell spell, IWorldEntity target, SpellEffectForceRemoveSemantics forceRemove, string removeScope, int removedBusyStates, string skippedReason)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics force-remove spell4Id={0} castingId={1} target={2} removeType={3} removeTargetId={4} removeScope={5} dataBits02={6} dataBits03={7} dataBits04={8} dataBits05={9} dataBits06={10} removedBusyStates={11} skippedReason={12}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                forceRemove.RemoveType,
                forceRemove.Spell4Id,
                removeScope,
                forceRemove.DataBits02,
                forceRemove.DataBits03,
                forceRemove.DataBits04,
                forceRemove.DataBits05,
                forceRemove.DataBits06,
                removedBusyStates,
                skippedReason);
        }

        public static void TraceProxy(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info, SpellEffectProxySemantics proxy)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics proxy spell4Id={0} castingId={1} target={2} spell4EffectId={3} effectType={4} proxySpell4Id={5} delayMs={6} tickMs={7} durationMs={8}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                info.Entry.Id,
                info.Entry.EffectType,
                proxy.Spell4Id,
                info.Entry.DelayTime,
                info.Entry.TickTime,
                info.Entry.DurationTime);
        }

        public static void TraceDespawnUnit(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info, bool removed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics despawn-unit spell4Id={0} castingId={1} target={2} spell4EffectId={3} delayMs={4} durationMs={5} removed={6}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                info.Entry.Id,
                info.Entry.DelayTime,
                info.Entry.DurationTime,
                removed);
        }

        public static void TraceActivate(ISpell spell, IWorldEntity target, SpellEffectActivateSemantics activate, uint playerGuid, uint activatedCreatureId, int targetGroupCount)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics activate spell4Id={0} castingId={1} target={2} player={3} activatedCreatureId={4} targetGroupCount={5} dataBits00={6} dataBits01={7} dataBits02={8} dataBits03={9} dataBits04={10} dataBits05={11}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                playerGuid,
                activatedCreatureId,
                targetGroupCount,
                activate.DataBits00,
                activate.DataBits01,
                activate.DataBits02,
                activate.DataBits03,
                activate.DataBits04,
                activate.DataBits05);
        }

        public static void TraceStealth(ISpell spell, IUnitEntity target, SpellEffectStateSemantics stealth, bool exiting, bool changed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics stealth spell4Id={0} castingId={1} target={2} exiting={3} changed={4} dataBits00={5} dataBits01={6} dataBits02={7} dataBits03={8} dataBits04={9} dataBits05={10}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                exiting,
                changed,
                stealth.DataBits00,
                stealth.DataBits01,
                stealth.DataBits02,
                stealth.DataBits03,
                stealth.DataBits04,
                stealth.DataBits05);
        }

        public static void TraceAggroImmune(ISpell spell, IUnitEntity target, SpellEffectStateSemantics aggroImmune, bool changed)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics aggro-immune spell4Id={0} castingId={1} target={2} changed={3} dataBits00={4} dataBits01={5} dataBits02={6} dataBits03={7} dataBits04={8} dataBits05={9}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                target.Guid,
                changed,
                aggroImmune.DataBits00,
                aggroImmune.DataBits01,
                aggroImmune.DataBits02,
                aggroImmune.DataBits03,
                aggroImmune.DataBits04,
                aggroImmune.DataBits05);
        }

        public static void TraceSpellGo(ISpell spell, int targetInfoCount, int effectInfoCount, int combatLogCount)
        {
            SpellRuntimeEvidenceCollector.RecordPacketEvent(
                spell,
                "ServerSpellGo",
                "spell-go",
                targetInfoCount,
                effectInfoCount,
                combatLogCount);

            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics spell-go spell4Id={0} baseSpell4Id={1} castingId={2} targetInfoCount={3} effectInfoCount={4} combatLogCount={5}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                targetInfoCount,
                effectInfoCount,
                combatLogCount);
        }
    }
}
