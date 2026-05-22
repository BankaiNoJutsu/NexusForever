using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class SummonTrapEvidenceBoundaryTests
{
    [Fact]
    public void Describe_StalkerProximityMineFixture_AllowsConservativeCreate()
    {
        SummonTrapEvidenceBoundarySnapshot boundary = SummonTrapEvidenceBoundary.Describe(
            creatureId: 26850u,
            triggerSpell4Id: 34095u,
            creatureExists: true,
            triggerSpellExists: true);

        Assert.True(boundary.CreatureIdPresent);
        Assert.True(boundary.TriggerSpellResolvable);
        Assert.Null(boundary.BlockedReason);
        Assert.True(boundary.IsConservativelyCreateSupported);
    }

    [Fact]
    public void Describe_UnknownTriggerSpell_BlocksCreate()
    {
        SummonTrapEvidenceBoundarySnapshot boundary = SummonTrapEvidenceBoundary.Describe(
            creatureId: 26850u,
            triggerSpell4Id: 34095u,
            creatureExists: true,
            triggerSpellExists: false);

        Assert.Equal("unknown-trigger-spell4", boundary.BlockedReason);
        Assert.False(boundary.IsConservativelyCreateSupported);
    }
}
