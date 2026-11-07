using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_RESOURCE_SLOT_20261107.md): prerequisite type 64 (jump-table entry 64 -> 1404a24ad) is NOT a path level. It reads the unit's resource slot ObjectId (0 to 10, the dword array at
// unit+0x21c that FUN_140466520 exposes as vitals Resource0..Resource10) as a float and compares it with Value (as a float): slot <comparison> Value. A slot index above 10 reads 0; a null unit is false.
// Retail: 107 slots, ObjectId 1 to 4, Value 0, 1, 25, 50, 60, 100 (resource thresholds, not levels; ObjectId 4 is not a path type).
// Status: Implemented (disassembly); supersedes the earlier path-type/path-level reading.
public class PrerequisiteCheckResourceSlotTests
{
    [Theory]
    [InlineData(0u, Vital.Resource0)]
    [InlineData(1u, Vital.Resource1)]
    [InlineData(4u, Vital.Resource4)]
    [InlineData(6u, Vital.Resource6)]
    [InlineData(7u, Vital.Resource7)]
    [InlineData(8u, Vital.Resource8)]
    [InlineData(9u, Vital.Resource9)]
    [InlineData(10u, Vital.Resource10)]
    public void ReadsTheResourceSlotOfObjectId(uint slot, Vital expected)
    {
        IUnitEntity unit = Unit(new Dictionary<Vital, float> { [expected] = 60f });

        Assert.True(new PrerequisiteCheckResourceSlot().MeetsUnit(unit, PrerequisiteComparison.GreaterThanOrEqual, 60u, slot, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckResourceSlot().MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 60u, slot, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 49f, 50u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 50f, 50u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 24.5f, 25u, true)]
    [InlineData(PrerequisiteComparison.Equal, 100f, 100u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 100f, 100u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 0f, 0u, true)]
    [InlineData((PrerequisiteComparison)0, 10f, 10u, false)]
    public void ComparesTheSlotWithValueAsFloats(PrerequisiteComparison comparison, float slot, uint value, bool expected)
    {
        IUnitEntity unit = Unit(new Dictionary<Vital, float> { [Vital.Resource1] = slot });

        Assert.Equal(expected, new PrerequisiteCheckResourceSlot().MeetsUnit(unit, comparison, value, 1u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ASlotAboveTenReadsZeroAndANullUnitFails()
    {
        IUnitEntity unit = Unit(new Dictionary<Vital, float> { [Vital.Resource1] = 9f });
        var check = new PrerequisiteCheckResourceSlot();

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 11u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.GreaterThan, 0u, 11u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.GreaterThanOrEqual, 0u, 1u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnUnreadableSlotCountsAsZero()
    {
        IUnitEntity unit = Unit(new Dictionary<Vital, float>());

        Assert.True(new PrerequisiteCheckResourceSlot().MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 3u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerOverloadEvaluatesThePlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            args[1] = (Vital)args[0] == Vital.Resource2 ? 80f : 1f;
            return true;
        });

        Assert.True(new PrerequisiteCheckResourceSlot().Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 80u, 2u, new PrerequisiteParameters()));
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
