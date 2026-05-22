using NexusForever.Game.Spell.Effect;

namespace NexusForever.Game.Tests.Spell;

public class DamageShieldFixtureWitnessTests
{
    [Fact]
    public void SuperchargedShieldDamage82315_DecodesUnitCoefficientDamageSemantics()
    {
        var damage = new SpellEffectDamageSemantics(
            BitConverter.UInt32BitsToSingle(1065353216u),
            0f);

        Assert.Equal(1.0f, damage.TypeMultiplier);
        Assert.Equal(0f, damage.TypeBaseValue);
    }
}
