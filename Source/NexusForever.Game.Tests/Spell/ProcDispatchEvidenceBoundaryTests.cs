using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ProcDispatchEvidenceBoundaryTests
{
    [Theory]
    [InlineData(ProcTriggerEventCandidate.DealDamage, 1u, ProcDispatchTargetRoute.Holder)]
    [InlineData(ProcTriggerEventCandidate.DealDamage, 2u, ProcDispatchTargetRoute.Holder)]
    [InlineData(ProcTriggerEventCandidate.DealDamage, 9u, ProcDispatchTargetRoute.Holder)]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamage, 4u, ProcDispatchTargetRoute.Counterpart)]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageMelee, 4u, ProcDispatchTargetRoute.Counterpart)]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageMelee, 12u, ProcDispatchTargetRoute.Counterpart)]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageRanged, 4u, ProcDispatchTargetRoute.Counterpart)]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageMagic, 4u, ProcDispatchTargetRoute.Counterpart)]
    [InlineData(ProcTriggerEventCandidate.HealOther, 12u, ProcDispatchTargetRoute.Counterpart)]
    public void Describe_AllowsSupportedTriggerEventAndTargetData(uint triggerEvent, uint targetData, ProcDispatchTargetRoute expectedRoute)
    {
        ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(triggerEvent, targetData);

        Assert.True(boundary.IsConservativelyDispatchSupported);
        Assert.True(boundary.TriggerEventSupported);
        Assert.True(boundary.TargetDataSupported);
        Assert.Equal(expectedRoute, boundary.TargetRoute);
        Assert.Equal("dispatch-supported", boundary.DispatchSupportLabel);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(5u)]
    [InlineData(11u)]
    public void Describe_BlocksUnsupportedTriggerEvents(uint triggerEvent)
    {
        ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(triggerEvent, 1u);

        Assert.False(boundary.IsConservativelyDispatchSupported);
        Assert.False(boundary.TriggerEventSupported);
        Assert.True(boundary.TargetDataSupported);
        Assert.Equal(ProcDispatchTargetRoute.Holder, boundary.TargetRoute);
        Assert.Equal("unsupported-trigger-event", boundary.BlockedReason);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(14u)]
    [InlineData(18u)]
    [InlineData(20u)]
    [InlineData(33u)]
    [InlineData(34u)]
    [InlineData(36u)]
    public void Describe_BlocksUnsupportedTargetDataTails(uint targetData)
    {
        ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(ProcTriggerEventCandidate.DealDamage, targetData);

        Assert.False(boundary.IsConservativelyDispatchSupported);
        Assert.True(boundary.TriggerEventSupported);
        Assert.False(boundary.TargetDataSupported);
        Assert.Null(boundary.TargetRoute);
        Assert.Equal("unsupported-target-data", boundary.BlockedReason);
    }

    [Fact]
    public void Describe_BlocksUnsupportedTriggerEventsAndTargetDataTogether()
    {
        ProcDispatchEvidenceBoundarySnapshot boundary = ProcDispatchEvidenceBoundary.Describe(99u, 36u);

        Assert.False(boundary.IsConservativelyDispatchSupported);
        Assert.False(boundary.TriggerEventSupported);
        Assert.False(boundary.TargetDataSupported);
        Assert.Equal("unsupported-trigger-event+unsupported-target-data", boundary.BlockedReason);
    }
}
