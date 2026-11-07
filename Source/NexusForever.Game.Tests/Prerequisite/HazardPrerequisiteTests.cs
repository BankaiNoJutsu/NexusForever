using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Hazard;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_HAZARD_20261107.md): the client's hazard manager (DAT_140c659a8) keeps a tree of the hazards the server listed, keyed by hazard id. Prerequisite type 111
// (FUN_1404a03e0) asks whether the hazard ObjectId has a record whose field +0x28 (the packet's Enabled flag, after Meter at +8) is non-zero: Equal returns that, NotEqual its negation, other comparisons are
// false; with no manager Equal is false and NotEqual true. Type 113 (FUN_1404a04f0 -> FUN_1404a6070) compares the record's meter value (+8, the packet MeterValue) with (float)Value through the float
// helper, actual first; a hazard without a record reads -1.0 (DAT_140b7a23c). Retail: 111 has 47 slots (Equal 44), 113 has 36 slots with Values 0 to 584. Status: Implemented, inferred (the +0x28 field is
// identified by the packet field order; type 112 is not read).
public class HazardPrerequisiteTests
{
    [Fact]
    public void ACollectionReportsActiveHazardsAndTheirMeter()
    {
        var hazards = new HazardStateCollection();
        Assert.True(hazards.TryEnable(10u, Entry(91u, max: 105u), out _, out _));

        Assert.True(hazards.IsActive(91u));
        Assert.False(hazards.IsActive(92u));
        Assert.Equal(0f, hazards.GetMeterValue(91u));
        Assert.Equal(-1f, hazards.GetMeterValue(92u));

        Assert.True(hazards.TryModifyMeter(91u, 40f, out _));
        Assert.Equal(40f, hazards.GetMeterValue(91u));

        Assert.True(hazards.RemoveEnable(10u, out _, out _));
        Assert.False(hazards.IsActive(91u));
        Assert.Equal(-1f, hazards.GetMeterValue(91u));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    [InlineData((PrerequisiteComparison)0, true, false)]
    public void Type111TestsWhetherTheHazardIsEnabled(PrerequisiteComparison comparison, bool enabled, bool expected)
    {
        IPlayer player = Player(enabled, meter: 0f);

        Assert.Equal(expected, new PrerequisiteCheckHazardProperty111().Meets(player, comparison, 0u, 222u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 70u, 70f, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 71u, 70f, false)]
    [InlineData(PrerequisiteComparison.Equal, 100u, 100f, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 99u, 99.5f, true)]
    [InlineData(PrerequisiteComparison.LessThan, 40u, 39.9f, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 30u, 30.1f, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 30u, 30f, false)]
    [InlineData(PrerequisiteComparison.Equal, 0u, -1f, false)]       // a hazard without a record reads -1, not 0
    [InlineData(PrerequisiteComparison.LessThan, 0u, -1f, true)]
    public void Type113ComparesTheMeterValueAsAFloat(PrerequisiteComparison comparison, uint value, float meter, bool expected)
    {
        IPlayer player = Player(enabled: meter >= 0f, meter);

        Assert.Equal(expected, new PrerequisiteCheckHazardProperty113().Meets(player, comparison, value, 251u, new PrerequisiteParameters()));
    }

    private static HazardEntry Entry(uint id, uint max) => new()
    {
        Id             = id,
        MeterMaxValue  = max,
        Flags          = 0u,
        HazardTypeEnum = (uint)HazardType.Timer
    };

    private static IPlayer Player(bool enabled, float meter)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IPlayer.IsHazardEnabled), enabled);
        proxy.SetMethodReturn(nameof(IPlayer.GetHazardMeter), meter);
        return player;
    }
}
