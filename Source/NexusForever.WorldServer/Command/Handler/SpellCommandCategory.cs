using NexusForever.Game.Abstract.Entity;
using System.Linq;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.RBAC;
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

            context.SendMessage($"Spell base {spell4BaseId}, tier {tier.Value}, spell4 {spellInfo.Entry.Id}: class {spellInfo.BaseInfo.SpellClass}, effects {spellInfo.Effects.Count}, telegraphs {spellInfo.Telegraphs.Count}.");
            context.SendMessage($"TargetMechanics {spellInfo.BaseInfo.TargetMechanics?.Id ?? 0}, ValidTargets {spellInfo.BaseInfo.ValidTargets?.Id ?? 0}, AoeConstraints {spellInfo.AoeTargetConstraints?.Id ?? 0}, StackGroup {spellInfo.StackGroup?.Id ?? 0}.");

            foreach (SpellEffectInterpretation effect in spellInfo.Effects.Select(SpellEffectInterpreter.Interpret))
            {
                context.SendMessage($"Effect {effect.Entry.Id} order {effect.Entry.OrderIndex}: {effect.Entry.EffectType}, targetFlags {effect.Entry.TargetFlags}, damageType {effect.Entry.DamageType}, timing delay/tick/duration {effect.Timing.DelayTime}/{effect.Timing.TickTime}/{effect.Timing.DurationTime}.");
                context.SendMessage($"  Semantics: {DescribeEffectSemantics(effect)}");
                context.SendMessage($"  DataBits: {effect.FormatDataBits()}");
                context.SendMessage($"  Parameters: {effect.FormatParameters()}");
            }
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

        private static string DescribeEffectSemantics(SpellEffectInterpretation effect)
        {
            if (effect.Damage != null)
                return $"damage-family multiplier {effect.Damage.TypeMultiplier:R}, base value {effect.Damage.TypeBaseValue:R}";

            if (effect.Proxy != null)
                return $"proxy spell4 {effect.Proxy.Spell4Id}";

            if (effect.Teleport != null)
                return $"world location {effect.Teleport.WorldLocation2Id}";

            if (effect.UnitPropertyModifier != null)
                return $"property {effect.UnitPropertyModifier.Property}, priority {effect.UnitPropertyModifier.Priority}, mod hint {effect.UnitPropertyModifier.ModifierTypeHint?.ToString() ?? "unknown"}, percentage {effect.UnitPropertyModifier.PercentageValue:R}, flat {effect.UnitPropertyModifier.FlatValue:R}, level scale {effect.UnitPropertyModifier.LevelScaleValue:R}";

            return "unknown";
        }
    }
}
