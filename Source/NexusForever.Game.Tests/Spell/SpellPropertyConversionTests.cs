using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Tests.Spell;

public class SpellPropertyConversionTests
{
    [Fact]
    public void Interpret_MountainRow_DecodesLiveBaseHealthToArmorConversion()
    {
        SpellEffectInterpretation interpretation = SpellEffectInterpreter.Interpret(new Spell4EffectsEntry
        {
            EffectType = SpellEffectType.UnitPropertyConversion,
            DataBits00 = (uint)Property.BaseHealth,
            DataBits01 = (uint)Property.Armor,
            DataBits02 = BitConverter.SingleToUInt32Bits(0.01f)
        });

        Assert.Equal(Property.BaseHealth, interpretation.UnitPropertyConversion.SourceProperty);
        Assert.Equal(Property.Armor, interpretation.UnitPropertyConversion.TargetProperty);
        Assert.Equal(0.01f, interpretation.UnitPropertyConversion.Multiplier);
    }

    [Fact]
    public void PropertyConversion_RecalculatesTargetWhenSourceChanges_AndRestoresOnRemoval()
    {
        var unit = new TestUnitEntity();
        unit.SetBaseProperty(Property.BaseHealth, 1000f);
        unit.SetBaseProperty(Property.Armor, 100f);

        Assert.True(unit.TryAddSpellPropertyConversion(
            new SpellPropertyConversion(Property.BaseHealth, Property.Armor, 0.01f),
            effectId: 215867u,
            spell4Id: 82056u,
            spell4EffectId: 215867u,
            castingId: 1u,
            out string skippedReason));
        Assert.Null(skippedReason);
        Assert.Equal(110f, unit.GetPropertyValue(Property.Armor), precision: 3);

        unit.SetBaseProperty(Property.BaseHealth, 1500f);

        Assert.Equal(115f, unit.GetPropertyValue(Property.Armor), precision: 3);
        Assert.True(unit.RemoveSpellProperty(Property.Armor, 215867u));
        Assert.Equal(100f, unit.GetPropertyValue(Property.Armor), precision: 3);
    }

    [Fact]
    public void PropertyConversion_PropagatesAcyclicChains_AndRejectsFeedbackLoop()
    {
        var unit = new TestUnitEntity();
        unit.SetBaseProperty(Property.BaseHealth, 1000f);

        Assert.True(unit.TryAddSpellPropertyConversion(
            new SpellPropertyConversion(Property.BaseHealth, Property.Armor, 0.01f),
            1u, 82056u, 1u, 1u, out _));
        Assert.True(unit.TryAddSpellPropertyConversion(
            new SpellPropertyConversion(Property.Armor, Property.RatingCritSeverityIncrease, 0.5f),
            2u, 87367u, 2u, 2u, out _));

        Assert.Equal(10f, unit.GetPropertyValue(Property.Armor), precision: 3);
        Assert.Equal(5f, unit.GetPropertyValue(Property.RatingCritSeverityIncrease), precision: 3);

        Assert.False(unit.TryAddSpellPropertyConversion(
            new SpellPropertyConversion(Property.RatingCritSeverityIncrease, Property.BaseHealth, 0.1f),
            3u, 86841u, 3u, 3u, out string skippedReason));
        Assert.Equal("conversion-cycle", skippedReason);

        Assert.False(unit.TryAddSpellPropertyConversion(
            new SpellPropertyConversion(unchecked((Property)(-1)), Property.BaseHealth, 0.1f),
            4u, 86841u, 4u, 4u, out skippedReason));
        Assert.Equal("unknown-property", skippedReason);

        Assert.False(unit.TryAddSpellPropertyConversion(
            new SpellPropertyConversion(Property.Armor, Property.BaseHealth, 0f),
            5u, 86841u, 5u, 5u, out skippedReason));
        Assert.Equal("invalid-multiplier", skippedReason);

        unit.SetBaseProperty(Property.BaseHealth, 2000f);
        Assert.Equal(20f, unit.GetPropertyValue(Property.Armor), precision: 3);
        Assert.Equal(10f, unit.GetPropertyValue(Property.RatingCritSeverityIncrease), precision: 3);
    }

    private sealed class TestUnitEntity : UnitEntity
    {
        public override EntityType Type => EntityType.Simple;

        public TestUnitEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new WorldUnitEntityModel();
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }
    }
}
