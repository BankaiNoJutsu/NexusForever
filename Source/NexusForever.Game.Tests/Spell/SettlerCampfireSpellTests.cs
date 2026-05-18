using NexusForever.Game.Spell;

namespace NexusForever.Game.Tests.Spell;

public class SettlerCampfireSpellTests
{
    [Theory]
    [InlineData(0u, 32766u)]
    [InlineData(1u, 32777u)]
    [InlineData(2u, 32778u)]
    public void TryGetBackInActionSpell4Id_MapsClientTierIndexToBuffSpell(uint tierIndex, uint expectedSpell4Id)
    {
        Assert.True(SettlerCampfireSpell.TryGetBackInActionSpell4Id(tierIndex, out uint spell4Id));
        Assert.Equal(expectedSpell4Id, spell4Id);
    }

    [Theory]
    [InlineData(3u)]
    [InlineData(99u)]
    public void TryGetBackInActionSpell4Id_RejectsUnknownTierIndex(uint tierIndex)
    {
        Assert.False(SettlerCampfireSpell.TryGetBackInActionSpell4Id(tierIndex, out uint spell4Id));
        Assert.Equal(0u, spell4Id);
    }
}
