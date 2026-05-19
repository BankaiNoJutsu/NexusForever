using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ProcTargetDataCandidateTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(9u)]
    public void TryGetConservativeDispatchRoute_RecognizesHolderRoutes(uint targetData)
    {
        Assert.True(ProcTargetDataCandidate.TryGetConservativeDispatchRoute(targetData, out ProcDispatchTargetRoute route));
        Assert.Equal(ProcDispatchTargetRoute.Holder, route);
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(12u)]
    public void TryGetConservativeDispatchRoute_RecognizesCounterpartRoutes(uint targetData)
    {
        Assert.True(ProcTargetDataCandidate.TryGetConservativeDispatchRoute(targetData, out ProcDispatchTargetRoute route));
        Assert.Equal(ProcDispatchTargetRoute.Counterpart, route);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(10u)]
    [InlineData(14u)]
    [InlineData(17u)]
    [InlineData(18u)]
    [InlineData(20u)]
    [InlineData(33u)]
    [InlineData(34u)]
    [InlineData(36u)]
    public void TryGetConservativeDispatchRoute_RejectsUnsupportedTargetData(uint targetData)
    {
        Assert.False(ProcTargetDataCandidate.TryGetConservativeDispatchRoute(targetData, out _));
        Assert.False(ProcTargetDataCandidate.IsConservativelySupported(targetData));
        Assert.False(ProcTargetDataCandidate.TryGetConservativeLabel(targetData, out _));
    }
}
