using NexusForever.Game.Static.Tutorial;

namespace NexusForever.Game.Tests.Quests;

public class StarterTutorialHoverboardFinishRecoveryTests
{
    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    public void ShouldRecoverHoverboardFinishPosition_RequiresProjectorAndRideCompleteBeforeFinishInteractable(
        bool projectorObjectiveComplete,
        bool rideObjectiveComplete,
        bool finishObjectiveComplete,
        bool expected)
    {
        bool result = StarterTutorialDefinition.ShouldRecoverHoverboardFinishPosition(
            projectorObjectiveComplete,
            rideObjectiveComplete,
            finishObjectiveComplete);

        Assert.Equal(expected, result);
    }
}
