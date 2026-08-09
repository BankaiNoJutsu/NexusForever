using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

public class PrerequisiteCheckAchievementStateTests
{
    [Theory]
    [InlineData(false, PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(false, PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(true, PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(true, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(false, PrerequisiteComparison.NotEqual, 1u, true)]
    [InlineData(true, PrerequisiteComparison.NotEqual, 0u, true)]
    public void Meets_ComparesCompletedStateAgainstClientValue(
        bool completed,
        PrerequisiteComparison comparison,
        uint expectedState,
        bool expected)
    {
        IPlayer player = CreatePlayer(completed);
        var check = new PrerequisiteCheckAchievementState(
            NullLogger<PrerequisiteCheckAchievementState>.Instance);

        bool result = check.Meets(player, comparison, expectedState, 5881u, null);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Meets_WithUnsupportedAchievementState_FailsClosed()
    {
        IPlayer player = CreatePlayer(completed: true);
        var check = new PrerequisiteCheckAchievementState(
            NullLogger<PrerequisiteCheckAchievementState>.Instance);

        bool result = check.Meets(
            player,
            PrerequisiteComparison.Equal,
            3u,
            5881u,
            null);

        Assert.False(result);
    }

    private static IPlayer CreatePlayer(bool completed)
    {
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(
                out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(
            out RecordingDispatchProxy<IPlayer> playerProxy);

        achievementProxy.SetMethodHandler(
            nameof(ICharacterAchievementManager.HasCompletedAchievement),
            args =>
            {
                Assert.Equal((ushort)5881, (ushort)args[0]);
                return completed;
            });
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);

        return player;
    }
}
