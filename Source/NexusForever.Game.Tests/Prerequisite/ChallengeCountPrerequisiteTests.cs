using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_CHALLENGE_FIELDS_20261107.md): the four challenge prerequisites 74, 124, 125 and 160 read a 32-bit field of the client challenge record found by ObjectId in the
// map at DAT_140c65948+0x30 (absent record: field 0; no map: false) and pass (comparison, field, Value) to the shared compare helper, 124 reading +0x28 and 160 reading +0x14. The record layout is read
// from the order of ServerChallengeUpdate (ChallengeId, Type, TargetGroupId, QualifyCount, QualityTotal, CurrentCount at +0x14, GoalCount, ObjectiveCompletion, CurrentTier, LastRewardTier, CompletionCount at +0x28),
// confirmed by the client Lua accessors (GetCurrentCount reads +0x14, GetCompletionCount +0x28, IsActivated +0x30, IsInCooldown +0x34). Status: Implemented.
public class ChallengeCountPrerequisiteTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 3u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 3u, true)]
    public void ChallengeCompletionCountComparesTheRecordsCompletionCount(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(completionCount: 3u, currentCount: 9u);

        Assert.Equal(expected, new PrerequisiteCheckChallengeCompletionCount().Meets(player, comparison, value, 1114u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 9u, true)]
    [InlineData(PrerequisiteComparison.Equal, 3u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 9u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 4u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 10u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 9u, false)]
    public void ChallengeCurrentCountComparesTheRecordsCurrentCount(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(completionCount: 3u, currentCount: 9u);

        Assert.Equal(expected, new PrerequisiteCheckChallengeCurrentCount().Meets(player, comparison, value, 1078u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnAbsentChallengeRecordComparesZeroAndAnOutOfRangeIdIsAbsent()
    {
        IPlayer player = Player(completionCount: 0u, currentCount: 0u);

        Assert.True(new PrerequisiteCheckChallengeCurrentCount().Meets(player, PrerequisiteComparison.Equal, 0u, 99u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckChallengeCompletionCount().Meets(player, PrerequisiteComparison.LessThan, 1u, 99u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckChallengeCurrentCount().Meets(player, PrerequisiteComparison.GreaterThan, 0u, 100_000u, new PrerequisiteParameters()));
    }

    // Type 125 (FUN_14049fac0) returns the raw 32-bit field at +0x34 for Equal and "field is 0" for NotEqual, every other comparison false. The client's own Lua IsInCooldown
    // (FUN_140685ad0) tests that same field (`*(record+0x34) != 0`), so it is the on-cooldown flag. Retail: NotEqual 145, Equal 8.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, false, false)]
    [InlineData((PrerequisiteComparison)0, true, false)]
    public void ChallengeOnCooldownTestsTheRecordsCooldownFlag(PrerequisiteComparison comparison, bool onCooldown, bool expected)
    {
        IPlayer player = Player(completionCount: 0u, currentCount: 0u, onCooldown: onCooldown);

        Assert.Equal(expected, new PrerequisiteCheckChallengeOnCooldown().Meets(player, comparison, 0u, 742u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ChallengeOnCooldownTreatsAnAbsentRecordAsNotOnCooldown()
    {
        IPlayer player = Player(completionCount: 0u, currentCount: 0u, onCooldown: true);

        // Only challenges 742, 1114 and 1078 have a record; 99 and an id beyond 16 bits do not.
        Assert.False(new PrerequisiteCheckChallengeOnCooldown().Meets(player, PrerequisiteComparison.Equal, 0u, 99u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckChallengeOnCooldown().Meets(player, PrerequisiteComparison.NotEqual, 0u, 99u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckChallengeOnCooldown().Meets(player, PrerequisiteComparison.NotEqual, 0u, 100_000u, new PrerequisiteParameters()));
    }

    // The fake manager returns the configured state only for the challenge ids 742, 1114 and 1078.
    private static IPlayer Player(uint completionCount, uint currentCount, bool onCooldown = false)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager manager = RecordingDispatchProxy<IChallengeManager>.Create(out var managerProxy);
        managerProxy.SetMethodHandler(nameof(IChallengeManager.GetCompletionCount), args => (ushort)args[0] is 1114 or 1078 ? completionCount : 0u);
        managerProxy.SetMethodHandler(nameof(IChallengeManager.GetCurrentCount), args => (ushort)args[0] is 1114 or 1078 ? currentCount : 0u);
        managerProxy.SetMethodHandler(nameof(IChallengeManager.IsChallengeOnCooldown), args => (ushort)args[0] is 742 or 1114 or 1078 && onCooldown);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager);
        return player;
    }
}
