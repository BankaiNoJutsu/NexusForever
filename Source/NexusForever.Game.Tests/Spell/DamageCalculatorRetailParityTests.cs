using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Spell.Effect;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;

namespace NexusForever.Game.Tests.Spell;

public class DamageCalculatorRetailParityTests
{
    [Theory]
    [InlineData(400f, null, 100f)]
    [InlineData(400f, 0.5f, 200f)]
    public void ApplyPowerCoefficient_UsesRetailFallbackWhenFormulaIsMissing(float rating, float? coefficient, float expected)
    {
        float damage = DamageCalculator.ApplyPowerCoefficient(rating, coefficient);

        Assert.Equal(expected, damage);
    }

    [Theory]
    [InlineData(0.1509f, 4.77f, 57u)]
    [InlineData(0.13f, 4.13f, 49u)]
    public void CalculateBaseDamage_WithArtillerybotFormulaShape_UsesCasterAssaultPowerAndLevel(
        float assaultPowerCoefficient,
        float levelCoefficient,
        uint expectedDamage)
    {
        var calculator = new DamageCalculator(
            NullLogger<DamageCalculator>.Instance,
            CreateGameTableManager(CreatePowerCoefficientFormula()));
        IUnitEntity attacker = CreateUnit(
            level: 4u,
            new Dictionary<Property, float>
            {
                [Property.AssaultRating] = 1000f,
                [Property.DamageDealtMultiplierPhysical] = 1f
            });
        IUnitEntity victim = CreateUnit(level: 4u, new Dictionary<Property, float>());
        var effectEntry = new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.Damage,
            DamageType = DamageType.Physical,
            DataBits00 = BitConverter.SingleToUInt32Bits(1f),
            ParameterType =
            [
                SpellEffectParameterType.AssaultPower,
                SpellEffectParameterType.PerLevel,
                SpellEffectParameterType.None,
                SpellEffectParameterType.None
            ],
            ParameterValue =
            [
                assaultPowerCoefficient,
                levelCoefficient,
                0f,
                0f
            ]
        };

        uint damage = InvokeBaseDamage(calculator, attacker, victim, effectEntry);

