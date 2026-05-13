using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.Effect;
using NLog;

namespace NexusForever.Game.Spell
{
    public static class SpellEffectDiagnostics
    {
        private static readonly ILogger log = LogManager.GetLogger("SpellEffectDiagnostics");

        public static void TraceTargetSelection(ISpell spell, IReadOnlyCollection<ISpellTargetInfo> targets, int telegraphCount)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics target-selection spell4Id={0} baseSpell4Id={1} castingId={2} caster={3} targetCount={4} telegraphCount={5} targets=[{6}]",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.Parameters.SpellInfo.BaseInfo.Entry.Id,
                spell.CastingId,
                spell.Caster.Guid,
                targets.Count,
                telegraphCount,
                string.Join("; ", targets.Select(t => $"{t.Entity.Guid}:{t.Flags}")));
        }

        public static void TraceEffectDispatch(ISpell spell, SpellEffectInterpretation effect, int targetCount, bool hasHandler)
        {
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

        public static void TraceEffectResult(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!log.IsTraceEnabled)
                return;

            log.Trace(
                "SpellDiagnostics effect-result spell4Id={0} castingId={1} spell4EffectId={2} effectUniqueId={3} target={4} dropEffect={5} combatLogCount={6} damageRaw={7} damageAdjusted={8} damageShieldAbsorb={9}",
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                info.Entry.Id,
                info.EffectId,
                target.Guid,
                info.DropEffect,
                info.CombatLogs.Count,
                info.Damage?.RawDamage,
                info.Damage?.AdjustedDamage,
                info.Damage?.ShieldAbsorbAmount);
        }

        public static void TraceSpellGo(ISpell spell, int targetInfoCount, int effectInfoCount, int combatLogCount)
        {
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
