using NexusForever.Script.Instance;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Boss-independent tests for the provisional encounter runner. Every cast delay and interval fed to it
    /// by a real boss script is an estimate, never an observed retail value; the runner is off by default.
    /// </summary>
    public class ProvisionalEncounterRunnerTests
    {
        private static ProvisionalEncounterPlan TwoPhasePlan() => new(
        [
            new ProvisionalPhase(null, [new ProvisionalCast(100u, 5d, 10d), new ProvisionalCast(101u, 8d, 0d)], "opening"),
            new ProvisionalPhase(0.5f, [new ProvisionalCast(200u, 3d, 6d)], "second")
        ]);

        [Fact]
        public void DisabledRunnerEmitsNothing()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan());

            Assert.False(runner.Enabled);
            Assert.Empty(runner.Update(60d, 10u, 100u, true));
            Assert.Equal(0, runner.PhaseIndex);
        }

        [Fact]
        public void CastsFireAfterTheFirstDelayThenOnEveryInterval()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            Assert.Empty(runner.Update(4d, 100u, 100u, true));
            Assert.Equal([100u], runner.Update(1d, 100u, 100u, true).Select(c => c.SpellId));
            Assert.Equal([101u], runner.Update(3d, 100u, 100u, true).Select(c => c.SpellId));
            Assert.Empty(runner.Update(5d, 100u, 100u, true));
            Assert.Equal([100u], runner.Update(2d, 100u, 100u, true).Select(c => c.SpellId));
        }

        [Fact]
        public void ZeroIntervalCastFiresOnlyOnce()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            Assert.Equal([100u, 101u], runner.Update(8d, 100u, 100u, true).Select(c => c.SpellId));
            Assert.DoesNotContain(runner.Update(100d, 100u, 100u, true), c => c.SpellId == 101u);
        }

        [Fact]
        public void NothingAdvancesOutOfCombat()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            Assert.Empty(runner.Update(60d, 100u, 100u, false));
            Assert.Empty(runner.Update(4d, 100u, 100u, true));
            Assert.Equal([100u], runner.Update(1d, 100u, 100u, true).Select(c => c.SpellId));
        }

        [Fact]
        public void HealthThresholdSwitchesThePhaseAndRestartsTheDelays()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            runner.Update(1d, 100u, 100u, true);
            Assert.Empty(runner.Update(0.1d, 50u, 100u, true));
            Assert.Equal(1, runner.PhaseIndex);

            Assert.Empty(runner.Update(2.5d, 50u, 100u, true));
            Assert.Equal([200u], runner.Update(1d, 50u, 100u, true).Select(c => c.SpellId));
            Assert.DoesNotContain(runner.Update(30d, 50u, 100u, true), c => c.SpellId is 100u or 101u);
        }

        [Fact]
        public void HealingBackAboveTheThresholdDoesNotRevertThePhase()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            runner.Update(0.1d, 40u, 100u, true);
            runner.Update(0.1d, 100u, 100u, true);

            Assert.Equal(1, runner.PhaseIndex);
        }

        [Fact]
        public void DroppingBelowSeveralThresholdsAtOnceLandsOnTheLastOne()
        {
            var plan = new ProvisionalEncounterPlan(
            [
                new ProvisionalPhase(null, [], "a"),
                new ProvisionalPhase(0.8f, [], "b"),
                new ProvisionalPhase(0.4f, [new ProvisionalCast(300u, 1d, 5d)], "c")
            ]);
            var runner = new ProvisionalEncounterRunner(plan) { Enabled = true };

            runner.Update(0.1d, 30u, 100u, true);

            Assert.Equal(2, runner.PhaseIndex);
        }

        [Fact]
        public void ResetReturnsToThePhaseOneSchedule()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            runner.Update(10d, 40u, 100u, true);
            runner.Reset();

            Assert.Equal(0, runner.PhaseIndex);
            Assert.Empty(runner.Update(4d, 100u, 100u, true));
            Assert.Equal([100u], runner.Update(1d, 100u, 100u, true).Select(c => c.SpellId));
        }

        [Fact]
        public void LargeTickEmitsEachCastAtMostOncePerUpdate()
        {
            var runner = new ProvisionalEncounterRunner(TwoPhasePlan()) { Enabled = true };

            Assert.Single(runner.Update(500d, 100u, 100u, true), c => c.SpellId == 100u);
        }

        [Fact]
        public void PlanValidationRejectsBadShapes()
        {
            Assert.Throws<ArgumentException>(() => new ProvisionalEncounterPlan([]));
            Assert.Throws<ArgumentException>(() => new ProvisionalEncounterPlan([new ProvisionalPhase(0.5f, [], "x")]));
            Assert.Throws<ArgumentException>(() => new ProvisionalEncounterPlan(
            [
                new ProvisionalPhase(null, [], "a"),
                new ProvisionalPhase(0.4f, [], "b"),
                new ProvisionalPhase(0.6f, [], "c")
            ]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProvisionalCast(1u, -1d, 5d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProvisionalCast(1u, 1d, -5d));
        }
    }
}
