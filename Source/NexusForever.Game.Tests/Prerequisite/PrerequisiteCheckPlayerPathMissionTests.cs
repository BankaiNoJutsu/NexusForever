using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_PATH_MISSION_ACTIVE_20261107.md): prerequisite type 54 (evaluator case 0x36 -> FUN_14049fdc0 -> FUN_1404a1950) looks the player's path mission up by
// Value (a mission id), asks the mission object's virtual +0x28 and returns that; Equal returns the value, NotEqual its negation, other comparisons false, and a missing service or mission gives 0.
// Virtual +0x28 returns the field at +0x18, which the class's Activate (+0x18, sets it to 1 unless the mission is complete) and Deactivate (+0x20, sets it to 0) virtuals write: it is the
// "mission is currently active" flag (events PlayerPathMissionActivate / Deactivate). ObjectId is not read. Retail: Equal 247, NotEqual 9, Value is a PathMission id (0 in 38 slots, which no mission has).
// Status: Implemented, inferred for the server mapping (an active mission is one in the server's Started state); the Soldier-path holdout-table refinement is not modelled.
public class PrerequisiteCheckPlayerPathMissionTests
{
    [Theory]
    [InlineData(PathMissionState.Started, PrerequisiteComparison.Equal, true)]
    [InlineData(PathMissionState.Started, PrerequisiteComparison.NotEqual, false)]
    [InlineData(PathMissionState.Unlocked, PrerequisiteComparison.Equal, false)]
    [InlineData(PathMissionState.Unlocked, PrerequisiteComparison.NotEqual, true)]
    [InlineData(PathMissionState.Complete, PrerequisiteComparison.Equal, false)]
    [InlineData(PathMissionState.Complete, PrerequisiteComparison.NotEqual, true)]
    [InlineData(PathMissionState.NoMission, PrerequisiteComparison.Equal, false)]
    [InlineData(PathMissionState.NoMission, PrerequisiteComparison.NotEqual, true)]
    [InlineData(PathMissionState.Started, PrerequisiteComparison.GreaterThan, false)]
    [InlineData(PathMissionState.Started, (PrerequisiteComparison)0, false)]
    public void AMissionIsMetWhileItIsTheActiveMission(PathMissionState state, PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = Player(state, out var pathProxy);

        Assert.Equal(expected, new PrerequisiteCheckPlayerPathMission().Meets(player, comparison, 126u, 99u, new PrerequisiteParameters()));
        var call = Assert.Single(pathProxy.GetInvocations(nameof(IPathManager.GetMissionState)));
        Assert.Equal(126u, call.Arguments[0]);        // Value is the mission id; ObjectId is not read
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    public void WithoutAPathManagerNoMissionIsActive(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, new PrerequisiteCheckPlayerPathMission().Meets(player, comparison, 126u, 0u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(PathMissionState state, out RecordingDispatchProxy<IPathManager> pathProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathProxy);
        pathProxy.SetMethodReturn(nameof(IPathManager.GetMissionState), state);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        return player;
    }
}
