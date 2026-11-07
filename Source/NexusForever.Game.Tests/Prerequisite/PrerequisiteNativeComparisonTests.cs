using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_NATIVE_COMPARISONS_20261107.md):
//  - type 11 OtherPrerequisite (evaluator case 0xb): comparison 2 returns the NOT of the nested prerequisite, every other comparison returns the nested result. Retail uses comparison 1 (3,559
//    slots), 2 (322) and 0 (22).
//  - type 52 Path (evaluator case 0x34, FUN_14049fd50): the player's path equals Value; comparison 1 returns that, 2 its negation, any other comparison false. Retail: Equal 538, NotEqual 14.
public class PrerequisiteNativeComparisonTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData((PrerequisiteComparison)0, true, true)]                    // 22 retail slots use comparison 0
    [InlineData((PrerequisiteComparison)0, false, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, false, false)]
    public void OtherPrerequisiteNegatesOnlyForComparisonTwo(PrerequisiteComparison comparison, bool nested, bool expected)
    {
        IPrerequisiteManager manager = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), _ => nested);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.MeetsUnit), _ => nested);
        var check = new PrerequisiteCheckOtherPrerequisite(NullLogger<PrerequisiteCheckOtherPrerequisite>.Instance, manager);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, check.Meets(player, comparison, 0u, 1234u, new PrerequisiteParameters()));
        Assert.Equal(expected, check.MeetsUnit(player, comparison, 0u, 1234u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, Path.Soldier, true)]
    [InlineData(PrerequisiteComparison.Equal, Path.Scientist, false)]
    [InlineData(PrerequisiteComparison.NotEqual, Path.Soldier, false)]
    [InlineData(PrerequisiteComparison.NotEqual, Path.Scientist, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, Path.Soldier, false)]
    [InlineData(PrerequisiteComparison.LessThan, Path.Scientist, false)]
    [InlineData((PrerequisiteComparison)0, Path.Soldier, false)]
    public void PathComparesTheActivePathWithValue(PrerequisiteComparison comparison, Path value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.IsPathActive), args => (Path)args[0] == Path.Soldier);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckPath(NullLogger<PrerequisiteCheckPath>.Instance);

        Assert.Equal(expected, check.Meets(player, comparison, (uint)value, 0u, new PrerequisiteParameters()));
    }
}
