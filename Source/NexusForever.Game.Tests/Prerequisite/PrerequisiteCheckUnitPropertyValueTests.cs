using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_UNIT_PROPERTY_VALUE_20261107.md): prerequisite type 55 (jump-table entry 55 -> 1404a243f) is NOT a currency amount. It compares the unit's property array entry ObjectId
// (197 entries, 0 above index 0xC4) as a float with Value as a float: property <comparison> Value. Retail: 4 slots with ObjectId 9 (ResourceMax0), 41 (ShieldCapacityMax) and 100 (MoveSpeedMultiplier) - property ids, not currency types.
// Status: Implemented (disassembly); supersedes the earlier currency-amount reading. Type 240 keeps the currency check (its own native slot is not read).
public class PrerequisiteCheckUnitPropertyValueTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 1000f, 1000u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 1000f, 1000u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 999.5f, 1000u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1f, 1u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 1f, 1u, false)]
    [InlineData((PrerequisiteComparison)0, 1f, 1u, false)]
    public void ComparesThePropertyWithValueAsFloats(PrerequisiteComparison comparison, float property, uint value, bool expected)
    {
        IUnitEntity unit = Unit(new Dictionary<Property, float> { [Property.ResourceMax0] = property });

        Assert.Equal(expected, new PrerequisiteCheckUnitPropertyValue().MeetsUnit(unit, comparison, value, (uint)Property.ResourceMax0, new PrerequisiteParameters()));
    }

    [Fact]
    public void ReadsTheRetailPropertyIds()
    {
        IUnitEntity unit = Unit(new Dictionary<Property, float> { [Property.ShieldCapacityMax] = 500f, [Property.MoveSpeedMultiplier] = 1.25f });
        var check = new PrerequisiteCheckUnitPropertyValue();

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 400u, 41u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 2u, 100u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnIndexAboveTheArrayBoundReadsZeroAndANullUnitFails()
    {
        IUnitEntity unit = Unit(new Dictionary<Property, float> { [Property.ResourceMax0] = 9f });
        var check = new PrerequisiteCheckUnitPropertyValue();

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 0xC5u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 0u, 0xC5u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.GreaterThanOrEqual, 0u, 9u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerOverloadEvaluatesThePlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IWorldEntity.GetPropertyValue), args => (Property)args[0] == Property.ResourceMax0 ? 1500f : 0f);

        Assert.True(new PrerequisiteCheckUnitPropertyValue().Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 1000u, 9u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(Dictionary<Property, float> properties)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IWorldEntity.GetPropertyValue), args => properties.TryGetValue((Property)args[0], out float v) ? v : 0f);
        return unit;
    }
}
