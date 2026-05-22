using NexusForever.Game.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class AbsorptionSemanticsTests
{
    [Theory]
    [InlineData(1u, DamageType.Physical, true)]
    [InlineData(1u, DamageType.Tech, false)]
    [InlineData(1u, DamageType.Magic, false)]
    [InlineData(2u, DamageType.Physical, false)]
    [InlineData(2u, DamageType.Tech, true)]
    [InlineData(2u, DamageType.Magic, false)]
    [InlineData(4u, DamageType.Physical, false)]
    [InlineData(4u, DamageType.Tech, false)]
    [InlineData(4u, DamageType.Magic, true)]
    [InlineData(6u, DamageType.Physical, false)]
    [InlineData(6u, DamageType.Tech, true)]
    [InlineData(6u, DamageType.Magic, true)]
    [InlineData(7u, DamageType.Physical, true)]
    [InlineData(7u, DamageType.Tech, true)]
    [InlineData(7u, DamageType.Magic, true)]
    public void IsAbsorptionApplicable_UsesMappedDamageSchoolMask(uint absorptionType, DamageType damageType, bool expected)
    {
        Assert.Equal(expected, UnitEntity.IsAbsorptionApplicable(absorptionType, damageType));
    }

    [Theory]
    [InlineData(DamageType.Heal)]
    [InlineData(DamageType.HealShields)]
    [InlineData(DamageType.Fall)]
    [InlineData(DamageType.Suffocate)]
    public void IsAbsorptionApplicable_DoesNotConsumeForNonSchoolDamageTypes(DamageType damageType)
    {
        Assert.False(UnitEntity.IsAbsorptionApplicable(7u, damageType));
    }
}
