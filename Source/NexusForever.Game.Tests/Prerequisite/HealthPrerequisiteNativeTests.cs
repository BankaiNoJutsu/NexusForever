using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native 1404a4d90 -> 1404a2010; the instruction-level NaN rule is recorded in
// Decomp/Analysis/PREREQUISITE_40_PRIMARY_SUBJECT_20261114.md.
public sealed class HealthPrerequisiteNativeTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    public void ZeroOverZeroFollowsNativeUnorderedFlags(int mode, bool expected)
    {
        IPlayer player = Player(0, 0);
        var check = new PrerequisiteCheckHealth();
        var comparison = (PrerequisiteComparison)mode;
        var parameters = new PrerequisiteParameters();

        Assert.Equal(expected, check.MeetsUnit(player, comparison, 50u, 0u, parameters));
        Assert.Equal(expected, check.Meets(player, comparison, 50u, 0u, parameters));
        Assert.False(check.MeetsUnit(null, comparison, 50u, 0u, parameters));
        Assert.False(check.Meets(null, comparison, 50u, 0u, parameters));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    public void PositiveHealthOverZeroRemainsPositiveInfinity(int mode, bool expected)
    {
        IPlayer player = Player(1, 0);
        var check = new PrerequisiteCheckHealth();
        var comparison = (PrerequisiteComparison)mode;
        var parameters = new PrerequisiteParameters();

        Assert.Equal(expected, check.MeetsUnit(player, comparison, 100u, 0u, parameters));
        Assert.Equal(expected, check.Meets(player, comparison, 100u, 0u, parameters));
    }

    [Theory]
    [InlineData(1u, 3u, PrerequisiteComparison.GreaterThan, 33u, true)]
    [InlineData(1u, 3u, PrerequisiteComparison.Equal, 33u, false)]
    [InlineData(1u, 3u, PrerequisiteComparison.LessThan, 34u, true)]
    [InlineData(uint.MaxValue, uint.MaxValue, PrerequisiteComparison.Equal, 100u, true)]
    [InlineData(uint.MaxValue, 100u, PrerequisiteComparison.Equal, uint.MaxValue, true)]
    [InlineData(1u, uint.MaxValue, PrerequisiteComparison.GreaterThan, 0u, true)]
    public void PercentageKeepsSinglePrecisionAndUnsignedOperands(uint health, uint maximum, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(health, maximum);
        var check = new PrerequisiteCheckHealth();
        var parameters = new PrerequisiteParameters();

        Assert.Equal(expected, check.MeetsUnit(player, comparison, value, uint.MaxValue, parameters));
        Assert.Equal(expected, check.Meets(player, comparison, value, uint.MaxValue, parameters));
    }

    private static IPlayer Player(uint health, uint maximum)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.Health), health);
        proxy.SetProperty(nameof(IWorldEntity.MaxHealth), maximum);
        return player;
    }
}
