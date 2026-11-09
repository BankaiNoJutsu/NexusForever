using NexusForever.Script.Main.AI;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Boss-independent tests for the shared encounter health-threshold tracker (Phase A of
    /// INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). No boss value is encoded here.
    /// </summary>
    public class EncounterHealthPhasesTests
    {
        [Fact]
        public void ThresholdFiresOnceWhenHealthReachesIt()
        {
            var phases = new EncounterHealthPhases(0.6f);

            Assert.Empty(phases.Update(100u, 100u));
            Assert.Empty(phases.Update(61u, 100u));
            Assert.Equal([0], phases.Update(60u, 100u));
            Assert.Empty(phases.Update(59u, 100u));
            Assert.True(phases.HasCrossed(0));
        }

        [Fact]
        public void HealingBackAboveDoesNotRefire()
        {
            var phases = new EncounterHealthPhases(0.5f);

            Assert.Equal([0], phases.Update(40u, 100u));
            Assert.Empty(phases.Update(90u, 100u));
            Assert.Empty(phases.Update(45u, 100u));
        }

        [Fact]
        public void SeveralThresholdsCrossedInOneUpdateFireHighestFirst()
        {
            // Supplied out of order on purpose: indexes follow descending threshold order.
            var phases = new EncounterHealthPhases(0.4f, 0.8f, 0.6f);

            Assert.Equal([0, 1, 2], phases.Update(30u, 100u));
            Assert.Empty(phases.Update(10u, 100u));
        }

        [Fact]
        public void OnlyTheThresholdsAlreadyReachedFire()
        {
            var phases = new EncounterHealthPhases(0.8f, 0.4f);

            Assert.Equal([0], phases.Update(75u, 100u));
            Assert.False(phases.HasCrossed(1));
            Assert.Equal([1], phases.Update(35u, 100u));
        }

        [Fact]
        public void ResetRearmsEveryThreshold()
        {
            var phases = new EncounterHealthPhases(0.9f, 0.5f);

            Assert.Equal([0, 1], phases.Update(10u, 100u));
            phases.Reset();

            Assert.False(phases.HasCrossed(0));
            Assert.False(phases.HasCrossed(1));
            Assert.Empty(phases.Update(100u, 100u));
            Assert.Equal([0], phases.Update(85u, 100u));
        }

        [Fact]
        public void ZeroMaxHealthIsIgnored()
        {
            var phases = new EncounterHealthPhases(0.5f);

            Assert.Empty(phases.Update(0u, 0u));
            Assert.False(phases.HasCrossed(0));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(1f)]
        [InlineData(-0.1f)]
        [InlineData(1.5f)]
        public void ThresholdsMustBeStrictlyBetweenZeroAndOne(float threshold)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterHealthPhases(threshold));
        }

        [Fact]
        public void DuplicateThresholdsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new EncounterHealthPhases(0.5f, 0.5f));
        }

        [Fact]
        public void HasCrossedRejectsAnUnknownIndex()
        {
            var phases = new EncounterHealthPhases(0.5f);

            Assert.Throws<ArgumentOutOfRangeException>(() => phases.HasCrossed(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => phases.HasCrossed(-1));
        }
    }
}
