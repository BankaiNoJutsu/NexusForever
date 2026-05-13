using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.Effect
{
    public enum SpellEffectParameterRole
    {
        None,
        CasterPrimaryStatCoefficient,
        CasterPowerCoefficient,
        TargetVitalCoefficient,
        CasterVitalCoefficient,
        CasterLevelCoefficient,
        ItemBudget,
        Unknown
    }

    public sealed record SpellEffectDataBit(byte Index, uint RawValue, float FloatValue);

    public sealed record SpellEffectParameter(int Index, SpellEffectParameterType Type, float Value, SpellEffectParameterRole Role);

    public sealed record SpellEffectTiming(uint DelayTime, uint TickTime, uint DurationTime);

    public sealed record SpellEffectDamageSemantics(float TypeMultiplier, float TypeBaseValue);

    public sealed record SpellEffectProxySemantics(uint Spell4Id);

    public sealed record SpellEffectTeleportSemantics(uint WorldLocation2Id);

    public sealed record SpellEffectUnitPropertyModifierSemantics(
        Property Property,
        uint Priority,
        ModType? ModifierTypeHint,
        float PercentageValue,
        float FlatValue,
        float LevelScaleValue);

    public sealed class SpellEffectInterpretation
    {
        public Spell4EffectsEntry Entry { get; }
        public SpellEffectTiming Timing { get; }
        public IReadOnlyList<SpellEffectDataBit> DataBits { get; }
        public IReadOnlyList<SpellEffectParameter> Parameters { get; }

        public SpellEffectDamageSemantics Damage { get; internal set; }
        public SpellEffectProxySemantics Proxy { get; internal set; }
        public SpellEffectTeleportSemantics Teleport { get; internal set; }
        public SpellEffectUnitPropertyModifierSemantics UnitPropertyModifier { get; internal set; }

        public bool HasKnownFamilySemantics => Damage != null
            || Proxy != null
            || Teleport != null
            || UnitPropertyModifier != null;

        internal SpellEffectInterpretation(
            Spell4EffectsEntry entry,
            SpellEffectTiming timing,
            IReadOnlyList<SpellEffectDataBit> dataBits,
            IReadOnlyList<SpellEffectParameter> parameters)
        {
            Entry      = entry;
            Timing     = timing;
            DataBits   = dataBits;
            Parameters = parameters;
        }

        public string FormatDataBits()
        {
            return string.Join(", ", DataBits.Select(d => $"DataBits{d.Index:00}={d.RawValue} (0x{d.RawValue:X8}, float={d.FloatValue:R})"));
        }

        public string FormatParameters()
        {
            return string.Join(", ", Parameters.Select(p => $"Parameter{p.Index}={p.Type}:{p.Value:R}:{p.Role}"));
        }
    }

    public static class SpellEffectInterpreter
    {
        public static SpellEffectInterpretation Interpret(ISpellTargetEffectInfo info)
        {
            if (info is SpellTargetInfo.SpellTargetEffectInfo targetEffectInfo)
                return targetEffectInfo.Interpretation;

            return Interpret(info.Entry);
        }

        public static SpellEffectInterpretation Interpret(Spell4EffectsEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var interpretation = new SpellEffectInterpretation(
                entry,
                new SpellEffectTiming(entry.DelayTime, entry.TickTime, entry.DurationTime),
                CreateDataBits(entry),
                CreateParameters(entry));

            switch (entry.EffectType)
            {
                case SpellEffectType.Transference:
                    interpretation.Damage = new SpellEffectDamageSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        entry.DataBits03);
                    break;
                case SpellEffectType.Damage:
                case SpellEffectType.Heal:
                case SpellEffectType.DistanceDependentDamage:
                case SpellEffectType.DistributedDamage:
                case SpellEffectType.HealShields:
                case SpellEffectType.DamageShields:
                    interpretation.Damage = new SpellEffectDamageSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits01);
                    break;
                case SpellEffectType.Proxy:
                case SpellEffectType.ProxyLinearAE:
                case SpellEffectType.ProxyChannel:
                case SpellEffectType.ProxyChannelVariableTime:
                case SpellEffectType.ProxyRandomExclusive:
                    interpretation.Proxy = new SpellEffectProxySemantics(entry.DataBits00);
                    break;
                case SpellEffectType.Teleport:
                    interpretation.Teleport = new SpellEffectTeleportSemantics(entry.DataBits00);
                    break;
                case SpellEffectType.UnitPropertyModifier:
                    interpretation.UnitPropertyModifier = CreateUnitPropertyModifier(entry);
                    break;
            }

            return interpretation;
        }

        private static IReadOnlyList<SpellEffectDataBit> CreateDataBits(Spell4EffectsEntry entry)
        {
            uint[] raw =
            [
                entry.DataBits00,
                entry.DataBits01,
                entry.DataBits02,
                entry.DataBits03,
                entry.DataBits04,
                entry.DataBits05,
                entry.DataBits06,
                entry.DataBits07,
                entry.DataBits08,
                entry.DataBits09
            ];

            return raw
                .Select((value, index) => new SpellEffectDataBit((byte)index, value, BitConverter.UInt32BitsToSingle(value)))
                .ToArray();
        }

        private static IReadOnlyList<SpellEffectParameter> CreateParameters(Spell4EffectsEntry entry)
        {
            var parameters = new SpellEffectParameter[4];
            for (int i = 0; i < parameters.Length; i++)
            {
                SpellEffectParameterType type = entry.ParameterType?.Length > i
                    ? entry.ParameterType[i]
                    : SpellEffectParameterType.None;
                float value = entry.ParameterValue?.Length > i
                    ? entry.ParameterValue[i]
                    : 0f;

                parameters[i] = new SpellEffectParameter(i, type, value, GetParameterRole(type));
            }

            return parameters;
        }

        private static SpellEffectParameterRole GetParameterRole(SpellEffectParameterType type)
        {
            switch (type)
            {
                case SpellEffectParameterType.None:
                    return SpellEffectParameterRole.None;
                case SpellEffectParameterType.Brutality:
                case SpellEffectParameterType.Finesse:
                case SpellEffectParameterType.Tech:
                case SpellEffectParameterType.Moxie:
                case SpellEffectParameterType.Insight:
                case SpellEffectParameterType.Grit:
                    return SpellEffectParameterRole.CasterPrimaryStatCoefficient;
                case SpellEffectParameterType.AssaultPower:
                case SpellEffectParameterType.SupportPower:
                    return SpellEffectParameterRole.CasterPowerCoefficient;
                case SpellEffectParameterType.TargetMaxHealth:
                case SpellEffectParameterType.TargetShieldCapacity:
                case SpellEffectParameterType.TargetMaxShieldCapacity:
                case SpellEffectParameterType.TargetCurrentHealth:
                case SpellEffectParameterType.TargetShieldCapacity2:
                case SpellEffectParameterType.TargetMissingHealth:
                case SpellEffectParameterType.TargetMissingShields:
                    return SpellEffectParameterRole.TargetVitalCoefficient;
                case SpellEffectParameterType.CasterMaxHealth:
                case SpellEffectParameterType.CasterShieldCapacity:
                case SpellEffectParameterType.CasterMaxShieldCapacity:
                case SpellEffectParameterType.CasterCurrentHealth:
                case SpellEffectParameterType.CasterShieldCapacity2:
                case SpellEffectParameterType.CasterMissingHealth:
                case SpellEffectParameterType.CasterMissingShields:
                    return SpellEffectParameterRole.CasterVitalCoefficient;
                case SpellEffectParameterType.PerLevel:
                    return SpellEffectParameterRole.CasterLevelCoefficient;
                case SpellEffectParameterType.ItemBudget:
                    return SpellEffectParameterRole.ItemBudget;
                default:
                    return SpellEffectParameterRole.Unknown;
            }
        }

        private static SpellEffectUnitPropertyModifierSemantics CreateUnitPropertyModifier(Spell4EffectsEntry entry)
        {
            ModType? modifierTypeHint = Enum.IsDefined(typeof(ModType), (int)entry.DataBits01)
                ? (ModType)entry.DataBits01
                : null;

            return new SpellEffectUnitPropertyModifierSemantics(
                (Property)entry.DataBits00,
                entry.DataBits01,
                modifierTypeHint,
                BitConverter.UInt32BitsToSingle(entry.DataBits02),
                BitConverter.UInt32BitsToSingle(entry.DataBits03),
                BitConverter.UInt32BitsToSingle(entry.DataBits04));
        }
    }
}