        Assert.Equal(expectedDamage, damage);
    }

    [Theory]
    [InlineData(DamageType.Physical, 0.25f, 0.10f, 0.05f, 75u)]
    [InlineData(DamageType.Tech, 0.05f, 0.30f, 0.10f, 70u)]
    [InlineData(DamageType.Magic, 0.05f, 0.10f, 0.40f, 60u)]
    public void ApplyArmorMitigation_UsesDamageTypeSpecificMitigationOffset(
        DamageType damageType,
        float physicalOffset,
        float techOffset,
        float magicOffset,
        uint expectedDamage)
    {
        uint damage = DamageCalculator.ApplyArmorMitigation(
            100u,
            damageType,
            victimLevel: 50u,
            victimArmor: 0f,
            physicalOffset,
            techOffset,
            magicOffset,
            CreateArmorFormula(maximumMitigationPercent: 100u));

        Assert.Equal(expectedDamage, damage);
    }

    [Fact]
    public void ApplyArmorMitigation_ClampsOffsetToFormulaMaximum()
    {
        uint damage = DamageCalculator.ApplyArmorMitigation(
            100u,
            DamageType.Physical,
            victimLevel: 50u,
            victimArmor: 0f,
            physicalOffset: 0.90f,
            techOffset: 0f,
            magicOffset: 0f,
            CreateArmorFormula(maximumMitigationPercent: 50u));

        Assert.Equal(50u, damage);
    }

    [Fact]
    public void ApplyArmorMitigation_IgnoresNonDamageTypedOffsets()
    {
        uint damage = DamageCalculator.ApplyArmorMitigation(
            100u,
            DamageType.Heal,
            victimLevel: 50u,
            victimArmor: 0f,
            physicalOffset: 0.90f,
            techOffset: 0.80f,
            magicOffset: 0.70f,
            CreateArmorFormula(maximumMitigationPercent: 100u));

        Assert.Equal(100u, damage);
    }

    // Damage DataBits04 is the fraction of armor the row ignores. Tooltip witnesses: Spellslinger True Shot Tap 1 (Spell4 36052, 0.35) "pierce 35% armor";
    // True Shot Surge Tap 2 (36054, 0.5) "Armor Pierce increased to 50%"; Warrior Breaching Strikes 2nd Hit (42371, 0.2) "Pierces 20% Armor".
    // With this formula row 1,000 armor at level 50 mitigates 40%.
    [Theory]
    [InlineData(0f, 60u)]
    [InlineData(0.35f, 74u)]
    [InlineData(0.5f, 80u)]
    [InlineData(1f, 100u)]
    [InlineData(4f, 100u)]
    [InlineData(float.NaN, 60u)]
    public void ApplyArmorMitigation_IgnoresThePiercedFractionOfArmor(float armorPierce, uint expectedDamage)
    {
        var formula = new GameFormulaEntry { Id = 1234u, Dataint01 = 75u, Datafloat0 = 2f, Datafloat01 = 1f };

        uint damage = DamageCalculator.ApplyArmorMitigation(
            100u,
            DamageType.Physical,
            victimLevel: 50u,
            victimArmor: 1000f,
            physicalOffset: 0f,
            techOffset: 0f,
            magicOffset: 0f,
            formula,
            armorPierce);

        Assert.Equal(expectedDamage, damage);
    }

    [Fact]
    public void ArmorPierceLeavesTheFlatMitigationOffsetAlone()
    {
        var formula = new GameFormulaEntry { Id = 1234u, Dataint01 = 75u, Datafloat0 = 2f, Datafloat01 = 1f };

        uint damage = DamageCalculator.ApplyArmorMitigation(100u, DamageType.Physical, 50u, 1000f, 0.10f, 0f, 0f, formula, armorPierce: 1f);

        Assert.Equal(90u, damage);
    }

    // Damage DataBits05 is the fraction shields cannot absorb. Tooltip witness: Stalker Cutthroat (Spell4 70965, row 181556, 1.0) "This damage ignores Shields".
    // "Generic Instakill" (29207) carries 0.99 in both the armor and the shield word.
    [Theory]
    [InlineData(0f, 60u)]
    [InlineData(0.5f, 30u)]
    [InlineData(0.99f, 0u)]
    [InlineData(1f, 0u)]
    [InlineData(float.NaN, 60u)]
    public void CalculateShieldAmount_LeavesThePiercedShareToHealth(float shieldPierce, uint expectedShielded)
    {
        Assert.Equal(expectedShielded, DamageCalculator.CalculateShieldAmount(100u, 1000u, 0.6f, shieldPierce));
    }

    [Fact]
    public void OnlyDamageRowsReadDataBits05AsShieldPierce()
    {
        uint one = BitConverter.SingleToUInt32Bits(1f);

        Assert.Equal(1f, SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 181556u, EffectType = SpellEffectType.Damage, DataBits05 = one }).Damage.ShieldPierce);
        Assert.Equal(0f, SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 1u, EffectType = SpellEffectType.Heal, DataBits05 = one }).Damage.ShieldPierce);
    }

    [Fact]
    public void OnlyDamageRowsReadDataBits04AsArmorPierce()
    {
        uint half = BitConverter.SingleToUInt32Bits(0.5f);

        Assert.Equal(0.5f, SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 80119u, EffectType = SpellEffectType.Damage, DataBits04 = half }).Damage.ArmorPierce);
        Assert.Equal(0f, SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 1u, EffectType = SpellEffectType.Heal, DataBits04 = half }).Damage.ArmorPierce);
        Assert.Equal(0f, SpellEffectInterpreter.Interpret(new Spell4EffectsEntry { Id = 2u, EffectType = SpellEffectType.DistributedDamage, DataBits04 = half }).Damage.ArmorPierce);
    }

    [Fact]
    public void ApplyArmorMitigation_WithMissingFormulaKeepsDamage()
    {
        uint damage = DamageCalculator.ApplyArmorMitigation(
            100u,
            DamageType.Physical,
            victimLevel: 50u,
            victimArmor: 1000f,
            physicalOffset: 0.25f,
            techOffset: 0f,
            magicOffset: 0f,
            armorFormulaEntry: null);

        Assert.Equal(100u, damage);
    }

    [Theory]
    [InlineData(9u, 10u, 1.20f, 9u)]
    [InlineData(100u, 40u, 0.50f, 40u)]
    [InlineData(100u, 80u, 0.50f, 50u)]
    [InlineData(100u, 80u, 0.00f, 0u)]
    [InlineData(100u, 80u, -1.00f, 0u)]
    public void CalculateShieldAmount_ClampsAbsorbToRemainingDamage(
        uint damage,
        uint shield,
        float shieldMitigationMax,
        uint expectedShieldAmount)
    {
        uint shieldAmount = DamageCalculator.CalculateShieldAmount(damage, shield, shieldMitigationMax);

        Assert.Equal(expectedShieldAmount, shieldAmount);
    }

    [Fact]
    public void CriticalSeverityMultiplier_UsesBaseMultiplierWhenRatingIsZero()
    {
        var calculator = new DamageCalculator(
            NullLogger<DamageCalculator>.Instance,
            CreateGameTableManager(CreateCritSeverityFormula()));
        IUnitEntity attacker = CreateUnit(
            level: 4u,
            new Dictionary<Property, float>
            {
                [Property.CriticalHitSeverityMultiplier] = 1.5f,
                [Property.RatingCritSeverityIncrease]    = 0f
            });

        float multiplier = InvokeRatingPercentMod(calculator, Property.RatingCritSeverityIncrease, attacker);

        Assert.Equal(1.5f, multiplier, precision: 3);
    }

    [Fact]
    public void DeflectChance_SubtractsAttackerStrikethroughFromVictimAvoid()
    {
        var calculator = new DamageCalculator(
            NullLogger<DamageCalculator>.Instance,
            CreateGameTableManager(CreateAvoidFormula(), CreateStrikethroughFormula()));
        IUnitEntity attacker = CreateUnit(
            level: 50u,
            new Dictionary<Property, float>
            {
                [Property.BaseAvoidReduceChance] = 0.10f
            });
        IUnitEntity victim = CreateUnit(
            level: 50u,
            new Dictionary<Property, float>
            {
                [Property.BaseAvoidChance] = 0.25f
            });

        float chance = calculator.CalculateEffectiveDeflectChance(attacker, victim);

        Assert.Equal(0.15f, chance, precision: 3);
    }

    [Fact]
    public void DeflectChance_ClampsWhenAttackerStrikethroughExceedsVictimAvoid()
    {
        var calculator = new DamageCalculator(
            NullLogger<DamageCalculator>.Instance,
            CreateGameTableManager(CreateAvoidFormula(), CreateStrikethroughFormula()));
        IUnitEntity attacker = CreateUnit(
            level: 50u,
            new Dictionary<Property, float>
            {
                [Property.BaseAvoidReduceChance] = 0.25f
            });
        IUnitEntity victim = CreateUnit(
            level: 50u,
            new Dictionary<Property, float>
            {
                [Property.BaseAvoidChance] = 0.10f
            });

        float chance = calculator.CalculateEffectiveDeflectChance(attacker, victim);

        Assert.Equal(0f, chance);
    }

    [Fact]
    public void CalculateDamage_WhenDeflectChanceIsCertain_DropsEffectAndAddsDeflectLog()
    {
        var calculator = new DamageCalculator(
            NullLogger<DamageCalculator>.Instance,
            CreateGameTableManager(CreateAvoidFormula(), CreateStrikethroughFormula()));
        IUnitEntity attacker = CreateUnit(
            level: 50u,
            new Dictionary<Property, float>(),
            guid: 0x11121314u);
        IUnitEntity victim = CreateUnit(
            level: 50u,
            new Dictionary<Property, float>
            {
                [Property.BaseAvoidChance] = 1f
            },
            guid: 0x21222324u);
        ISpell spell = CreateSpell(0x12345u);
        var effectInfo = new SpellTargetInfo.SpellTargetEffectInfo(0x34353637u, new Spell4EffectsEntry
        {
            Id         = 0x45464748u,
            EffectType = SpellEffectType.Damage,
            DamageType = DamageType.Physical
        });

        calculator.CalculateDamage(attacker, victim, spell, effectInfo);

        Assert.True(effectInfo.DropEffect);
        Assert.Null(effectInfo.Damage);
        CombatLogDeflect deflect = Assert.IsType<CombatLogDeflect>(Assert.Single(effectInfo.CombatLogs));
        Assert.False(deflect.BMultiHit);
        Assert.Equal(0x11121314u, deflect.CastData.CasterId);
        Assert.Equal(0x21222324u, deflect.CastData.TargetId);
        Assert.Equal(0x12345u, deflect.CastData.SpellId);
        Assert.Equal(CombatResult.Hit, deflect.CastData.CombatResult);
    }

    private static GameFormulaEntry CreateArmorFormula(uint maximumMitigationPercent)
    {
        return new GameFormulaEntry
        {
            Id        = 1234u,
            Dataint01 = maximumMitigationPercent
        };
    }

    private static GameFormulaEntry CreateCritSeverityFormula()
    {
        return new GameFormulaEntry
        {
            Id          = 1232u,
            Dataint01   = 300u,
            Datafloat0  = 50f,
            Datafloat01 = 0.01f
        };
    }

    private static GameFormulaEntry CreatePowerCoefficientFormula()
    {
        return new GameFormulaEntry
        {
            Id          = 1266u,
            Datafloat0  = 0.25f,
            Datafloat01 = 0.25f
        };
    }

    private static GameFormulaEntry CreateAvoidFormula()
    {
        return CreateBaseOnlyPercentFormula(1235u);
    }

    private static GameFormulaEntry CreateStrikethroughFormula()
    {
        return CreateBaseOnlyPercentFormula(1230u);
    }

    private static GameFormulaEntry CreateBaseOnlyPercentFormula(uint id)
    {
        return new GameFormulaEntry
        {
            Id        = id,
            Dataint01 = 100u
        };
    }

    private static ISpell CreateSpell(uint spell4Id)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id = spell4Id
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Parameters), new SpellParameters
        {
            SpellInfo = spellInfo
        });
        return spell;
    }

    private static IGameTableManager CreateGameTableManager(params GameFormulaEntry[] formulas)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(formulas));
        return gameTableManager;
    }

    private static IUnitEntity CreateUnit(uint level, IReadOnlyDictionary<Property, float> propertyValues, uint guid = 0u)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> proxy);
        proxy.SetProperty(nameof(IGridEntity.Guid), guid);
        proxy.SetProperty("Level", level);
        proxy.SetMethodHandler(nameof(IUnitEntity.GetPropertyValue), args =>
        {
            Property property = (Property)args[0];
            return propertyValues.TryGetValue(property, out float value) ? value : 0f;
        });
        proxy.SetMethodHandler(nameof(IUnitEntity.GetProperty), args =>
        {
            Property property = (Property)args[0];
            return new PropertyValue(property, propertyValues.TryGetValue(property, out float value) ? value : 0f);
        });

        return unit;
    }

    private static uint InvokeBaseDamage(DamageCalculator calculator, IUnitEntity caster, IUnitEntity target, Spell4EffectsEntry effectEntry)
    {
        MethodInfo method = typeof(DamageCalculator)
            .GetMethod("CalculateBaseDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        return (uint)method.Invoke(calculator, [caster, target, SpellEffectInterpreter.Interpret(effectEntry)]);
    }

    private static float InvokeRatingPercentMod(DamageCalculator calculator, Property property, IUnitEntity entity)
    {
        MethodInfo method = typeof(DamageCalculator)
            .GetMethod("GetRatingPercentMod", BindingFlags.Instance | BindingFlags.NonPublic);

        return (float)method.Invoke(calculator, [property, entity]);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
