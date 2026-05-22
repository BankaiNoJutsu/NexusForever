using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Tests.Spell;

public class ClampVitalSemanticsTests
{
    [Theory]
    [InlineData(0u, 0u, 0.90f)]
    [InlineData(1u, 0u, 0.05f)]
    [InlineData(0u, 2u, 1.00f)]
    [InlineData(1u, 2u, 1.00f)]
    public void ResolveClampVital_TreatsObservedModesAsHealthCeiling(uint mode, uint vitalMode, float ratio)
    {
        var clampVital = new SpellEffectClampVitalSemantics(
            mode,
            vitalMode,
            ratio,
            DataBits03: 0u,
            DataBits04: 0u,
            DataBits05: 0u,
            DataBits06: 0u,
            DataBits07: 0u,
            DataBits08: 0u,
            DataBits09: 0u);

        Assert.Equal(Vital.Health, SpellHandler.ResolveClampVital(clampVital));
    }
}
