using NexusForever.Game.Combat;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

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

    private static GameFormulaEntry CreateArmorFormula(uint maximumMitigationPercent)
    {
        return new GameFormulaEntry
        {
            Id        = 1234u,
            Dataint01 = maximumMitigationPercent
        };
    }
}
