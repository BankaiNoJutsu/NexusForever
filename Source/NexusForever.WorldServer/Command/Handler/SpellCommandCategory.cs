using NexusForever.Game.Abstract.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.WorldServer.Command.Context;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Spell, "A collection of commands to manage spells.", "spell")]
    public class SpellCommandCategory : CommandCategory
    {
        [Command(Permission.SpellAdd, "Add a base spell to character, optionally supplying the tier.", "add")]
        [CommandTarget(typeof(IPlayer))]
        public void HandleSpellAdd(ICommandContext context,
            [Parameter("Spell base id to add to character.")]
            uint spell4BaseId,
            [Parameter("Tier of the base spell to add to character.")]
            byte? tier)
        {
            tier ??= 1;

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
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

        [Command(Permission.SpellCast, "Cast a base spell for target, optionally supplying the tier.", "cast")]
        [CommandTarget(typeof(IUnitEntity))]
        public void HandleSpellCast(ICommandContext context,
            [Parameter("Spell base id to cast from target.")]
            uint spell4BaseId,
            [Parameter("Tier of the base spell to cast from target.")]
            byte? tier)
        {
            tier ??= 1;

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
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

            context.GetTargetOrInvoker<IUnitEntity>().CastSpell(spell4BaseId, tier.Value, new SpellParameters
            {
                UserInitiatedSpellCast = false
            });
        }

        [Command(Permission.SpellCast, "Cast a concrete Spell4 id for target.", "cast4", "castid")]
        [CommandTarget(typeof(IUnitEntity))]
        public void HandleSpellCastSpell4(ICommandContext context,
            [Parameter("Concrete Spell4 id to cast from target.")]
            uint spell4Id)
        {
            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                context.SendMessage($"Invalid spell4 id {spell4Id}!");
                return;
            }

            context.GetTargetOrInvoker<IUnitEntity>().CastSpell(spell4Id, new SpellParameters
            {
                UserInitiatedSpellCast = false
            });
        }

        [Command(Permission.Spell, "Inspect decoded spell effect rows for a base spell tier.", "inspect")]
        public void HandleSpellInspect(ICommandContext context,
            [Parameter("Spell base id to inspect.")]
            uint spell4BaseId,
            [Parameter("Tier of the base spell to inspect.")]
            byte? tier)
        {
            tier ??= 1;

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
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
            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                context.SendMessage($"Invalid spell4 id {spell4Id}!");
                return;
            }

            ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
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

        private static void InspectSpell(ICommandContext context, ISpellBaseInfo spellBaseInfo, ISpellInfo spellInfo, byte tier)
        {
            Spell4Entry entry = spellInfo.Entry;

            context.SendMessage($"Spell base {spellBaseInfo.Entry.Id}, tier {tier}, spell4 {entry.Id}: class {spellBaseInfo.SpellClass}, castMethod {spellBaseInfo.CastMethod}, school {spellBaseInfo.School}, tags [{FormatSpellTags(entry)}], effects {spellInfo.Effects.Count}, telegraphs {spellInfo.Telegraphs.Count}.");
            if (!string.IsNullOrWhiteSpace(entry.Description))
                context.SendMessage($"Description: {entry.Description}");

            context.SendMessage($"Timing cast/duration/cooldown {entry.CastTime}/{entry.SpellDuration}/{entry.SpellCoolDown}ms, channel initial/max/pulse {entry.ChannelInitialDelay}/{entry.ChannelMaxTime}/{entry.ChannelPulseTime}ms, GCD enum {entry.GlobalCooldownEnum}, global cooldown id {entry.SpellCoolDownIdGlobal}.");
            context.SendMessage($"Range min/max/vertical {entry.TargetMinRange:R}/{entry.TargetMaxRange:R}/{entry.TargetVerticalRange:R}, charges count/rechargeTime/rechargeCount {entry.AbilityChargeCount}/{entry.AbilityRechargeTime}/{entry.AbilityRechargeCount}, thresholdTime {entry.ThresholdTime}.");
            context.SendMessage($"Flags property 0x{entry.PropertyFlags:X8}, beneficial {spellInfo.IsBeneficial}, hideCooldownTooltip {spellInfo.HideCooldownInTooltip}, serviceTokenCost {DescribeServiceTokenCost(spellInfo)}.");
            context.SendMessage($"Costs innate0 type/cost/emm {entry.InnateCostType0}/{entry.InnateCost0}/{entry.InnateCostEMMId0}, innate1 type/cost/emm {entry.InnateCostType1}/{entry.InnateCost1}/{entry.InnateCostEMMId1}, abilityPointCost {entry.AbilityPointCost}.");
            context.SendMessage($"Hooks castEvents [{FormatNonZero(entry.Spell4IdCastEvent00, entry.Spell4IdCastEvent01, entry.Spell4IdCastEvent02, entry.Spell4IdCastEvent03)}], runners [{FormatNonZero(entry.Spell4RunnerId00, entry.Spell4RunnerId01)}], runnerPrereqs [{FormatNonZero(entry.PrerequisiteIdRunners)}], alternate {entry.Spell4IdMechanicAlternateSpell}, petSwitch {entry.Spell4IdPetSwitch}.");
            context.SendMessage($"Prereqs casterCast {entry.PrerequisiteIdCasterCast}, targetCast {entry.PrerequisiteIdTargetCast}, casterPersist {entry.PrerequisiteIdCasterPersistence}, targetPersist {entry.PrerequisiteIdTargetPersistence}, aoeTarget {entry.PrerequisiteIdAoeTarget}, aoePreferred {entry.PrerequisiteIdAoePreferredTarget}.");
            context.SendMessage($"TargetMechanics {DescribeTargetMechanics(spellBaseInfo.TargetMechanics)}, TargetAngle {DescribeTargetAngle(spellBaseInfo.TargetAngle)}, ValidTargets {DescribeValidTargets(spellBaseInfo.ValidTargets)}, AoeConstraints {DescribeAoeConstraints(spellInfo.AoeTargetConstraints)}, StackGroup {DescribeStackGroup(spellInfo.StackGroup)}.");

            foreach (TelegraphDamageEntry telegraph in spellInfo.Telegraphs)
            {
                context.SendMessage($"Telegraph {telegraph.Id}: shape {telegraph.DamageShapeEnum}, subtype {telegraph.TelegraphSubtypeEnum}, params {telegraph.Param00:R}/{telegraph.Param01:R}/{telegraph.Param02:R}/{telegraph.Param03:R}/{telegraph.Param04:R}/{telegraph.Param05:R}, time start/end/ramp {telegraph.TelegraphTimeStartMs}/{telegraph.TelegraphTimeEndMs}/{telegraph.TelegraphTimeRampInMs}/{telegraph.TelegraphTimeRampOutMs}ms, targetFlags {telegraph.TargetTypeFlags}, phase {telegraph.PhaseFlags}, casterPrereq {telegraph.PrerequisiteIdCaster}.");
            }

            foreach (SpellEffectInterpretation effect in spellInfo.Effects.Select(SpellEffectInterpreter.Interpret))
            {
                context.SendMessage($"Effect {effect.Entry.Id} order {effect.Entry.OrderIndex}: {effect.Entry.EffectType}, targetFlags {effect.Entry.TargetFlags}, damageType {effect.Entry.DamageType}, flags {effect.Entry.Flags}, phase {effect.Entry.PhaseFlags}, group {effect.Entry.Spell4EffectGroupListId}, timing delay/tick/duration {effect.Timing.DelayTime}/{effect.Timing.TickTime}/{effect.Timing.DurationTime}.");
                context.SendMessage($"  Prereqs apply caster/target {effect.Entry.PrerequisiteIdCasterApply}/{effect.Entry.PrerequisiteIdTargetApply}, persist caster/target {effect.Entry.PrerequisiteIdCasterPersistence}/{effect.Entry.PrerequisiteIdTargetPersistence}, suspend target {effect.Entry.PrerequisiteIdTargetSuspend}.");
                context.SendMessage($"  Semantics: {DescribeEffectSemantics(effect)}");
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
                1 => "single target",
                2 => "self AOE",
                3 => "target AOE",
                4 => "position AOE",
                5 => "chain target",
                _ => null
            };

            return label != null ? $" ({label})" : string.Empty;
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
                ? $"{entry.Id} angle {entry.Angle:R}, targets {entry.TargetCount}, range {entry.MinRange:R}-{entry.MaxRange:R}, selection {entry.TargetSelection}{DescribeAoeTargetSelection(entry.TargetSelection)}"
                : "0";
        }

        private static string DescribeValidTargetMask(uint targetBitmask)
        {
            List<string> labels = [];
            if ((targetBitmask & 0x08u) != 0u)
                labels.Add("dead/corpse");

            return labels.Count == 0 ? string.Empty : $" ({string.Join(", ", labels)})";
        }

        private static string DescribeAoeTargetSelection(uint targetSelection)
        {
            string label = targetSelection switch
            {
                4 => "lowest absolute health",
                5 => "most missing health",
                _ => null
            };

            return label != null ? $" ({label})" : string.Empty;
        }

        private static string DescribeStackGroup(Spell4StackGroupEntry entry)
        {
            return entry != null
                ? $"{entry.Id} cap {entry.StackCap}, type {entry.StackTypeEnum}"
                : "0";
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

        private static string DescribeEffectSemantics(SpellEffectInterpretation effect)
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
                return $"unit state {effect.UnitStateSet.StateId}, data {effect.UnitStateSet.DataBits01}/{effect.UnitStateSet.DataBits02}/{effect.UnitStateSet.DataBits03}/{effect.UnitStateSet.DataBits04}/{effect.UnitStateSet.DataBits05}/{effect.UnitStateSet.DataBits06}/{effect.UnitStateSet.DataBits07}/{effect.UnitStateSet.DataBits08}/{effect.UnitStateSet.DataBits09}";

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

            if (effect.RavelSignal != null)
                return $"ravel signal mode {effect.RavelSignal.Mode}, signal {effect.RavelSignal.SignalId}, data {effect.RavelSignal.DataBits02}/{effect.RavelSignal.DataBits03}/{effect.RavelSignal.DataBits04}/{effect.RavelSignal.DataBits05}/{effect.RavelSignal.DataBits06}/{effect.RavelSignal.DataBits07}/{effect.RavelSignal.DataBits08}/{effect.RavelSignal.DataBits09}";

            if (effect.ModifyInterruptArmor != null)
                return $"modify interrupt armor amount {effect.ModifyInterruptArmor.Amount}, remove on interrupt {effect.ModifyInterruptArmor.RemoveOnInterrupt}, data {effect.ModifyInterruptArmor.DataBits02}/{effect.ModifyInterruptArmor.DataBits03}/{effect.ModifyInterruptArmor.DataBits04}/{effect.ModifyInterruptArmor.DataBits05}";

            if (effect.ThreatModification != null)
                return $"threat modification mode {effect.ThreatModification.Mode}, ratio/percent {effect.ThreatModification.RatioOrPercent:R}, value {effect.ThreatModification.ThreatValue}, data {effect.ThreatModification.DataBits02}/{effect.ThreatModification.DataBits04}/{effect.ThreatModification.DataBits05}";

            if (effect.ThreatTransfer != null)
                return $"threat transfer mode {effect.ThreatTransfer.Mode}, ratio/percent {effect.ThreatTransfer.RatioOrPercent:R}, data {effect.ThreatTransfer.DataBits02}/{effect.ThreatTransfer.DataBits03}/{effect.ThreatTransfer.DataBits04}/{effect.ThreatTransfer.DataBits05}";

            if (effect.Proc != null)
                return $"proc trigger event {effect.Proc.TriggerEvent}, trigger spell4 {DescribeSpell4(effect.Proc.TriggerSpell4Id)}, chance {effect.Proc.Chance:R}, target data {effect.Proc.TargetData}, cooldown/sentinel {effect.Proc.CooldownMsOrSentinel}, data {effect.Proc.DataBits05}/{effect.Proc.DataBits06}/{effect.Proc.DataBits07}/{effect.Proc.DataBits08}/{effect.Proc.DataBits09}";

            if (effect.DelayDeath != null)
                return $"delay death mode {effect.DelayDeath.Mode}, trigger spell4 {DescribeSpell4(effect.DelayDeath.TriggerSpell4Id)}, trigger delay {effect.DelayDeath.TriggerDelayMs}ms, data {effect.DelayDeath.DataBits03}/{effect.DelayDeath.DataBits04}/{effect.DelayDeath.DataBits05}/{effect.DelayDeath.DataBits06}/{effect.DelayDeath.DataBits07}/{effect.DelayDeath.DataBits08}/{effect.DelayDeath.DataBits09}";

            if (effect.ClampVital != null)
                return $"clamp vital mode {effect.ClampVital.Mode}, vital mode {effect.ClampVital.VitalMode}, ratio {effect.ClampVital.Ratio:R}, data {effect.ClampVital.DataBits03}/{effect.ClampVital.DataBits04}/{effect.ClampVital.DataBits05}/{effect.ClampVital.DataBits06}/{effect.ClampVital.DataBits07}/{effect.ClampVital.DataBits08}/{effect.ClampVital.DataBits09}";

            if (effect.ShieldOverload != null)
                return $"shield overload data {effect.ShieldOverload.DataBits00}/{effect.ShieldOverload.DataBits01}/{effect.ShieldOverload.DataBits02}/{effect.ShieldOverload.DataBits03}/{effect.ShieldOverload.DataBits04}/{effect.ShieldOverload.DataBits05}/{effect.ShieldOverload.DataBits06}/{effect.ShieldOverload.DataBits07}/{effect.ShieldOverload.DataBits08}/{effect.ShieldOverload.DataBits09}";

            if (effect.GrantXp != null)
                return $"grant xp amount {effect.GrantXp.Amount}, data {effect.GrantXp.DataBits01}/{effect.GrantXp.DataBits02}/{effect.GrantXp.DataBits03}/{effect.GrantXp.DataBits04}/{effect.GrantXp.DataBits05}";

            if (effect.PathXpModify != null)
                return $"path xp modify amount {effect.PathXpModify.Amount}, mode {effect.PathXpModify.Mode}, data {effect.PathXpModify.DataBits02}/{effect.PathXpModify.DataBits03}/{effect.PathXpModify.DataBits04}/{effect.PathXpModify.DataBits05}";

            if (effect.GrantLevelScaledXp != null)
                return $"grant level-scaled xp percent {effect.GrantLevelScaledXp.PercentOfLevel:R}, max level {effect.GrantLevelScaledXp.MaxLevel}, mode {effect.GrantLevelScaledXp.Mode}, data {effect.GrantLevelScaledXp.DataBits03}/{effect.GrantLevelScaledXp.DataBits04}/{effect.GrantLevelScaledXp.DataBits05}";

            if (effect.GiveAugmentPowerToPlayer != null)
                return $"give augment power amount {effect.GiveAugmentPowerToPlayer.Amount}, data {effect.GiveAugmentPowerToPlayer.DataBits01}/{effect.GiveAugmentPowerToPlayer.DataBits02}/{effect.GiveAugmentPowerToPlayer.DataBits03}/{effect.GiveAugmentPowerToPlayer.DataBits04}/{effect.GiveAugmentPowerToPlayer.DataBits05}";

            if (effect.ForceFacing != null)
                return $"force facing angle offset {effect.ForceFacing.AngleDegrees:R} degrees, uses offset {effect.ForceFacing.UsesAngleOffset}, turn {effect.ForceFacing.TurnDurationMs}ms, data {effect.ForceFacing.DataBits01}/{effect.ForceFacing.DataBits02}/{effect.ForceFacing.DataBits04}/{effect.ForceFacing.DataBits05}/{effect.ForceFacing.DataBits06}/{effect.ForceFacing.DataBits07}/{effect.ForceFacing.DataBits08}/{effect.ForceFacing.DataBits09}";

            if (effect.ForcedMove != null)
                return $"forced move type {effect.ForcedMove.MovementType}, duration {effect.ForcedMove.DurationTime}ms, flags {effect.ForcedMove.Flags}, floats {effect.ForcedMove.DataFloat01:R}/{effect.ForcedMove.DataFloat02:R}/{effect.ForcedMove.DataFloat06:R}/{effect.ForcedMove.DataFloat07:R}/{effect.ForcedMove.DataFloat08:R}";

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
                return $"cc state break mask {effect.CCStateBreak.StateMask} [{FormatCCStateMask(effect.CCStateBreak.StateMask)}], data {effect.CCStateBreak.DataBits01}/{effect.CCStateBreak.DataBits02}/{effect.CCStateBreak.DataBits03}/{effect.CCStateBreak.DataBits04}/{effect.CCStateBreak.DataBits05}";

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

        private static string DescribeSpell4(uint spell4Id)
        {
            if (spell4Id == 0u)
                return "0";

            Spell4Entry entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (entry == null)
                return $"{spell4Id} (missing)";

            return $"{entry.Id} base {entry.Spell4BaseIdBaseSpell} tier {entry.TierIndex} \"{entry.Description}\"";
        }

        private static string DescribeCreature(uint creatureId)
        {
            if (creatureId == 0u)
                return "0";

            Creature2Entry entry = GameTableManager.Instance.Creature2.GetEntry(creatureId);
            if (entry == null)
                return $"{creatureId} (missing)";

            return creatureId.ToString();
        }
    }
}
