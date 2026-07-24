using NexusForever.Game.Abstract.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Combat;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Spell, "A collection of commands to manage spells.", "spell")]
    public class SpellCommandCategory : CommandCategory
    {
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly IGameTableManager gameTableManager;

        public SpellCommandCategory(
            IGlobalSpellManager globalSpellManager,
            IGameTableManager gameTableManager)
        {
            this.globalSpellManager = globalSpellManager;
            this.gameTableManager   = gameTableManager;
        }

        [Command(Permission.SpellAdd, "Add a base spell to character, optionally supplying the tier.", "add")]
        [CommandTarget(typeof(IPlayer))]
        public void HandleSpellAdd(ICommandContext context,
            [Parameter("Spell base id to add to character.")]
            uint spell4BaseId,
            [Parameter("Tier of the base spell to add to character.")]
            byte? tier)
        {
            tier ??= 1;

            ISpellBaseInfo spellBaseInfo = globalSpellManager.GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
            {
                context.SendMessage($"Invalid spell base id {spell4BaseId}!");
                return;
            }

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier.Value);
            if (spellInfo == null)
            {
                context.SendMessage($"Invalid tier {tier.Value} for spell base id {spell4BaseId}!");
                return;
            }

            context.GetTargetOrInvoker<IPlayer>().SpellManager.AddSpell(spell4BaseId, tier.Value);
        }

        [Command(Permission.SpellCast, "Cast a base spell from a selected unit, or from the invoker at a selected world target, optionally supplying the tier.", "cast")]
        [CommandTarget(typeof(IWorldEntity))]
        public void HandleSpellCast(ICommandContext context,
            [Parameter("Spell base id to cast from target.")]
            uint spell4BaseId,
            [Parameter("Tier of the base spell to cast from target.")]
            byte? tier)
        {
            tier ??= 1;

            ISpellBaseInfo spellBaseInfo = globalSpellManager.GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
            {
                context.SendMessage($"Invalid spell base id {spell4BaseId}!");
                return;
            }

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier.Value);
            if (spellInfo == null)
            {
                context.SendMessage($"Invalid tier {tier.Value} for spell base id {spell4BaseId}!");
                return;
            }

            if (!TryResolveSpellCastSource(context, out IUnitEntity caster, out uint primaryTargetId))
            {
                context.SendError("Spell cast requires a unit caster, or a player invoker with a selected world target.");
                return;
            }

            caster.CastSpell(spell4BaseId, tier.Value, new SpellParameters
            {
                PrimaryTargetId        = primaryTargetId,
                UserInitiatedSpellCast = false
            });
        }

        [Command(Permission.SpellCast, "Cast a concrete Spell4 id from a selected unit, or from the invoker at a selected world target.", "cast4", "castid")]
        [CommandTarget(typeof(IWorldEntity))]
        public void HandleSpellCastSpell4(ICommandContext context,
            [Parameter("Concrete Spell4 id to cast from target.")]
            uint spell4Id)
        {
            if (!TryCastSpell4(context, spell4Id, false, false, out _))
                return;
        }

        [Command(Permission.SpellCast, "Cast a concrete Spell4 id and export a runtime evidence artifact when the spell finishes.", "capture4", "evidence4")]
        [CommandTarget(typeof(IWorldEntity))]
        public void HandleSpellCaptureSpell4(ICommandContext context,
            [Parameter("Concrete Spell4 id to cast from target and capture.")]
            uint spell4Id)
        {
            if (!TryCastSpell4(context, spell4Id, true, false, out CastResult result))
                return;

            context.SendMessage($"Spell4 {spell4Id} capture requested ({result}). Evidence artifacts are written under {SpellRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
        }

        [Command(Permission.SpellCast, "Cast a concrete Spell4 id, export runtime evidence, and emit test-only diagnostic spell broadcast packets for blocked immunity cases.", "diag4", "broadcast4")]
        [CommandTarget(typeof(IWorldEntity))]
        public void HandleSpellDiagnosticSpell4(ICommandContext context,
            [Parameter("Concrete Spell4 id to cast with runtime evidence and diagnostic broadcasts.")]
            uint spell4Id)
        {
            if (!TryCastSpell4(context, spell4Id, true, true, out CastResult result))
                return;

            context.SendMessage($"Spell4 {spell4Id} diagnostic capture requested ({result}). Evidence artifacts are written under {SpellRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
        }

        [Command(Permission.SpellCast, "Arm runtime evidence export for the next real client-originated spell request from the invoker.", "capturenext", "captureclient", "evidencenext")]
        public void HandleSpellCaptureNextClient(ICommandContext context)
        {
            if (!TryGetInvokerSession(context, out IWorldSession session))
                return;

            session.ArmNextClientSpellEvidenceCapture();
            context.SendMessage($"Next supported real client spell request will export a runtime evidence artifact under {SpellRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
        }

        [Command(Permission.SpellCast, "Arm runtime evidence export plus blocked-immunity diagnostic broadcasts for the next real client-originated spell request from the invoker.", "diagnext", "diagclient", "broadcastnext")]
        public void HandleSpellDiagnosticNextClient(ICommandContext context)
        {
            if (!TryGetInvokerSession(context, out IWorldSession session))
                return;

            session.ArmNextClientSpellEvidenceCapture(true);
            context.SendMessage($"Next supported real client spell request will export runtime evidence and emit guarded diagnostic broadcasts under {SpellRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
        }

        [Command(Permission.Spell, "List active proc registrations on the selected unit or invoker with conservative dispatch evidence labels.", "procstates", "procs", "procstate")]
        [CommandTarget(typeof(IUnitEntity))]
        public void HandleSpellInspectProcStates(ICommandContext context)
        {
            IUnitEntity target = context.GetTargetOrInvoker<IUnitEntity>();
            IReadOnlyCollection<ProcRegistrationSnapshot> procStates = target.CreateProcRegistrationSnapshot();
            if (procStates.Count == 0)
            {
                context.SendMessage($"Unit {target.Guid} has no active proc registrations.");
                return;
            }

            context.SendMessage($"Unit {target.Guid} active proc registrations: {procStates.Count}.");
            foreach (ProcRegistrationSnapshot procState in procStates)
            {
                ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(procState.TriggerEvent, procState.TargetData);
                context.SendMessage(
                    $"Proc effect {procState.EffectId}: holder {DescribeSpell4(procState.Spell4Id)}, casting {procState.CastingId}, trigger event {procState.TriggerEvent} ({boundary.TriggerEventLabel}, {(boundary.TriggerEventSupported ? "dispatch-supported" : "dispatch-unsupported")}), trigger spell4 {DescribeSpell4(procState.TriggerSpell4Id)}, chance {procState.Chance:R}, target data {procState.TargetData} ({boundary.TargetDataLabel}, route {boundary.TargetRouteLabel}), cooldown/sentinel {procState.CooldownMsOrSentinel}, cooldownRemaining {procState.CooldownRemainingSeconds:R}s, boundary {boundary.DispatchSupportLabel}, data {procState.DataBits05}/{procState.DataBits06}/{procState.DataBits07}/{procState.DataBits08}/{procState.DataBits09}");
            }

            context.SendMessage($"Use !spell capturenext before the real client action, !spell procreport after the live event for a structured registration/probe/dispatch artifact under {ProcRuntimeEvidenceCollector.GetOutputDirectoryHint()}, or !spell procunsupported for a compact unsupported-tail summary.");
        }

        [Command(Permission.Spell, "Export a structured proc evidence report for the selected unit or invoker, including recent registration/probe/dispatch observations and unsupported-tail summaries.", "procreport", "procevidence", "procreview")]
        [CommandTarget(typeof(IUnitEntity))]
        public void HandleSpellProcReport(ICommandContext context)
        {
            IUnitEntity target = context.GetTargetOrInvoker<IUnitEntity>();
            ProcRuntimeEvidenceSummary summary = ProcRuntimeEvidenceCollector.CreateSummary(target);
            string outputPath = ProcRuntimeEvidenceCollector.ExportReport(
                target,
                "manual-command",
                "Manual proc evidence report exported without widening unsupported trigger-event or targetData dispatch.");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                context.SendError("Failed to export proc evidence report. Check server logs for details.");
                return;
            }

            context.SendMessage(
                $"Proc evidence report exported to {outputPath} (active {summary.ActiveRegistrationCount}, unsupported active {summary.UnsupportedRegistrationCount}, recent observations {summary.RecentObservationCount}, recent unsupported {summary.RecentUnsupportedObservationCount}).");
        }

        [Command(Permission.Spell, "Summarize unsupported proc trigger-event and targetData tails on the selected unit or invoker, plus recent evidence-only probe/dispatch observations.", "procunsupported", "proctails", "proctail")]
        [CommandTarget(typeof(IUnitEntity))]
        public void HandleSpellProcUnsupported(ICommandContext context)
        {
            IUnitEntity target = context.GetTargetOrInvoker<IUnitEntity>();
            ProcRuntimeEvidenceSummary summary = ProcRuntimeEvidenceCollector.CreateSummary(target);
            if (summary.UnsupportedRegistrationCount == 0 && summary.RecentUnsupportedObservationCount == 0)
            {
                context.SendMessage($"Unit {target.Guid} has no unsupported proc trigger-event or targetData tails in the active registrations or recent observation buffer. Use !spell procreport for the full structured artifact under {ProcRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Unit {target.Guid} unsupported proc evidence: active {summary.UnsupportedRegistrationCount}/{summary.ActiveRegistrationCount}, recent unsupported observations {summary.RecentUnsupportedObservationCount}/{summary.RecentObservationCount}.");

            if (summary.UnsupportedActiveTriggerEvents.Count > 0)
                builder.AppendLine($"Active unsupported trigger events: {FormatProcEvidenceCounts(summary.UnsupportedActiveTriggerEvents)}.");
            if (summary.UnsupportedActiveTargetData.Count > 0)
                builder.AppendLine($"Active unsupported targetData tails: {FormatProcEvidenceCounts(summary.UnsupportedActiveTargetData)}.");
            if (summary.RecentUnsupportedTriggerEvents.Count > 0)
                builder.AppendLine($"Recent unsupported proc trigger events: {FormatProcEvidenceCounts(summary.RecentUnsupportedTriggerEvents)}.");
            if (summary.RecentUnsupportedTargetData.Count > 0)
                builder.AppendLine($"Recent unsupported proc targetData tails: {FormatProcEvidenceCounts(summary.RecentUnsupportedTargetData)}.");
            if (summary.RecentBlockedReasons.Count > 0)
                builder.AppendLine($"Recent blocked or skipped reasons: {FormatProcEvidenceCounts(summary.RecentBlockedReasons)}.");

            builder.AppendLine($"Use !spell procreport to export the full registration/probe/dispatch artifact under {ProcRuntimeEvidenceCollector.GetOutputDirectoryHint()} without widening dispatch.");
            context.SendMessage(builder.ToString());
        }

        [Command(Permission.Spell, "Inspect decoded spell effect rows for a base spell tier.", "inspect")]
        public void HandleSpellInspect(ICommandContext context,
            [Parameter("Spell base id to inspect.")]
            uint spell4BaseId,
            [Parameter("Tier of the base spell to inspect.")]
            byte? tier)
        {
            tier ??= 1;

            ISpellBaseInfo spellBaseInfo = globalSpellManager.GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
            {
                context.SendMessage($"Invalid spell base id {spell4BaseId}!");
                return;
            }

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier.Value);
            if (spellInfo == null)
            {
                context.SendMessage($"Invalid tier {tier.Value} for spell base id {spell4BaseId}!");
                return;
            }

            InspectSpell(context, spellBaseInfo, spellInfo, tier.Value);
        }

        [Command(Permission.Spell, "Inspect decoded spell effect rows for a concrete Spell4 id.", "inspect4", "inspectid")]
        public void HandleSpellInspectSpell4(ICommandContext context,
            [Parameter("Concrete Spell4 id to inspect.")]
            uint spell4Id)
        {
            Spell4Entry spell4Entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                context.SendMessage($"Invalid spell4 id {spell4Id}!");
                return;
            }

            ISpellBaseInfo spellBaseInfo = globalSpellManager.GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
            if (spellBaseInfo == null)
            {
                context.SendMessage($"Invalid spell base id {spell4Entry.Spell4BaseIdBaseSpell} for spell4 id {spell4Id}!");
                return;
            }

            var tier = (byte)spell4Entry.TierIndex;
            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier);
            if (spellInfo == null)
            {
                context.SendMessage($"Invalid tier {tier} for spell4 id {spell4Id}!");
                return;
            }

            InspectSpell(context, spellBaseInfo, spellInfo, tier);
        }

        [Command(Permission.SpellResetCooldown, "Reset a single spell cooldown for character, if no spell if supplied all cooldowns will be reset", "resetcooldown")]
        [CommandTarget(typeof(IPlayer))]
        public void HandleSpellResetCooldown(ICommandContext context,
            [Parameter("Spell id to reset cooldown for character.")]
            uint? spell4Id)
        {
            IPlayer target = context.GetTargetOrInvoker<IPlayer>();
            if (spell4Id.HasValue)
                target.SpellManager.SetSpellCooldown(spell4Id.Value, 0d);
            else
                target.SpellManager.ResetAllSpellCooldowns();
        }

        private static bool TryResolveSpellCastSource(ICommandContext context, out IUnitEntity caster, out uint primaryTargetId)
        {
            primaryTargetId = 0u;

            if (context.Target is IUnitEntity unitTarget)
            {
                caster = unitTarget;
                return true;
            }

            if (context.Target != null && context.Invoker is IUnitEntity invoker)
            {
                caster          = invoker;
                primaryTargetId = context.Target.Guid;
                return true;
            }

            if (context.Invoker is IUnitEntity invokerOnly)
            {
                caster = invokerOnly;
                return true;
            }

            caster = null;
            return false;
        }

        private static bool TryGetInvokerSession(ICommandContext context, out IWorldSession session)
        {
            session = (context.Invoker as IPlayer)?.Session as IWorldSession;
            if (session != null)
                return true;

            context.SendError("This command requires a player invoker with an active world session.");
            return false;
        }

        private bool TryCastSpell4(ICommandContext context, uint spell4Id, bool captureRuntimeEvidence, bool emitDiagnosticSpellBroadcasts, out CastResult castResult)
        {
            Spell4Entry spell4Entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                context.SendMessage($"Invalid spell4 id {spell4Id}!");
                castResult = CastResult.SpellUnknown;
                return false;
            }

            if (!TryResolveSpellCastSource(context, out IUnitEntity caster, out uint primaryTargetId))
            {
                context.SendError("Spell cast requires a unit caster, or a player invoker with a selected world target.");
                castResult = CastResult.CasterUnknown;
                return false;
            }

            castResult = caster.TryCastSpell(spell4Id, new SpellParameters
            {
                PrimaryTargetId = primaryTargetId,
                UserInitiatedSpellCast = false,
                CaptureRuntimeEvidence = captureRuntimeEvidence,
                EmitDiagnosticSpellBroadcasts = emitDiagnosticSpellBroadcasts
            });

            if (castResult != CastResult.Ok)
            {
                context.SendError($"Spell4 {spell4Id} failed to cast: {castResult}.");
                return false;
            }

            return true;
        }

        private void InspectSpell(ICommandContext context, ISpellBaseInfo spellBaseInfo, ISpellInfo spellInfo, byte tier)
        {
            Spell4Entry entry = spellInfo.Entry;

            context.SendMessage($"Spell base {spellBaseInfo.Entry.Id}, tier {tier}, spell4 {entry.Id}: class {spellBaseInfo.SpellClass}, castMethod {spellBaseInfo.CastMethod}, school {spellBaseInfo.School}, tags [{FormatSpellTags(entry)}], effects {spellInfo.Effects.Count}, telegraphs {spellInfo.Telegraphs.Count}.");
            if (!string.IsNullOrWhiteSpace(entry.Description))
                context.SendMessage($"Description: {entry.Description}");

            context.SendMessage($"Timing cast/duration/cooldown {entry.CastTime}/{entry.SpellDuration}/{entry.SpellCoolDown}ms, GCD enum {entry.GlobalCooldownEnum}, global cooldown id {entry.SpellCoolDownIdGlobal}.");
            context.SendMessage($"ChannelData {DescribeChannelData(entry)}, ProxyChannelData [{DescribeProxyChannelData(spellInfo)}].");
            context.SendMessage($"Range min/max/vertical {entry.TargetMinRange:R}/{entry.TargetMaxRange:R}/{entry.TargetVerticalRange:R}, AbilityCharges static {DescribeStaticAbilityCharges(entry)}, runtime {DescribeRuntimeAbilityCharges(context, spellBaseInfo)}, thresholdTime {entry.ThresholdTime}.");
            context.SendMessage($"Targeting weaponSlot {spellBaseInfo.Entry.WeaponSlot}, flags {DescribeTargetingFlags(spellBaseInfo.TargetingFlags)}, freeform {spellBaseInfo.IsFreeformTarget}, movingInterrupted {spellBaseInfo.IsMovingInterrupted}.");
            context.SendMessage($"Flags property 0x{entry.PropertyFlags:X8}, beneficial {spellInfo.IsBeneficial}, hideCooldownTooltip {spellInfo.HideCooldownInTooltip}, serviceTokenCost {DescribeServiceTokenCost(spellInfo)}.");
            context.SendMessage($"Costs innate [{DescribeInnateCosts(entry)}], abilityPointCost {entry.AbilityPointCost}.");
            context.SendMessage($"Innate requirements caster [{DescribeCasterInnateRequirements(entry)}], target {DescribeTargetInnateRequirement(entry)}.");
            context.SendMessage($"LAS tierDesc {DescribeLocalizedText(entry.LocalizedTextIdLASTier)}, bonusEachTierDesc {DescribeLocalizedText(spellBaseInfo.Entry.LocalizedTextIdLASTierPoint)}.");
            context.SendMessage($"Hooks castEvents [{FormatNonZero(entry.Spell4IdCastEvent00, entry.Spell4IdCastEvent01, entry.Spell4IdCastEvent02, entry.Spell4IdCastEvent03)}], runners [{FormatNonZero(entry.Spell4RunnerId00, entry.Spell4RunnerId01)}], runnerPrereqs [{FormatNonZero(entry.PrerequisiteIdRunners)}], alternate {entry.Spell4IdMechanicAlternateSpell}, petSwitch {entry.Spell4IdPetSwitch}.");
            context.SendMessage($"Thresholds [{DescribeThresholds(entry.Id)}].");
            context.SendMessage($"Prereqs baseFlags {DescribePrerequisiteFlags(spellBaseInfo.PrerequisiteFlags)}, casterCast {entry.PrerequisiteIdCasterCast}, targetCast {entry.PrerequisiteIdTargetCast}, casterPersist {entry.PrerequisiteIdCasterPersistence}, targetPersist {entry.PrerequisiteIdTargetPersistence}, aoeTarget {entry.PrerequisiteIdAoeTarget}, aoePreferred {entry.PrerequisiteIdAoePreferredTarget}.");
            context.SendMessage($"TargetMechanics {DescribeTargetMechanics(spellBaseInfo.TargetMechanics)}, TargetAngle {DescribeTargetAngle(spellBaseInfo.TargetAngle)}, ValidTargets {DescribeValidTargets(spellBaseInfo.ValidTargets)}, AoeConstraints {DescribeAoeConstraints(spellInfo.AoeTargetConstraints)}, StackGroup {DescribeStackGroup(spellInfo.StackGroup)}.");

            foreach (TelegraphDamageEntry telegraph in spellInfo.Telegraphs)
            {
                context.SendMessage($"Telegraph {telegraph.Id}: shape {telegraph.DamageShapeEnum}, subtype {telegraph.TelegraphSubtypeEnum}, params {telegraph.Param00:R}/{telegraph.Param01:R}/{telegraph.Param02:R}/{telegraph.Param03:R}/{telegraph.Param04:R}/{telegraph.Param05:R}, time start/end/ramp {telegraph.TelegraphTimeStartMs}/{telegraph.TelegraphTimeEndMs}/{telegraph.TelegraphTimeRampInMs}/{telegraph.TelegraphTimeRampOutMs}ms, targetFlags {telegraph.TargetTypeFlags}, phase {telegraph.PhaseFlags}, casterPrereq {telegraph.PrerequisiteIdCaster}.");
            }

            foreach (SpellEffectInterpretation effect in spellInfo.Effects.Select(SpellEffectInterpreter.Interpret))
            {
                context.SendMessage($"Effect {effect.Entry.Id} order {effect.Entry.OrderIndex}: {effect.Entry.EffectType}, targetFlags {effect.Entry.TargetFlags}, damageType {effect.Entry.DamageType}, flags {effect.Entry.Flags}, phase {effect.Entry.PhaseFlags}, group {effect.Entry.Spell4EffectGroupListId}, timing delay/tick/duration {effect.Timing.DelayTime}/{effect.Timing.TickTime}/{effect.Timing.DurationTime}.");
                context.SendMessage($"  Prereqs apply caster/target {effect.Entry.PrerequisiteIdCasterApply}/{effect.Entry.PrerequisiteIdTargetApply}, persist caster/target {effect.Entry.PrerequisiteIdCasterPersistence}/{effect.Entry.PrerequisiteIdTargetPersistence}, suspend target {effect.Entry.PrerequisiteIdTargetSuspend}.");
                context.SendMessage($"  Costs perTick [{DescribeEffectInnateCosts(effect.Entry)}], effectEmm {DescribeEffectEmm(effect.Entry)}.");
                context.SendMessage($"  Semantics: {DescribeEffectSemantics(effect)}");
                if (effect.CCStateBreak != null)
                    context.SendMessage("  Runtime diagnostics: beforeMask/afterMask + removedStates(state:effectId) are emitted by TraceCCStateBreak; combat-log writes caster-context + eState per removed state, while strState is client-local display text.");
                context.SendMessage($"  DataBits: {effect.FormatDataBits()}");
                context.SendMessage($"  Parameters: {effect.FormatParameters()}");
            }
        }

        private static string DescribeTargetMechanics(Spell4TargetMechanicsEntry entry)
        {
            return entry != null
                ? $"{entry.Id} type {entry.TargetType}{DescribeTargetMechanicType(entry.TargetType)} flags {entry.Flags}"
                : "0";
        }

        private static string DescribeTargetMechanicType(uint targetType)
        {
            string label = targetType switch
            {
                0 => "unresolved client type 0 (IsSelfSpell candidate)",
                1 => "single target",
                2 => "self AOE",
                3 => "target AOE",
                4 => "position AOE",
                5 => "chain target",
                6 => "unresolved client type 6",
                7 => "unresolved client type 7 (service-lookup resolved-spell branch)",
                _ => null
            };

            return label != null ? $" ({label})" : string.Empty;
        }

        private static string DescribeProcTriggerEventCandidate(uint triggerEvent)
        {
            ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(triggerEvent, 1u);
            if (!boundary.TriggerEventSupported)
                return " (dispatch-unsupported, evidence-only)";

            return $" ({boundary.TriggerEventLabel}, dispatch-supported)";
        }

        private static string DescribeProcTargetDataCandidate(uint targetData)
        {
            ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(ProcTriggerEventCandidate.DealDamage, targetData);
            if (!boundary.TargetDataSupported)
                return " (dispatch-unsupported, evidence-only tail)";

            return $" ({boundary.TargetDataLabel}, route {boundary.TargetRouteLabel}, dispatch-supported)";
        }

        private static string DescribeRavelSignalReceiverCandidate(uint mode)
        {
            RavelSignalReceiverEvidenceBoundarySnapshot boundary = RavelSignalReceiverEvidenceBoundary.Describe(mode);
            if (!boundary.IsConservativelyDispatchSupported)
                return " (receiver diagnostics-only, evidence-only)";

            return $" ({boundary.ModeLabel}, dispatch-supported)";
        }

        private static string FormatProcEvidenceCounts(IEnumerable<ProcRuntimeEvidenceValueCount> counts)
        {
            return string.Join(", ", counts.Select(count =>
                !string.IsNullOrWhiteSpace(count.Label)
                    ? $"{count.Value} ({count.Label}) x{count.Count}"
                    : $"{count.Value} x{count.Count}"));
        }

        private static string FormatProcEvidenceCounts(IEnumerable<ProcRuntimeEvidenceStringCount> counts)
        {
            return string.Join(", ", counts.Select(count => $"{count.Value} x{count.Count}"));
        }

        private static string DescribeCombatLogHandlerCandidate(SpellEffectType effectType)
        {
            string label = effectType switch
            {
                SpellEffectType.CCStateSet           => CombatLogHandlerCandidate.CCState,
                SpellEffectType.ModifyInterruptArmor => CombatLogHandlerCandidate.ModifyInterruptArmor,
                SpellEffectType.CCStateBreak         => CombatLogHandlerCandidate.CCStateBreak,
                _                                    => null
            };

            return label != null ? $", combat-log handler {label}" : string.Empty;
        }

        private static string DescribeValidTargets(Spell4ValidTargetsEntry entry)
        {
            return entry != null
                ? $"{entry.Id} bitmask {entry.TargetBitmask}{DescribeValidTargetMask(entry.TargetBitmask)}"
                : "0";
        }

        private static string DescribeTargetAngle(Spell4TargetAngleEntry entry)
        {
            return entry != null
                ? $"{entry.Id} angle {entry.TargetAngle:R}"
                : "0";
        }

        private static string DescribeAoeConstraints(Spell4AoeTargetConstraintsEntry entry)
        {
            return entry != null
                ? $"{entry.Id} angle {entry.Angle:R}, targets {entry.TargetCount}, range {entry.MinRange:R}-{entry.MaxRange:R}, selection {entry.TargetSelection}{DescribeAoeTargetSelection(entry.TargetSelection)}, client booleans dead/cluster/combat unresolved"
                : "0";
        }

        private static string DescribeChannelData(Spell4Entry entry)
        {
            return HasChannelData(entry)
                ? $"initialDelay {FormatSeconds(entry.ChannelInitialDelay)}, maxTime {FormatSeconds(entry.ChannelMaxTime)}, pulseTime {FormatSeconds(entry.ChannelPulseTime)}, flags 0x{entry.SpellChannelFlags:X8}"
                : "none";
        }

        private static bool HasChannelData(Spell4Entry entry)
        {
            return entry != null
                && (entry.ChannelInitialDelay != 0u || entry.ChannelMaxTime != 0u || entry.ChannelPulseTime != 0u);
        }

        private string DescribeProxyChannelData(ISpellInfo spellInfo)
        {
            List<string> proxyChannels = [];
            foreach (SpellEffectInterpretation effect in spellInfo.Effects.Select(SpellEffectInterpreter.Interpret))
            {
                if (effect.Proxy == null)
                    continue;

                Spell4Entry proxyEntry = gameTableManager.Spell4.GetEntry(effect.Proxy.Spell4Id);
                if (!HasChannelData(proxyEntry))
                    continue;

                proxyChannels.Add($"effect {effect.Entry.Id} -> {DescribeSpell4(proxyEntry.Id)} channel {DescribeChannelData(proxyEntry)}");
            }

            return proxyChannels.Count > 0 ? string.Join("; ", proxyChannels) : "none";
        }

        private static string DescribeStaticAbilityCharges(Spell4Entry entry)
        {
            return entry.AbilityChargeCount > 0u
                ? $"max {entry.AbilityChargeCount}, rechargeTime {FormatSeconds(entry.AbilityRechargeTime)}, rechargeCount {entry.AbilityRechargeCount}"
                : "none";
        }

        private static string DescribeRuntimeAbilityCharges(ICommandContext context, ISpellBaseInfo spellBaseInfo)
        {
            IPlayer player = context.Target as IPlayer ?? context.Invoker as IPlayer;
            if (player == null)
                return "not available";

            ICharacterSpell characterSpell = player.SpellManager.GetSpell(spellBaseInfo.Entry.Id);
            if (characterSpell == null || characterSpell.MaxAbilityCharges == 0u)
                return "none";

            return $"remaining {characterSpell.AbilityCharges}/{characterSpell.MaxAbilityCharges}, fRechargeTime {characterSpell.AbilityRechargeTimeRemaining:R}s, fRechargePercentRemaining {characterSpell.AbilityRechargePercentRemaining:R}";
        }

        private static string FormatSeconds(uint milliseconds)
        {
            return $"{milliseconds / 1000d:R}s ({milliseconds}ms)";
        }

        private static string DescribeTargetingFlags(SpellTargetingFlags targetingFlags)
        {
            List<string> labels = [];
            if ((targetingFlags & SpellTargetingFlags.InterruptOnMove) != 0)
                labels.Add("interrupt-on-move");
            if ((targetingFlags & SpellTargetingFlags.FreeformTarget) != 0)
                labels.Add("freeform-target");

            return labels.Count == 0
                ? $"0x{(uint)targetingFlags:X8}"
                : $"0x{(uint)targetingFlags:X8} ({string.Join(", ", labels)})";
        }

        private static string DescribePrerequisiteFlags(SpellPrerequisiteFlags flags)
        {
            List<string> labels = [];
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.TargetAvoided, "bTargetAvoided");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.TargetBlocked, "bTargetBlocked");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.TargetGlancing, "bTargetGlancing");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.TargetFierce, "bTargetFierce");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.NotUsed, "bNOTUSED");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.CasterAvoided, "bCasterAvoided");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.CasterBlocked, "bCasterBlocked");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.CasterGlancing, "bCasterGlancing");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.CasterFierce, "bCasterFierce");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.NotUsed1, "bNOTUSED1");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.CasterSpellSuccess, "bCasterSpellSuccess");
            AppendPrerequisiteFlag(labels, flags, SpellPrerequisiteFlags.LastCasterSpellSuccess, "bLastCasterSpellSuccess");

            SpellPrerequisiteFlags knownFlags =
                SpellPrerequisiteFlags.TargetAvoided
                | SpellPrerequisiteFlags.TargetBlocked
                | SpellPrerequisiteFlags.TargetGlancing
                | SpellPrerequisiteFlags.TargetFierce
                | SpellPrerequisiteFlags.NotUsed
                | SpellPrerequisiteFlags.CasterAvoided
                | SpellPrerequisiteFlags.CasterBlocked
                | SpellPrerequisiteFlags.CasterGlancing
                | SpellPrerequisiteFlags.CasterFierce
                | SpellPrerequisiteFlags.NotUsed1
                | SpellPrerequisiteFlags.CasterSpellSuccess
                | SpellPrerequisiteFlags.LastCasterSpellSuccess;

            uint unknownFlags = (uint)(flags & ~knownFlags);
            if (unknownFlags != 0u)
                labels.Add($"unknown 0x{unknownFlags:X8}");

            return labels.Count == 0
                ? $"0x{(uint)flags:X8}"
                : $"0x{(uint)flags:X8} ({string.Join(", ", labels)})";
        }

        private static void AppendPrerequisiteFlag(List<string> labels, SpellPrerequisiteFlags flags, SpellPrerequisiteFlags flag, string label)
        {
            if ((flags & flag) != 0)
                labels.Add(label);
        }

        private string DescribeLocalizedText(uint textId)
        {
            if (textId == 0u)
                return "0 (none)";

            string text = gameTableManager.TextEnglish.GetEntry(textId);
            if (string.IsNullOrWhiteSpace(text))
                return $"{textId} (missing)";

            return $"{textId} \"{text.Replace("\r", " ").Replace("\n", " ").Trim()}\"";
        }

        private static string DescribeInnateCosts(Spell4Entry entry)
        {
            List<string> costs = [];
            AppendInnateCost(costs, 0, entry.InnateCostType0, entry.InnateCost0, entry.InnateCostEMMId0);
            AppendInnateCost(costs, 1, entry.InnateCostType1, entry.InnateCost1, entry.InnateCostEMMId1);
            return costs.Count > 0 ? string.Join("; ", costs) : "none";
        }

        private static void AppendInnateCost(List<string> costs, int slot, uint costType, uint costValue, uint emmId)
        {
            if (costType == 0u && costValue == 0u && emmId == 0u)
                return;

            string emmDescription = emmId != 0u
                ? $"emmId {emmId} (unknown semantics)"
                : "emmId 0";

            costs.Add($"slot{slot} {DescribeVital(costType)}, value {costValue}, {emmDescription}");
        }

        private static string DescribeEffectInnateCosts(Spell4EffectsEntry entry)
        {
            List<string> costs = [];
            AppendEffectInnateCost(costs, 0, entry.InnateCostPerTickType0, entry.InnateCostPerTick0);
            AppendEffectInnateCost(costs, 1, entry.InnateCostPerTickType1, entry.InnateCostPerTick1);
            return costs.Count > 0 ? string.Join("; ", costs) : "none";
        }

        private static void AppendEffectInnateCost(List<string> costs, int slot, uint costType, uint costValue)
        {
            if (costType == 0u && costValue == 0u)
                return;

            costs.Add($"slot{slot} {DescribeVital(costType)}, value {costValue}");
        }

        private static string DescribeEffectEmm(Spell4EffectsEntry entry)
        {
            if (entry.EmmComparison == 0u && entry.EmmValue == 0u)
                return "none";

            return $"comparison {entry.EmmComparison}, value {entry.EmmValue} (unknown semantics)";
        }

        private string DescribeThresholds(uint spell4Id)
        {
            GameTable<Spell4ThresholdsEntry> thresholdTable = gameTableManager.Spell4Thresholds;
            if (thresholdTable?.Entries == null)
                return "unavailable (Spell4Thresholds table not loaded)";

            Spell4ThresholdsEntry[] entries = thresholdTable.Entries
                .Where(entry => entry.Spell4IdParent == spell4Id)
                .OrderBy(entry => entry.OrderIndex)
                .ToArray();

            if (entries.Length == 0)
                return "none";

            return string.Join("; ", entries.Select(DescribeThreshold));
        }

        private string DescribeThreshold(Spell4ThresholdsEntry entry)
        {
            string iconDescription = string.IsNullOrWhiteSpace(entry.IconReplacement)
                ? "none"
                : $"\"{entry.IconReplacement}\"";

            return $"order {entry.OrderIndex}, cast {DescribeSpell4(entry.Spell4IdToCast)}, duration {entry.ThresholdDuration}ms, costs [{DescribeThresholdCosts(entry)}], tooltip {DescribeLocalizedText(entry.LocalizedTextIdTooltip)}, icon {iconDescription}, visualEffect {entry.VisualEffectId}";
        }

        private static string DescribeThresholdCosts(Spell4ThresholdsEntry entry)
        {
            List<string> costs = [];
            if (entry.VitalEnumCostType00 != 0u || entry.VitalCostValue00 != 0u)
                costs.Add($"slot0 {DescribeVital(entry.VitalEnumCostType00)}, value {entry.VitalCostValue00}");
            if (entry.VitalEnumCostType01 != 0u || entry.VitalCostValue01 != 0u)
                costs.Add($"slot1 {DescribeVital(entry.VitalEnumCostType01)}, value {entry.VitalCostValue01}");

            return costs.Count > 0 ? string.Join("; ", costs) : "none";
        }

        private static string DescribeCasterInnateRequirements(Spell4Entry entry)
        {
            List<string> requirements = [];
            AppendInnateRequirement(requirements, 0, entry.CasterInnateRequirement0, entry.CasterInnateRequirementValue0, entry.CasterInnateRequirementEval0);
            AppendInnateRequirement(requirements, 1, entry.CasterInnateRequirement1, entry.CasterInnateRequirementValue1, entry.CasterInnateRequirementEval1);
            return requirements.Count > 0 ? string.Join("; ", requirements) : "none";
        }

        private static string DescribeTargetInnateRequirement(Spell4Entry entry)
        {
            if (entry.TargetBeginInnateRequirement == 0u && entry.TargetBeginInnateRequirementValue == 0u && entry.TargetBeginInnateRequirementEval == 0u)
                return "none";

            return $"{DescribePrerequisiteType(entry.TargetBeginInnateRequirement)}, value {entry.TargetBeginInnateRequirementValue}, eval {DescribeEvaluationMode(entry.TargetBeginInnateRequirementEval)}";
        }

        private static void AppendInnateRequirement(List<string> requirements, int slot, uint requirementType, uint requirementValue, uint evaluationMode)
        {
            if (requirementType == 0u && requirementValue == 0u && evaluationMode == 0u)
                return;

            requirements.Add($"slot{slot} {DescribePrerequisiteType(requirementType)}, value {requirementValue}, eval {DescribeEvaluationMode(evaluationMode)}");
        }

        private static string DescribeVital(uint vital)
        {
            return Enum.IsDefined(typeof(Vital), (int)vital)
                ? $"{(Vital)vital} ({vital})"
                : vital.ToString();
        }

        private static string DescribePrerequisiteType(uint requirementType)
        {
            return Enum.IsDefined(typeof(PrerequisiteType), (int)requirementType)
                ? $"{(PrerequisiteType)requirementType} ({requirementType})"
                : requirementType.ToString();
        }

        private static string DescribeEvaluationMode(uint evaluationMode)
        {
            return Enum.IsDefined(typeof(EvaluationMode), (int)evaluationMode)
                ? $"{(EvaluationMode)evaluationMode} ({evaluationMode})"
                : evaluationMode.ToString();
        }

        private static string DescribeValidTargetMask(uint targetBitmask)
        {
            List<string> labels = [];
            if ((targetBitmask & 0x02u) != 0u)
                labels.Add("interactable/object");

            if ((targetBitmask & 0x08u) != 0u)
                labels.Add("dead/corpse");

            return labels.Count == 0 ? string.Empty : $" ({string.Join(", ", labels)})";
        }

        private static string DescribeAoeTargetSelection(uint targetSelection)
        {
            return Enum.IsDefined(typeof(AoeSelectionType), (int)targetSelection)
                ? $" ({(AoeSelectionType)targetSelection})"
                : string.Empty;
        }

        private static string DescribeStackGroup(Spell4StackGroupEntry entry)
        {
            return entry != null
                ? $"{entry.Id} cap {entry.StackCap}, type {entry.StackTypeEnum} (runtime oldest-only)"
                : "0";
        }

        private static string DescribeShieldOverloadPayload(SpellEffectShieldOverloadSemantics shieldOverload)
        {
            bool allZeroRuntimeShape = shieldOverload.DataBits00 == 0u
                && shieldOverload.DataBits01 == 0u
                && shieldOverload.DataBits02 == 0u;

            return allZeroRuntimeShape
                ? " (all-zero runtime-supported)"
                : " (non-zero payload diagnostic-only)";
        }

        private static string DescribeUnitState(uint stateId)
        {
            string stateName = UnitStateSetRules.DescribeState(stateId);
            if (stateName == null)
                return string.Empty;

            string behavior = UnitStateSetRules.BlocksHostileEffects(stateId)
                ? ", hostile-effect immunity"
                : string.Empty;

            return $" ({stateName}{behavior})";
        }

        private static string FormatNonZero(params uint[] values)
        {
            string[] nonZero = values?
                .Where(v => v != 0u)
                .Select(v => v.ToString())
                .ToArray();

            return nonZero is { Length: > 0 } ? string.Join(", ", nonZero) : "none";
        }

        private static string FormatSpellTags(Spell4Entry entry)
        {
            uint[] tagIds =
            [
                entry.Spell4TagId00,
                entry.Spell4TagId01,
                entry.Spell4TagId02,
                entry.Spell4TagId03,
                entry.Spell4TagId04
            ];

            string[] tags = tagIds
                .Where(t => t != 0u)
                .Select(t => Enum.IsDefined(typeof(SpellTag), (int)t) ? $"{(SpellTag)t} ({t})" : t.ToString())
                .ToArray();

            return tags.Length > 0 ? string.Join(", ", tags) : "none";
        }

        private static string DescribeServiceTokenCost(ISpellInfo spellInfo)
        {
            if (!spellInfo.HasServiceTokenCost)
                return spellInfo.ServiceTokenCostEntry != null
                    ? $"table-only/{spellInfo.ServiceTokenCostEntry.ServiceTokenCost}"
                    : "none";

            return spellInfo.ServiceTokenCostEntry != null
                ? spellInfo.ServiceTokenCostEntry.ServiceTokenCost.ToString()
                : "flagged/no-row";
        }

        internal string DescribeEffectSemantics(SpellEffectInterpretation effect)
        {
            if (effect.Absorption != null)
                return $"absorption amount formula multiplier {effect.Absorption.TypeMultiplier:R}, base value {effect.Absorption.TypeBaseValue:R}, type {effect.Absorption.AbsorptionType}, data {effect.Absorption.DataBits02}/{effect.Absorption.DataBits03}/{effect.Absorption.DataBits05}";

            if (effect.HealingAbsorption != null)
                return $"healing absorption amount formula multiplier {effect.HealingAbsorption.TypeMultiplier:R}, base value {effect.HealingAbsorption.TypeBaseValue:R}, mode {effect.HealingAbsorption.Mode}, data {effect.HealingAbsorption.DataBits03}/{effect.HealingAbsorption.DataBits04}/{effect.HealingAbsorption.DataBits05}";

            if (effect.Transference != null)
                return $"transference damage multiplier {effect.Transference.DamageMultiplier:R}, base value {effect.Transference.BaseValue}, source vital {effect.Transference.SourceVital}, healed vital {effect.Transference.HealedVital}, transfer rate {effect.Transference.TransferRate:R}, data {effect.Transference.DataBits04}";

            if (effect.Damage != null)
                return $"damage-family multiplier {effect.Damage.TypeMultiplier:R}, base value {effect.Damage.TypeBaseValue:R}";

            if (effect.VitalModifier != null)
                return $"vital {effect.VitalModifier.Vital}, amount bits {effect.VitalModifier.DataBits01}/{effect.VitalModifier.DataBits02}, data {effect.VitalModifier.DataBits03}/{effect.VitalModifier.DataBits04}, percent-like {effect.VitalModifier.DataFloat05:R}";

            if (effect.UnitStateSet != null)
                return $"unit state {effect.UnitStateSet.StateId}{DescribeUnitState(effect.UnitStateSet.StateId)}, data {effect.UnitStateSet.DataBits01}/{effect.UnitStateSet.DataBits02}/{effect.UnitStateSet.DataBits03}/{effect.UnitStateSet.DataBits04}/{effect.UnitStateSet.DataBits05}/{effect.UnitStateSet.DataBits06}/{effect.UnitStateSet.DataBits07}/{effect.UnitStateSet.DataBits08}/{effect.UnitStateSet.DataBits09}";

            if (effect.SetBusy != null)
                return $"set busy {effect.SetBusy.Busy}, mode {effect.SetBusy.Mode}, context {effect.SetBusy.ContextId}, data {effect.SetBusy.DataBits02}/{effect.SetBusy.DataBits03}/{effect.SetBusy.DataBits04}/{effect.SetBusy.DataBits05}/{effect.SetBusy.DataBits06}/{effect.SetBusy.DataBits07}/{effect.SetBusy.DataBits08}/{effect.SetBusy.DataBits09}";

            if (effect.SapVital != null)
                return $"sap vital {effect.SapVital.Vital}, mode {effect.SapVital.Mode}, amount floats {effect.SapVital.DataFloat01:R}/{effect.SapVital.DataFloat02:R}, data {effect.SapVital.DataBits04}/{effect.SapVital.DataFloat05:R}/{effect.SapVital.DataBits06}/{effect.SapVital.DataBits07}/{effect.SapVital.DataBits08}/{effect.SapVital.DataBits09}";

            if (effect.SummonCreature != null)
                return $"summon creature {DescribeCreature(effect.SummonCreature.CreatureId)}, placement data {effect.SummonCreature.DataBits03}/{effect.SummonCreature.DataBits04}/{effect.SummonCreature.DataBits05}, payload {effect.SummonCreature.DataBits01}/{effect.SummonCreature.DataBits02}/{effect.SummonCreature.DataBits06}/{effect.SummonCreature.DataBits07}/{effect.SummonCreature.DataBits08}/{effect.SummonCreature.DataBits09}";

            if (effect.SummonVehicle != null)
                return $"summon vehicle creature {DescribeCreature(effect.SummonVehicle.CreatureId)}, unit vehicle {effect.SummonVehicle.UnitVehicleId}, board mode {effect.SummonVehicle.BoardMode}, data {effect.SummonVehicle.DataBits03}/{effect.SummonVehicle.DataBits04}/{effect.SummonVehicle.DataBits05}";

            if (effect.SummonTrap != null)
                return $"summon trap creature {DescribeCreature(effect.SummonTrap.CreatureId)}, trigger spell4 {DescribeSpell4(effect.SummonTrap.TriggerSpell4Id)}, arm {effect.SummonTrap.ArmTimeMs}ms, radius {effect.SummonTrap.Radius:R}, data {effect.SummonTrap.DataBits03}/{effect.SummonTrap.DataBits05}/{effect.SummonTrap.DataBits06}/{effect.SummonTrap.DataBits07}";

            if (effect.NpcExecutionDelay != null)
                return $"npc execution delay payload {effect.NpcExecutionDelay.DataBits00}/{effect.NpcExecutionDelay.DataBits01}/{effect.NpcExecutionDelay.DataBits02}/{effect.NpcExecutionDelay.DataBits03}/{effect.NpcExecutionDelay.DataBits04}/{effect.NpcExecutionDelay.DataBits05}/{effect.NpcExecutionDelay.DataBits06}/{effect.NpcExecutionDelay.DataBits07}/{effect.NpcExecutionDelay.DataBits08}/{effect.NpcExecutionDelay.DataBits09}";

            if (effect.NpcForceAiMovement != null)
                return $"npc force AI movement using cast position, payload {effect.NpcForceAiMovement.DataBits00}/{effect.NpcForceAiMovement.DataBits01}/{effect.NpcForceAiMovement.DataBits02}/{effect.NpcForceAiMovement.DataBits03}/{effect.NpcForceAiMovement.DataBits04}/{effect.NpcForceAiMovement.DataBits05}/{effect.NpcForceAiMovement.DataBits06}/{effect.NpcForceAiMovement.DataBits07}/{effect.NpcForceAiMovement.DataBits08}/{effect.NpcForceAiMovement.DataBits09}";

            if (effect.RavelSignal != null)
                return $"ravel signal mode {effect.RavelSignal.Mode}{DescribeRavelSignalReceiverCandidate(effect.RavelSignal.Mode)}, signal {effect.RavelSignal.SignalId}, data {effect.RavelSignal.DataBits02}/{effect.RavelSignal.DataBits03}/{effect.RavelSignal.DataBits04}/{effect.RavelSignal.DataBits05}/{effect.RavelSignal.DataBits06}/{effect.RavelSignal.DataBits07}/{effect.RavelSignal.DataBits08}/{effect.RavelSignal.DataBits09}";

            if (effect.VendorPriceModifier != null)
                return $"vendor sell multiplier {effect.VendorPriceModifier.VendorSellMultiplier:R}, vendor buy multiplier {effect.VendorPriceModifier.VendorBuyMultiplier:R}, data {effect.VendorPriceModifier.DataBits02}/{effect.VendorPriceModifier.DataBits03}/{effect.VendorPriceModifier.DataBits04}/{effect.VendorPriceModifier.DataBits05}";

            if (effect.HazardEnable != null)
                return $"hazard enable id {effect.HazardEnable.HazardId}";

            if (effect.HazardModify != null)
                return $"hazard modify operation {effect.HazardModify.Operation}, target mode {effect.HazardModify.TargetMode}, modifier {effect.HazardModify.ModifierValue:R}, amount {effect.HazardModify.Amount:R}, hazard id {effect.HazardModify.HazardId}, data {effect.HazardModify.DataBits05}";

            if (effect.HazardSuspend != null)
                return $"hazard suspend id {effect.HazardSuspend.HazardId}, target mode {effect.HazardSuspend.TargetMode}, data {effect.HazardSuspend.DataBits02}/{effect.HazardSuspend.DataBits03}/{effect.HazardSuspend.DataBits04}/{effect.HazardSuspend.DataBits05}";

            if (effect.ModifyInterruptArmor != null)
                return $"modify interrupt armor amount {effect.ModifyInterruptArmor.Amount}, remove on interrupt {effect.ModifyInterruptArmor.RemoveOnInterrupt}, data {effect.ModifyInterruptArmor.DataBits02}/{effect.ModifyInterruptArmor.DataBits03}/{effect.ModifyInterruptArmor.DataBits04}/{effect.ModifyInterruptArmor.DataBits05}{DescribeCombatLogHandlerCandidate(effect.Entry.EffectType)}";

            if (effect.ThreatModification != null)
                return $"threat modification mode {effect.ThreatModification.Mode}, ratio/percent {effect.ThreatModification.RatioOrPercent:R}, value {effect.ThreatModification.ThreatValue}, data {effect.ThreatModification.DataBits02}/{effect.ThreatModification.DataBits04}/{effect.ThreatModification.DataBits05}";

            if (effect.ThreatTransfer != null)
                return $"threat transfer mode {effect.ThreatTransfer.Mode}, ratio/percent {effect.ThreatTransfer.RatioOrPercent:R}, data {effect.ThreatTransfer.DataBits02}/{effect.ThreatTransfer.DataBits03}/{effect.ThreatTransfer.DataBits04}/{effect.ThreatTransfer.DataBits05}";

            if (effect.Proc != null)
                return $"proc trigger event {effect.Proc.TriggerEvent}{DescribeProcTriggerEventCandidate(effect.Proc.TriggerEvent)}, trigger spell4 {DescribeSpell4(effect.Proc.TriggerSpell4Id)}, chance {effect.Proc.Chance:R}, target data {effect.Proc.TargetData}{DescribeProcTargetDataCandidate(effect.Proc.TargetData)}, cooldown/sentinel {effect.Proc.CooldownMsOrSentinel}, data {effect.Proc.DataBits05}/{effect.Proc.DataBits06}/{effect.Proc.DataBits07}/{effect.Proc.DataBits08}/{effect.Proc.DataBits09}";

            if (effect.DelayDeath != null)
                return $"delay death mode {effect.DelayDeath.Mode}, trigger spell4 {DescribeSpell4(effect.DelayDeath.TriggerSpell4Id)}, trigger delay {effect.DelayDeath.TriggerDelayMs}ms, data {effect.DelayDeath.DataBits03}/{effect.DelayDeath.DataBits04}/{effect.DelayDeath.DataBits05}/{effect.DelayDeath.DataBits06}/{effect.DelayDeath.DataBits07}/{effect.DelayDeath.DataBits08}/{effect.DelayDeath.DataBits09}";

            if (effect.ClampVital != null)
                return $"clamp vital mode {effect.ClampVital.Mode}, vital mode {effect.ClampVital.VitalMode}, ratio {effect.ClampVital.Ratio:R}, data {effect.ClampVital.DataBits03}/{effect.ClampVital.DataBits04}/{effect.ClampVital.DataBits05}/{effect.ClampVital.DataBits06}/{effect.ClampVital.DataBits07}/{effect.ClampVital.DataBits08}/{effect.ClampVital.DataBits09}";

            if (effect.ShieldOverload != null)
                return $"shield overload data {effect.ShieldOverload.DataBits00}/{effect.ShieldOverload.DataBits01}/{effect.ShieldOverload.DataBits02}/{effect.ShieldOverload.DataBits03}/{effect.ShieldOverload.DataBits04}/{effect.ShieldOverload.DataBits05}/{effect.ShieldOverload.DataBits06}/{effect.ShieldOverload.DataBits07}/{effect.ShieldOverload.DataBits08}/{effect.ShieldOverload.DataBits09}{DescribeShieldOverloadPayload(effect.ShieldOverload)}";

            if (effect.GrantXp != null)
                return $"grant xp amount {effect.GrantXp.Amount}, data {effect.GrantXp.DataBits01}/{effect.GrantXp.DataBits02}/{effect.GrantXp.DataBits03}/{effect.GrantXp.DataBits04}/{effect.GrantXp.DataBits05}";

            if (effect.PathXpModify != null)
                return $"path xp modify amount {effect.PathXpModify.Amount}, mode {effect.PathXpModify.Mode}, data {effect.PathXpModify.DataBits02}/{effect.PathXpModify.DataBits03}/{effect.PathXpModify.DataBits04}/{effect.PathXpModify.DataBits05}";

            if (effect.TradeSkillProfession != null)
                return $"learn tradeskill {effect.TradeSkillProfession.Tradeskill} ({(uint)effect.TradeSkillProfession.Tradeskill}), data {effect.TradeSkillProfession.DataBits01}/{effect.TradeSkillProfession.DataBits02}/{effect.TradeSkillProfession.DataBits03}/{effect.TradeSkillProfession.DataBits04}/{effect.TradeSkillProfession.DataBits05}/{effect.TradeSkillProfession.DataBits06}/{effect.TradeSkillProfession.DataBits07}/{effect.TradeSkillProfession.DataBits08}/{effect.TradeSkillProfession.DataBits09}";

            if (effect.PathMissionIncrement != null)
                return $"increment path mission {effect.PathMissionIncrement.PathMissionId} by {effect.PathMissionIncrement.Amount}, data {effect.PathMissionIncrement.DataBits02}/{effect.PathMissionIncrement.DataBits03}/{effect.PathMissionIncrement.DataBits04}/{effect.PathMissionIncrement.DataBits05}/{effect.PathMissionIncrement.DataBits06}/{effect.PathMissionIncrement.DataBits07}/{effect.PathMissionIncrement.DataBits08}/{effect.PathMissionIncrement.DataBits09}";

            if (effect.GrantLevelScaledXp != null)
                return $"grant level-scaled xp percent {effect.GrantLevelScaledXp.PercentOfLevel:R}, max level {effect.GrantLevelScaledXp.MaxLevel}, mode {effect.GrantLevelScaledXp.Mode}, data {effect.GrantLevelScaledXp.DataBits03}/{effect.GrantLevelScaledXp.DataBits04}/{effect.GrantLevelScaledXp.DataBits05}";

            if (effect.GrantLevelScaledPrestige != null)
                return $"grant level-scaled prestige (diagnostic only; formula unverified) percent {effect.GrantLevelScaledPrestige.PercentOfLevel:R}, max level {effect.GrantLevelScaledPrestige.MaxLevel}, mode {effect.GrantLevelScaledPrestige.Mode}, data {effect.GrantLevelScaledPrestige.DataBits03}/{effect.GrantLevelScaledPrestige.DataBits04}/{effect.GrantLevelScaledPrestige.DataBits05}";

            if (effect.GiveAugmentPowerToPlayer != null)
                return $"give augment power amount {effect.GiveAugmentPowerToPlayer.Amount}, data {effect.GiveAugmentPowerToPlayer.DataBits01}/{effect.GiveAugmentPowerToPlayer.DataBits02}/{effect.GiveAugmentPowerToPlayer.DataBits03}/{effect.GiveAugmentPowerToPlayer.DataBits04}/{effect.GiveAugmentPowerToPlayer.DataBits05}";

            if (effect.GiveAbilityPointsToPlayer != null)
                return $"give ability tier points amount {effect.GiveAbilityPointsToPlayer.Amount}, data {effect.GiveAbilityPointsToPlayer.DataBits01}/{effect.GiveAbilityPointsToPlayer.DataBits02}/{effect.GiveAbilityPointsToPlayer.DataBits03}/{effect.GiveAbilityPointsToPlayer.DataBits04}/{effect.GiveAbilityPointsToPlayer.DataBits05}/{effect.GiveAbilityPointsToPlayer.DataBits06}/{effect.GiveAbilityPointsToPlayer.DataBits07}/{effect.GiveAbilityPointsToPlayer.DataBits08}/{effect.GiveAbilityPointsToPlayer.DataBits09}";

            if (effect.ForceFacing != null)
                return $"force facing angle offset {effect.ForceFacing.AngleDegrees:R} degrees, uses offset {effect.ForceFacing.UsesAngleOffset}, turn {effect.ForceFacing.TurnDurationMs}ms, data {effect.ForceFacing.DataBits01}/{effect.ForceFacing.DataBits02}/{effect.ForceFacing.DataBits04}/{effect.ForceFacing.DataBits05}/{effect.ForceFacing.DataBits06}/{effect.ForceFacing.DataBits07}/{effect.ForceFacing.DataBits08}/{effect.ForceFacing.DataBits09}";

            if (effect.ForcedMove != null)
                return $"forced move type {effect.ForcedMove.MovementType}, duration {effect.ForcedMove.DurationTime}ms, flags {effect.ForcedMove.Flags}, floats {effect.ForcedMove.DataFloat01:R}/{effect.ForcedMove.DataFloat02:R}/{effect.ForcedMove.DataFloat06:R}/{effect.ForcedMove.DataFloat07:R}/{effect.ForcedMove.DataFloat08:R}";

            if (effect.VectorSlide != null)
                return $"vector slide mode {effect.VectorSlide.Mode}, magnitude {effect.VectorSlide.Magnitude:R}, flags {effect.VectorSlide.Flags}, data {effect.VectorSlide.DataBits03}/{effect.VectorSlide.DataBits04}/{effect.VectorSlide.DataBits05}";

            if (effect.Proxy != null)
                return $"proxy spell4 {DescribeSpell4(effect.Proxy.Spell4Id)}";

            if (effect.Teleport != null)
                return $"world location {effect.Teleport.WorldLocation2Id}";

            if (effect.HousingTeleport != null)
                return $"housing teleport mode {effect.HousingTeleport.Mode}, destination mode {effect.HousingTeleport.DestinationMode}, data {effect.HousingTeleport.DataBits02}/{effect.HousingTeleport.DataBits03}/{effect.HousingTeleport.DataBits04}/{effect.HousingTeleport.DataBits05}";

            if (effect.SupportStuck != null)
                return $"support stuck durability mode {effect.SupportStuck.DurabilityMode:R}, data {effect.SupportStuck.DataBits01}/{effect.SupportStuck.DataBits02}/{effect.SupportStuck.DataBits03}/{effect.SupportStuck.DataBits04}/{effect.SupportStuck.DataBits05}";

            if (effect.CCState != null)
                return $"cc state {effect.CCState.State}, apply flags {effect.CCState.ApplyRulesFlags}, additional data {effect.CCState.AdditionalDataId}";

            if (effect.CCStateBreak != null)
                return $"cc state break mask {effect.CCStateBreak.StateMask} [{FormatCCStateMask(effect.CCStateBreak.StateMask)}], data {effect.CCStateBreak.DataBits01}/{effect.CCStateBreak.DataBits02}/{effect.CCStateBreak.DataBits03}/{effect.CCStateBreak.DataBits04}/{effect.CCStateBreak.DataBits05}, runtime trace beforeMask/afterMask/removedStates{DescribeCombatLogHandlerCandidate(effect.Entry.EffectType)}";

            if (effect.ForceRemove != null)
                return $"force remove type {effect.ForceRemove.RemoveType}, spell4 {DescribeSpell4(effect.ForceRemove.Spell4Id)}, data {effect.ForceRemove.DataBits02}/{effect.ForceRemove.DataBits03}/{effect.ForceRemove.DataBits04}/{effect.ForceRemove.DataBits05}/{effect.ForceRemove.DataBits06}";

            if (effect.DespawnUnit != null)
                return $"despawn unit data {effect.DespawnUnit.DataBits00}/{effect.DespawnUnit.DataBits01}/{effect.DespawnUnit.DataBits02}/{effect.DespawnUnit.DataBits03}/{effect.DespawnUnit.DataBits04}";

            if (effect.Activate != null)
                return $"activate data {effect.Activate.DataBits00}/{effect.Activate.DataBits01}/{effect.Activate.DataBits02}/{effect.Activate.DataBits03}/{effect.Activate.DataBits04}/{effect.Activate.DataBits05}";

            if (effect.Disembark != null)
                return $"disembark mode {effect.Disembark.Mode}, data {effect.Disembark.DataBits01}/{effect.Disembark.DataBits02}/{effect.Disembark.DataBits03}/{effect.Disembark.DataBits04}/{effect.Disembark.DataBits05}";

            if (effect.ActionBarSet != null)
                return $"action bar shortcut set {effect.ActionBarSet.ActionBarShortcutSetId}, data {effect.ActionBarSet.DataBits01}/{effect.ActionBarSet.DataBits02}/{effect.ActionBarSet.DataBits03}/{effect.ActionBarSet.DataBits04}/{effect.ActionBarSet.DataBits05}";

            if (effect.DisguiseOutfit != null)
                return $"disguise outfit {effect.DisguiseOutfit.OutfitInfoId}, primary display {effect.DisguiseOutfit.PrimaryItemDisplayId}, secondary display {effect.DisguiseOutfit.SecondaryItemDisplayId}, data {effect.DisguiseOutfit.DataBits03}/{effect.DisguiseOutfit.DataFloat04:R}/{effect.DisguiseOutfit.DataFloat05:R}";

            if (effect.MimicDisguise != null)
                return $"mimic disguise data {effect.MimicDisguise.DataBits00}/{effect.MimicDisguise.DataBits01}/{effect.MimicDisguise.DataBits02}/{effect.MimicDisguise.DataBits03}/{effect.MimicDisguise.DataBits04}/{effect.MimicDisguise.DataBits05}";

            if (effect.Stealth != null)
                return $"stealth data {effect.Stealth.DataBits00}/{effect.Stealth.DataBits01}/{effect.Stealth.DataBits02}/{effect.Stealth.DataBits03}/{effect.Stealth.DataBits04}/{effect.Stealth.DataBits05}";

            if (effect.RemoveStealth != null)
                return $"remove stealth data {effect.RemoveStealth.DataBits00}/{effect.RemoveStealth.DataBits01}/{effect.RemoveStealth.DataBits02}/{effect.RemoveStealth.DataBits03}/{effect.RemoveStealth.DataBits04}/{effect.RemoveStealth.DataBits05}";

            if (effect.AggroImmune != null)
                return $"aggro immune data {effect.AggroImmune.DataBits00}/{effect.AggroImmune.DataBits01}/{effect.AggroImmune.DataBits02}/{effect.AggroImmune.DataBits03}/{effect.AggroImmune.DataBits04}/{effect.AggroImmune.DataBits05}";

            if (effect.SpellEffectImmunity != null)
                return $"spell effect immunity effect {effect.SpellEffectImmunity.EffectType} ({effect.SpellEffectImmunity.EffectTypeRaw}), data {effect.SpellEffectImmunity.DataBits01}/{effect.SpellEffectImmunity.DataBits02}/{effect.SpellEffectImmunity.DataBits03}/{effect.SpellEffectImmunity.DataBits04}/{effect.SpellEffectImmunity.DataBits05}";

            if (effect.SpellImmunity != null)
                return $"spell immunity mode {effect.SpellImmunity.Mode}, spell4 {DescribeSpell4(effect.SpellImmunity.Spell4Id)}, data {effect.SpellImmunity.DataBits02}/{effect.SpellImmunity.DataBits03}/{effect.SpellImmunity.DataBits04}/{effect.SpellImmunity.DataBits05}";

            if (effect.PersonalDmgHealMod != null)
                return $"personal damage/heal modifier type {effect.PersonalDmgHealMod.ModifierType}, priority {effect.PersonalDmgHealMod.Priority}, multiplier {effect.PersonalDmgHealMod.Multiplier:R}, data {effect.PersonalDmgHealMod.DataFloat03:R}/{effect.PersonalDmgHealMod.DataBits04}/{effect.PersonalDmgHealMod.DataBits05}";

            if (effect.UnitPropertyModifier != null)
                return $"property {effect.UnitPropertyModifier.Property}, priority {effect.UnitPropertyModifier.Priority}, mod hint {effect.UnitPropertyModifier.ModifierTypeHint?.ToString() ?? "unknown"}, percentage {effect.UnitPropertyModifier.PercentageValue:R}, flat {effect.UnitPropertyModifier.FlatValue:R}, level scale {effect.UnitPropertyModifier.LevelScaleValue:R}";

            if (effect.UnitPropertyConversion != null)
                return $"convert {effect.UnitPropertyConversion.SourceProperty} into {effect.UnitPropertyConversion.TargetProperty} at {effect.UnitPropertyConversion.Multiplier:R}, data {effect.UnitPropertyConversion.DataBits03}/{effect.UnitPropertyConversion.DataBits04}/{effect.UnitPropertyConversion.DataBits05}";

            return "unknown";
        }

        private static string FormatCCStateMask(uint stateMask)
        {
            string[] states = Enum.GetValues<CCState>()
                .Where(state => (stateMask & (1u << (int)state)) != 0u)
                .Select(state => state.ToString())
                .ToArray();

            return states.Length > 0 ? string.Join(", ", states) : "none";
        }

        private string DescribeSpell4(uint spell4Id)
        {
            if (spell4Id == 0u)
                return "0";

            Spell4Entry entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (entry == null)
                return $"{spell4Id} (missing)";

            return $"{entry.Id} base {entry.Spell4BaseIdBaseSpell} tier {entry.TierIndex} \"{entry.Description}\"";
        }

        private string DescribeCreature(uint creatureId)
        {
            if (creatureId == 0u)
                return "0";

            Creature2Entry entry = gameTableManager.Creature2.GetEntry(creatureId);
            if (entry == null)
                return $"{creatureId} (missing)";

            return creatureId.ToString();
        }
    }
}
