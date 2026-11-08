using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite type 149 State (native FUN_14049c840; agent-written read of the writers in Decomp/Analysis/PREREQUISITE_9_149_WRITERS_NATIVE_READ_20261108.md): state = (unit+0x1e4 != 0) || (unit+0x2ac != 0),
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

    [Fact]
    public void ThePlayerOverloadEvaluatesTheTargetWhenOneIsGiven()
    {
        var check = new PrerequisiteCheckEntityState();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters { Target = Unit(busy: true, combat: false) };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
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
