using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Setting;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 143 (FUN_14049ef70) compares the client's world difficulty (singleton +0x6424, the field filled from
// ServerInstanceSettings.Difficulty next to the prime level and level-scaling fields) with Value: Equal returns equality, NotEqual its negation, other comparisons are false.
// Retail: 113 slots, comparison Equal only, Value 1 (74) or 0 (39), matching WorldDifficulty Veteran/Normal.
public class PrerequisiteCheckWorldDifficultyTests
{
    [Theory]
    [InlineData(WorldDifficulty.Normal, PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(WorldDifficulty.Normal, PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(WorldDifficulty.Veteran, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(WorldDifficulty.Veteran, PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(WorldDifficulty.Veteran, PrerequisiteComparison.NotEqual, 0u, true)]
    [InlineData(WorldDifficulty.Veteran, PrerequisiteComparison.NotEqual, 1u, false)]
    [InlineData(WorldDifficulty.Normal, PrerequisiteComparison.GreaterThan, 0u, false)]
    [InlineData(WorldDifficulty.Veteran, PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    [InlineData(WorldDifficulty.Normal, PrerequisiteComparison.LessThan, 1u, false)]
    public void ComparesThePlayersWorldDifficultyWithValue(WorldDifficulty difficulty, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.InstanceDifficulty), difficulty);

        Assert.Equal(expected, new PrerequisiteCheckWorldDifficulty().Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ObjectIdIsIgnored()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.InstanceDifficulty), WorldDifficulty.Veteran);

        Assert.True(new PrerequisiteCheckWorldDifficulty().Meets(player, PrerequisiteComparison.Equal, 1u, 18939u, new PrerequisiteParameters()));
    }
}
