using NexusForever.Game.Entity;

namespace NexusForever.Game.Tests.Entity;

public class RestXpCalculationTests
{
    [Fact]
    public void CalculateModifiedRestBonusXp_AddsLevelSpanMultiplier()
    {
        uint result = XpManager.CalculateModifiedRestBonusXp(
            currentRestBonusXp: 100u,
            levelXpSpan: 1000u,
            maximumRestBonusXp: 1500u,
            levelSpanMultiplier: 0.5f);

        Assert.Equal(600u, result);
    }

    [Fact]
    public void CalculateModifiedRestBonusXp_ClampsToMaximum()
    {
        uint result = XpManager.CalculateModifiedRestBonusXp(
            currentRestBonusXp: 250u,
            levelXpSpan: 1000u,
            maximumRestBonusXp: 1500u,
            levelSpanMultiplier: 5f);

        Assert.Equal(1500u, result);
    }

    [Fact]
    public void CalculateModifiedRestBonusXp_ClampsNegativeToZero()
    {
        uint result = XpManager.CalculateModifiedRestBonusXp(
            currentRestBonusXp: 250u,
            levelXpSpan: 1000u,
            maximumRestBonusXp: 1500u,
            levelSpanMultiplier: -1.5f);

        Assert.Equal(0u, result);
    }

    [Fact]
    public void CalculateModifiedRestBonusXp_InvalidOrMaxLevelInputsReturnZero()
    {
        Assert.Equal(0u, XpManager.CalculateModifiedRestBonusXp(250u, 0u, 1500u, 1f));
        Assert.Equal(0u, XpManager.CalculateModifiedRestBonusXp(250u, 1000u, 0u, 1f));
    }

    [Fact]
    public void CalculateModifiedRestBonusXp_InvalidMultiplierKeepsCurrentClampedValue()
    {
        Assert.Equal(250u, XpManager.CalculateModifiedRestBonusXp(250u, 1000u, 1500u, float.NaN));
        Assert.Equal(1500u, XpManager.CalculateModifiedRestBonusXp(2000u, 1000u, 1500u, float.PositiveInfinity));
    }
}
