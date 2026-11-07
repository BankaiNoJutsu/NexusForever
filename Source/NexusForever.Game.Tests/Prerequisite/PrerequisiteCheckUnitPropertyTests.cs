using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_UNIT_PROPERTY_COMPARE_20261107.md): prerequisite type 115 (evaluator case 0x73) compares two entries of the unit's 197-entry float property array, index ObjectId (actual)
// against index Value (expected); an index above 0xC4 reads 0; a null unit is false. Retail: 18 slots over ids {0, 2, 29, 34, 35, 36, 57}. Server mapping to Property is inferred from the 0xC4 bound.
public class PrerequisiteCheckUnitPropertyTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThan, 35, 36, 120f, 100f, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 35, 36, 100f, 100f, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 35, 36, 100f, 100f, true)]
    [InlineData(PrerequisiteComparison.LessThan, 34, 29, 1.5f, 2.5f, true)]
    [InlineData(PrerequisiteComparison.Equal, 0, 2, 7f, 7f, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0, 2, 7f, 7f, false)]
    [InlineData((PrerequisiteComparison)0, 0, 2, 7f, 8f, false)]
    public void ComparesThePropertyOfObjectIdWithThePropertyOfValue(PrerequisiteComparison comparison, uint objectId, uint value, float first, float second, bool expected)
    {
        IUnitEntity unit = Unit(new Dictionary<Property, float> { [(Property)objectId] = first, [(Property)value] = second });

        Assert.Equal(expected, new PrerequisiteCheckUnitProperty().MeetsUnit(unit, comparison, value, objectId, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnIndexAboveTheArrayBoundReadsZeroWithoutAskingTheUnit()
    {
        IUnitEntity unit = Unit(new Dictionary<Property, float> { [(Property)35] = 5f });
        var check = new PrerequisiteCheckUnitProperty();

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 0xC5u, 35u, new PrerequisiteParameters()));        // 5 > 0
        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 0xC5u, 0xC6u, new PrerequisiteParameters()));           // 0 == 0
        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 35u, 0xC5u, new PrerequisiteParameters()));       // 0 > 5
        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 0u, 35u, new PrerequisiteParameters()));           // index 0 is in range
    }

    [Fact]
    public void ANullUnitFails()
        => Assert.False(new PrerequisiteCheckUnitProperty().MeetsUnit(null, PrerequisiteComparison.GreaterThanOrEqual, 36u, 35u, new PrerequisiteParameters()));

    [Fact]
    public void ThePlayerOverloadEvaluatesThePlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IWorldEntity.GetPropertyValue), args => (Property)args[0] == (Property)35 ? 9f : 1f);

        Assert.True(new PrerequisiteCheckUnitProperty().Meets(player, PrerequisiteComparison.GreaterThan, 36u, 35u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(Dictionary<Property, float> properties)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IWorldEntity.GetPropertyValue), args => properties.TryGetValue((Property)args[0], out float v) ? v : 0f);
        return unit;
    }
}
