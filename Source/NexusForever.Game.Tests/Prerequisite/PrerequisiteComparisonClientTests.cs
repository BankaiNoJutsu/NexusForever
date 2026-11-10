using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

public class PrerequisiteComparisonClientTests
{
    // Raw table IDs, deliberately independent of the server enum labels.
    // Native build 16042 integer/float helpers: 1404a2090 / 1404a2010.
    public static IEnumerable<object[]> Boundaries()
    {
        bool[][] outcomes =
        [
            [false, true, false], // 1 equal
            [true, false, true],  // 2 not equal
            [false, false, true], // 3 greater
            [false, true, true],  // 4 greater or equal
            [true, false, false], // 5 less
            [true, true, false]   // 6 less or equal
        ];
        for (uint mode = 1; mode <= 6; mode++)
            for (byte level = 9; level <= 11; level++)
                yield return [mode, level, outcomes[mode - 1][level - 9]];
    }

    [Theory]
    [MemberData(nameof(Boundaries))]
    public void IntegerLevelUsesClientTableComparisonIds(uint mode, byte level, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Level), (uint)level);
        var check = new PrerequisiteCheckLevel(NullLogger<PrerequisiteCheckLevel>.Instance);
        Assert.Equal(expected, check.Meets(player, (PrerequisiteComparison)mode, 10, 0, new PrerequisiteParameters()));
    }

    [Theory]
    [MemberData(nameof(Boundaries))]
    public void FloatVitalUsesSameClientTableComparisonIds(uint mode, byte level, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPlayer.TryGetVitalValue), args => { args[1] = (float)level; return true; });
        var check = new PrerequisiteCheckVital(null, NullLogger<PrerequisiteCheckVital>.Instance);
        Assert.Equal(expected, check.Meets(player, (PrerequisiteComparison)mode, 10, 1, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(7u)]
    [InlineData(uint.MaxValue)]
    public void UnknownComparisonIdsStillFailClosed(uint mode)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Level), 10u);
        proxy.SetMethodHandler(nameof(IPlayer.TryGetVitalValue), args => { args[1] = 10f; return true; });
        Assert.False(new PrerequisiteCheckLevel(NullLogger<PrerequisiteCheckLevel>.Instance)
            .Meets(player, (PrerequisiteComparison)mode, 10, 0, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckVital(null, NullLogger<PrerequisiteCheckVital>.Instance)
            .Meets(player, (PrerequisiteComparison)mode, 10, 1, new PrerequisiteParameters()));
    }
}
