using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_VITAL_COMPARE_20261107.md): prerequisite type 114 (evaluator case 0x72) reads two vitals of the unit through FUN_140466520 - the first with ObjectId, the
// second with Value - and compares them as floats: the disassembly moves the first result to XMM2 (actual) and the second to XMM3 (expected) before FUN_1404a2010, so the test is vital(ObjectId)
// <comparison> vital(Value). A null unit is false. Retail: 10 slots, ObjectId and Value are vital ids 7, 8 and 9, comparisons >= and >: (7,8,>=) (7,9,>=) (8,7,>) (9,7,>) (8,9,>) (9,8,>).
// Status: Implemented (disassembly of the argument order); an unreadable vital counts as 0 like the native reader's default.
public class PrerequisiteCheckVital114Tests
{
    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 7, 8, 30f, 20f, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 7, 8, 20f, 20f, true)]       // ties: the lower id wins with >=
    [InlineData(PrerequisiteComparison.GreaterThan, 8, 7, 20f, 20f, false)]             // ... and the higher id loses with >
    [InlineData(PrerequisiteComparison.GreaterThan, 8, 7, 21f, 20f, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 7, 9, 19f, 20f, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 9, 8, 5.5f, 5.25f, true)]
    [InlineData(PrerequisiteComparison.Equal, 7, 8, 4f, 4f, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 7, 8, 4f, 4f, false)]
    [InlineData(PrerequisiteComparison.LessThan, 7, 8, 3f, 4f, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 7, 8, 5f, 4f, false)]
    [InlineData((PrerequisiteComparison)0, 7, 8, 3f, 4f, false)]
    public void ComparesTheVitalOfObjectIdWithTheVitalOfValue(PrerequisiteComparison comparison, uint objectId, uint value, float first, float second, bool expected)
    {
        IUnitEntity unit = Unit(new Dictionary<Vital, float> { [(Vital)objectId] = first, [(Vital)value] = second });

        Assert.Equal(expected, new PrerequisiteCheckVital114().MeetsUnit(unit, comparison, value, objectId, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnUnreadableVitalCountsAsZeroAndANullUnitFails()
    {
        IUnitEntity unit = Unit(new Dictionary<Vital, float> { [(Vital)7] = 3f });
        var check = new PrerequisiteCheckVital114();

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 8u, 7u, new PrerequisiteParameters()));       // 3 > 0
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.GreaterThanOrEqual, 8u, 7u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerOverloadEvaluatesThePlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            args[1] = (Vital)args[0] == (Vital)7 ? 9f : 1f;
            return true;
        });

        Assert.True(new PrerequisiteCheckVital114().Meets(player, PrerequisiteComparison.GreaterThan, 8u, 7u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(Dictionary<Vital, float> vitals)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            if (!vitals.TryGetValue((Vital)args[0], out float value))
                return false;

            args[1] = value;
            return true;
        });
        return unit;
    }
}
