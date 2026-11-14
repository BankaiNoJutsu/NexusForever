using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Verified native type149 selector/predicate/writers re-derived in Decomp/Analysis/PREREQUISITE_149_PRIMARY_SUBJECT_20261114.md: state = (unit+0x1e4 != 0) || (unit+0x2ac != 0),
// where +0x1e4 is the in-use flag set by ServerUnitInUse (0x0755, the server's IsBusy) and +0x2ac is the in-combat flag; Equal tests state == Value, NotEqual state != Value. Retail slots: Equal 0 (30),
// Equal 1 (13), NotEqual 1 (2), NotEqual 0 (1).
public class UnitStatePrerequisiteTests
{
    [Theory]
    [InlineData(false, false, 0u)]
    [InlineData(true, false, 1u)]
    [InlineData(false, true, 1u)]
    [InlineData(true, true, 1u)]
    public void TheStateIsOneWhenTheUnitIsBusyOrInCombat(bool busy, bool combat, uint state)
    {
        var check = new PrerequisiteCheckEntityState();
        IUnitEntity unit = Unit(busy, combat);

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.Equal, state, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 1u - state, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.NotEqual, state, 0u, new PrerequisiteParameters()));
        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.NotEqual, 1u - state, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void OtherComparisonsAreFalse()
    {
        var check = new PrerequisiteCheckEntityState();

        Assert.False(check.MeetsUnit(Unit(true, true), PrerequisiteComparison.GreaterThan, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(false, false, 2u)]
    [InlineData(true, false, 2u)]
    [InlineData(false, true, uint.MaxValue)]
    [InlineData(true, true, uint.MaxValue)]
    public void ValueRemainsAnIntegerOperand(bool busy, bool combat, uint value)
    {
        var check = new PrerequisiteCheckEntityState();
        IUnitEntity unit = Unit(busy, combat);

        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.Equal, value, uint.MaxValue, null));
        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.NotEqual, value, uint.MaxValue, null));
    }

    [Theory]
    [InlineData((PrerequisiteComparison)0)]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual)]
    [InlineData(PrerequisiteComparison.LessThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)7)]
    public void UnsupportedRawModesFail(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckEntityState().MeetsUnit(Unit(true, true), comparison, 2u, 0u, null));
    }

    [Fact]
    public void ThePlayerOverloadRetainsThePrimaryWhenARelationshipIsGiven()
    {
        var check = new PrerequisiteCheckEntityState();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters { Target = Unit(busy: true, combat: false) };

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, parameters));
        Assert.True(check.MeetsUnit(parameters.Target, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters { Target = player }));
    }

    [Fact]
    public void ThePlayerOverloadEvaluatesThePlayerWithoutATarget()
    {
        var check = new PrerequisiteCheckEntityState();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.InCombat), true);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(bool busy, bool combat)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);
        proxy.SetProperty(nameof(IUnitEntity.IsBusy), busy);
        proxy.SetProperty(nameof(IUnitEntity.InCombat), combat);
        return unit;
    }
}
