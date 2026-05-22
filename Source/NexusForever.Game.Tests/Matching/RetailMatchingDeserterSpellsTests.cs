using NexusForever.Game.Retail;

namespace NexusForever.Game.Tests.Matching;

public class RetailMatchingDeserterSpellsTests
{
    [Theory]
    [InlineData(0d, 45554u)]
    [InlineData(0.5d, 45553u)]
    [InlineData(0.9d, 45453u)]
    public void GetPveDeserterSpell4Id_ScalesWithCompletion(double completionRatio, uint expectedSpell4Id)
    {
        Assert.Equal(expectedSpell4Id, RetailMatchingDeserterSpells.GetPveDeserterSpell4Id(completionRatio));
    }
}
