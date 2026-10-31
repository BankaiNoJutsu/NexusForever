using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengeMedalPointsTests
{
    [Theory]
    [InlineData(100u, 1f, 100u)]
    [InlineData(100u, 1.25f, 125u)]
    [InlineData(100u, 1.5f, 150u)]
    [InlineData(100u, 1.75f, 175u)]
    [InlineData(100u, 1.239f, 123u)]
    [InlineData(10u, 0.7f, 7u)]
    [InlineData(16777217u, 1f, 16777216u)]
    [InlineData(1u, 2147483648f, 2147483648u)]
    [InlineData(1u, 4294967040f, 4294967040u)]
    [InlineData(0u, 1.5f, 0u)]
    [InlineData(100u, 0f, 0u)]
    public void OneIncrementUsesNativeFloat32ConversionMultiplicationAndTruncation(uint increment, float multiplier, uint expected)
    {
        var snapshot = new ChallengeRewardModifierSnapshot(multiplier, 17);
        Assert.True(snapshot.TryGetPoints(increment, out uint points));
        Assert.Equal(expected, points);
        Assert.Equal(17, snapshot.Revision);
    }

    [Theory]
    [InlineData(100u, float.NaN)]
    [InlineData(100u, float.PositiveInfinity)]
    [InlineData(100u, float.NegativeInfinity)]
    [InlineData(100u, -0.5f)]
    [InlineData(0u, -0.5f)]
    [InlineData(100u, float.MaxValue)]
    [InlineData(1u, 4294967296f)]
    [InlineData(uint.MaxValue, 1f)]
    public void UnsafeAwardsFailClosedWithoutWrappingOrSaturating(uint increment, float multiplier)
    {
        var snapshot = new ChallengeRewardModifierSnapshot(multiplier, 18);
        Assert.False(snapshot.TryGetPoints(increment, out uint points));
        Assert.Equal(0u, points);
    }

    [Fact]
    public void EachMedalTruncatesIndependentlyRatherThanRoundingACumulativeBase()
    {
        var snapshot = new ChallengeRewardModifierSnapshot(1.239f, 19);
        Assert.True(snapshot.TryGetPoints(100, out uint first));
        Assert.True(snapshot.TryGetPoints(100, out uint second));
        Assert.True(snapshot.TryGetPoints(100, out uint third));
        Assert.Equal(369u, first + second + third);
        Assert.True(snapshot.TryGetPoints(300, out uint singleCumulativeCalculation));
        Assert.Equal(371u, singleCumulativeCalculation);
    }

    [Fact]
    public void PremiumSourceIdentityAndCommittedLoyaltyChangeRevisionWithoutChangingRawApi()
    {
        var property = new ChallengeRewardProperty(new RewardPropertyEntry { Id = 4, RewardModifierValueTypeEnum = 0 }, new object());
        IPlayer actor = RecordingDispatchProxy<IPlayer>.Create(out _);
        property.Initialise(actor);
        ChallengeRewardModifierSnapshot identity = property.CaptureModifier();
        property.SetPremium([(223u, 0.25f)]);
        ChallengeRewardModifierSnapshot premium = property.CaptureModifier();
        Assert.True(premium.Revision > identity.Revision);
        Assert.Equal(1.25f, premium.Multiplier);
        Assert.Equal(0.25f, property.GetValue(3));
        property.SetPremium([(223u, 0.25f)]);
        Assert.Equal(premium.Revision, property.CaptureModifier().Revision);

        property.SetPremium([(999u, 0.25f)]);
        ChallengeRewardModifierSnapshot replaced = property.CaptureModifier();
        Assert.Equal(premium.Multiplier, replaced.Multiplier);
        Assert.True(replaced.Revision > premium.Revision);
        property.SetCommittedLoyalty(1);
        ChallengeRewardModifierSnapshot loyalty = property.CaptureModifier();
        Assert.Equal(1.5f, loyalty.Multiplier);
        Assert.Equal(0.25f, property.GetValue(3));
        Assert.True(loyalty.Revision > replaced.Revision);
        property.SetCommittedLoyalty(1);
        Assert.Equal(loyalty.Revision, property.CaptureModifier().Revision);

        property.Initialise(RecordingDispatchProxy<IPlayer>.Create(out _));
        Assert.True(property.CaptureModifier().Revision > loyalty.Revision);
        Assert.Equal(1.25f, property.CaptureModifier().Multiplier);
        Assert.True(premium.TryGetPoints(100, out uint frozen));
        Assert.Equal(125u, frozen);
    }
}
