using System.Numerics;
using NexusForever.Game.Static.Reputation;
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

    [Theory]
    [InlineData(Faction.Exile, -50.53350067138672f)]
    [InlineData(Faction.Dominion, 50.53350067138672f)]
    public void TryGetCombatSimulationTeleportPosition_ReturnsFactionLanding(Faction faction, float expectedX)
    {
        bool result = StarterTutorialDefinition.TryGetCombatSimulationTeleportPosition(faction, out Vector3 position);

        Assert.True(result);
        Assert.Equal(expectedX, position.X);
        Assert.Equal(-861.4010009765625f, position.Y);
        Assert.Equal(307.8080139160156f, position.Z);
    }
}
