using NexusForever.Game.Static.Combat.CrowdControl;

namespace NexusForever.Game.Tests.Spell;

public class CCStateSetFixtureWitnessTests
{
    [Fact]
    public void HivemindTrap57355_MapsDataBits00ToTether()
    {
        Assert.Equal(CCState.Tether, (CCState)20u);
    }
}
