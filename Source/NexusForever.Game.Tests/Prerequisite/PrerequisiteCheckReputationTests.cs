using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_HEALTH_PERCENT_20261107.md, evaluator case 5): the unit's reputation amount for faction ObjectId (the object at unit+0x118, virtual +0x20) is compared
// with Value through the FLOAT helper FUN_1404a2010. Retail Values are signed: 5 slots carry -2000, -30000 and -32000 and 13 slots test `< 0`, so a reputation below zero must compare as a negative number.
// The previous implementation truncated the amount to uint, which cannot represent a negative reputation.
public class PrerequisiteCheckReputationTests
{
    [Theory]
    [InlineData(-500f, PrerequisiteComparison.LessThan, 0, true)]                 // negative reputation is below 0
    [InlineData(-500f, PrerequisiteComparison.LessThan, -1, true)]                // Value is signed: -500 < -1
    [InlineData(-1f, PrerequisiteComparison.LessThan, -500, false)]
    [InlineData(-500f, PrerequisiteComparison.GreaterThanOrEqual, -2000, true)]   // Value -2000 is stored as 0xfffff830
    [InlineData(-3000f, PrerequisiteComparison.GreaterThanOrEqual, -2000, false)]
    [InlineData(-3000f, PrerequisiteComparison.LessThanOrEqual, -2000, true)]
    [InlineData(8000f, PrerequisiteComparison.GreaterThanOrEqual, 8000, true)]
    [InlineData(7999.5f, PrerequisiteComparison.GreaterThanOrEqual, 8000, false)]
    [InlineData(7999.5f, PrerequisiteComparison.LessThan, 8000, true)]
    [InlineData(0f, PrerequisiteComparison.Equal, 0, true)]
    [InlineData(1f, PrerequisiteComparison.NotEqual, 0, true)]
    [InlineData(100f, PrerequisiteComparison.GreaterThan, 99, true)]
    public void ComparesTheSignedReputationAmountAsAFloat(float amount, PrerequisiteComparison comparison, int signedValue, bool expected)
    {
        IPlayer player = Player(amount);

        Assert.Equal(expected, new PrerequisiteCheckReputation().Meets(player, comparison, unchecked((uint)signedValue), 1280u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AFactionWithoutAReputationRecordHasAmountZero()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IReputationManager manager = RecordingDispatchProxy<IReputationManager>.Create(out var managerProxy);
        managerProxy.SetMethodHandler(nameof(IReputationManager.GetReputation), _ => null);
        playerProxy.SetProperty(nameof(IPlayer.ReputationManager), manager);

        var check = new PrerequisiteCheckReputation();
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 1280u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 0u, 1280u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.LessThan, 0u, 1280u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(float amount)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IReputationManager manager = RecordingDispatchProxy<IReputationManager>.Create(out var managerProxy);
        IReputation reputation = RecordingDispatchProxy<IReputation>.Create(out var reputationProxy);
        reputationProxy.SetProperty(nameof(IReputation.Amount), amount);
        managerProxy.SetMethodReturn(nameof(IReputationManager.GetReputation), reputation);
        playerProxy.SetProperty(nameof(IPlayer.ReputationManager), manager);
        return player;
    }
}
