using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_CHALLENGE_TIER_20261107.md): prerequisite type 70 (FUN_14049f9d0) looks up the client challenge record with id ObjectId; flag = record present && Value < record+0x20, and the
// boolean tail follows (Equal returns the flag, NotEqual its negation, every other comparison is false). ServerChallengeUpdate rows are copied word for word into the record (consumer FUN_14048ded0, row reader
// FUN_1400aa930), so +0x20 is the packet's CurrentTier (after CurrentCount +0x14, GoalCount +0x18, ObjectiveCompletion +0x1c). An absent record is flag false (NotEqual true). Retail: 25 slots, NotEqual 18,
// Equal 7, Value 0 to 2: "the challenge's current tier is above Value". Supersedes the completion-count proxy.
public class ChallengeTierPrerequisiteTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, 1u, true)]       // tier 1 > 0
    [InlineData(PrerequisiteComparison.Equal, 1u, 1u, false)]      // tier 1 > 1 is false
    [InlineData(PrerequisiteComparison.Equal, 1u, 2u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, 0u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, 3u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, 3u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 5u, 3u, false)]
    [InlineData((PrerequisiteComparison)0, 0u, 3u, false)]
    public void TestsWhetherTheCurrentTierIsAboveValue(PrerequisiteComparison comparison, uint value, uint tier, bool expected)
    {
        IPlayer player = Player(tier);

        Assert.Equal(expected, new PrerequisiteCheckChallengeTier().Meets(player, comparison, value, 79u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnOutOfRangeChallengeIdIsAnAbsentRecord()
    {
        IPlayer player = Player(tier: 9u);

        Assert.False(new PrerequisiteCheckChallengeTier().Meets(player, PrerequisiteComparison.Equal, 0u, 100_000u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckChallengeTier().Meets(player, PrerequisiteComparison.NotEqual, 0u, 100_000u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint tier)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager manager = RecordingDispatchProxy<IChallengeManager>.Create(out var managerProxy);
        managerProxy.SetMethodReturn(nameof(IChallengeManager.GetCurrentTier), tier);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager);
        return player;
    }
}
