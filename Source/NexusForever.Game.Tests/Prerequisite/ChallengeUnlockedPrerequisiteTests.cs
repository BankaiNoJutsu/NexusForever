using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_CHALLENGE_UNLOCKED_20261107.md): prerequisite type 74 (FUN_14049fa80 -> FUN_14048d5a0) reads the 32-bit field at +0x2c of the client challenge record with id
// ObjectId (0 when the record is absent) and returns it for Equal, "the field is 0" for NotEqual and false for every other comparison. +0x2c is the Unlocked value of ServerChallengeUpdate (packet order after
// CurrentCount at +0x14 ... CompletionCount +0x28, Unlocked +0x2c, Activated +0x30, OnCooldown +0x34). The server only has a record for challenges it tracks and always sends Unlocked = true for them, so a
// challenge is unlocked exactly when the manager tracks it; the rest compare as 0 like an absent client record. Retail: 236 slots, NotEqual 185, Equal 51. Status: Implemented, inferred (which challenges
// retail listed to the client before they were touched is not known).
public class ChallengeUnlockedPrerequisiteTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, false, false)]
    [InlineData((PrerequisiteComparison)0, true, false)]
    public void ChallengeLockedTestsTheRecordsUnlockedField(PrerequisiteComparison comparison, bool unlocked, bool expected)
    {
        IPlayer player = Player(unlocked);

        Assert.Equal(expected, new PrerequisiteCheckChallengeLocked().Meets(player, comparison, 0u, 1092u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnOutOfRangeChallengeIdIsAnAbsentRecord()
    {
        IPlayer player = Player(unlocked: true);

        Assert.False(new PrerequisiteCheckChallengeLocked().Meets(player, PrerequisiteComparison.Equal, 0u, 100_000u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckChallengeLocked().Meets(player, PrerequisiteComparison.NotEqual, 0u, 100_000u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(bool unlocked)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager manager = RecordingDispatchProxy<IChallengeManager>.Create(out var managerProxy);
        managerProxy.SetMethodReturn(nameof(IChallengeManager.IsChallengeUnlocked), unlocked);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager);
        return player;
    }
}
