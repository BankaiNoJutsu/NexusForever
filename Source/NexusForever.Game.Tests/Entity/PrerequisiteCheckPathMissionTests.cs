using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Native evaluators (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 67 (FUN_14049fe10) finds the path mission with id Value and reads its completion (mission virtual +0x38):
// Equal meets when complete, NotEqual when not (a missing mission is not complete), other comparisons never meet. Type 61 (FUN_14049fef0 over FUN_1404a1aa0) counts the completed
// missions of PathEpisode ObjectId, skipping missions whose PathMissionFlags has 0x40, and compares the count with Value using the comparison (count first, Value second). The compare helper
// FUN_1404a2090 orders its arguments (comparison, actual, expected). Retail: 67 has 191 slots (Equal 113, NotEqual 78, Value a PathMission id in 174); 61 has 25 slots (episode ids in 22,
// comparisons >= and =, Value 1 to 10).
public class PrerequisiteCheckPathMissionTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 100u, true)]
    [InlineData(PrerequisiteComparison.Equal, 101u, false)]
    [InlineData(PrerequisiteComparison.Equal, 999u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 100u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 101u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 999u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 100u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 101u, false)]
    public void PathMissionRequirementComparesTheCompletionOfTheMissionInValue(PrerequisiteComparison comparison, uint missionId, bool expected)
    {
        IPlayer player = Player(100u);

        Assert.Equal(expected, new PrerequisiteCheckPathMissionRequirement().Meets(player, comparison, missionId, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 2u, true)]              // missions 100 and 101 of episode 7 are complete; 102 is not; 103 is flagged and complete
    [InlineData(PrerequisiteComparison.Equal, 3u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 1u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 3u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, false)]
    public void PathMissionCountComparesTheCompletedMissionsOfTheEpisodeWithValue(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(100u, 101u, 103u);

        Assert.Equal(expected, Count().Meets(player, comparison, value, 7u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PathMissionCountIgnoresOtherEpisodesAndAnUnknownEpisodeCountsZero()
    {
        IPlayer player = Player(100u, 101u, 200u);

        Assert.True(Count().Meets(player, PrerequisiteComparison.Equal, 1u, 8u, new PrerequisiteParameters()));
        Assert.True(Count().Meets(player, PrerequisiteComparison.Equal, 0u, 99u, new PrerequisiteParameters()));
    }

    // Type 155 (FUN_1404a0360): the mission with id ObjectId is looked up and its state (mission virtual +0xd0, 0 when absent) is compared with Value. Retail: 174 slots, ObjectId a
    // PathMission id in 155, Value 0 to 4 matching PathMissionState (NoMission 0, Unlocked 2, Started 3, Complete 4), comparisons Equal 138 and NotEqual 36.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, PathMissionState.Started, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, PathMissionState.Started, 4u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, PathMissionState.Started, 4u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, PathMissionState.Started, 3u, false)]
    [InlineData(PrerequisiteComparison.Equal, PathMissionState.Complete, 4u, true)]
    [InlineData(PrerequisiteComparison.Equal, PathMissionState.NoMission, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, PathMissionState.Unlocked, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, PathMissionState.Complete, 3u, true)]
    [InlineData(PrerequisiteComparison.LessThan, PathMissionState.Started, 4u, true)]
    public void PathMissionStateComparesTheMissionStateWithValue(PrerequisiteComparison comparison, PathMissionState state, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager paths = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.GetMissionState), args => (uint)args[0] == 55u ? state : PathMissionState.NoMission);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), paths);

        Assert.Equal(expected, new PrerequisiteCheckPathMissionState().Meets(player, comparison, value, 55u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]        // an absent mission has state 0
    [InlineData(PrerequisiteComparison.Equal, 3u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    public void PathMissionStateTreatsAnAbsentMissionAsStateZero(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager paths = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.GetMissionState), args => PathMissionState.NoMission);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), paths);

        Assert.Equal(expected, new PrerequisiteCheckPathMissionState().Meets(player, comparison, value, 56u, new PrerequisiteParameters()));
    }

    private static PrerequisiteCheckPathMissionCount Count()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.PathMission), PlayerBindPointTests.Table(
            new PathMissionEntry { Id = 100, PathEpisodeId = 7 },
            new PathMissionEntry { Id = 101, PathEpisodeId = 7 },
            new PathMissionEntry { Id = 102, PathEpisodeId = 7 },
            new PathMissionEntry { Id = 103, PathEpisodeId = 7, PathMissionFlags = 0x40 },
            new PathMissionEntry { Id = 200, PathEpisodeId = 8 }));
        return new PrerequisiteCheckPathMissionCount(tables);
    }

    private static IPlayer Player(params uint[] completeMissions)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager paths = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.IsMissionComplete), args => completeMissions.Contains((uint)args[0]));
        playerProxy.SetProperty(nameof(IPlayer.PathManager), paths);
        return player;
    }
}
