using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ProcFixtureWitnessTests
{
    [Fact]
    public void BrutalDamageProc4046_IsConservativelyDispatchSupported()
    {
        ProcDispatchEvidenceBoundarySnapshot boundary =
            ProcDispatchEvidenceBoundary.Describe(ProcTriggerEventCandidate.DealDamage, 4u);

        Assert.True(boundary.IsConservativelyDispatchSupported);
        Assert.Equal(ProcDispatchTargetRoute.Counterpart, boundary.TargetRoute);
        Assert.Equal("deal-damage", boundary.TriggerEventLabel);
        Assert.Equal("counterpart route candidate", boundary.TargetDataLabel);
    }
}
