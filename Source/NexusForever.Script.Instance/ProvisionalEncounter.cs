using NexusForever.Script.Main.AI;

namespace NexusForever.Script.Instance
{
    /// <summary>
    /// A boss cast scheduled by a provisional encounter plan.
    /// Delays and intervals are ESTIMATES unless a plan states otherwise: no source available in this
    /// environment gives retail cast timing, so they must never be described as retail values.
    /// </summary>
    public sealed record ProvisionalCast
    {
        public uint SpellId { get; }
        public double FirstDelaySeconds { get; }
        public double IntervalSeconds { get; }
        public bool TargetCurrentTarget { get; }

        /// <param name="intervalSeconds">Zero means the cast fires once per phase.</param>
        public ProvisionalCast(uint spellId, double firstDelaySeconds, double intervalSeconds, bool targetCurrentTarget = false)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(firstDelaySeconds);
            ArgumentOutOfRangeException.ThrowIfNegative(intervalSeconds);

            SpellId             = spellId;
            FirstDelaySeconds   = firstDelaySeconds;
            IntervalSeconds     = intervalSeconds;
            TargetCurrentTarget = targetCurrentTarget;
        }
    }

    /// <summary>
    /// One phase of a provisional plan: entered when health reaches <see cref="HealthFraction"/>
    /// (null for the opening phase) and replacing the previous phase's casts.
    /// <see cref="Source"/> records which observation, if any, the phase rests on.
    /// </summary>
    public sealed record ProvisionalPhase(float? HealthFraction, IReadOnlyList<ProvisionalCast> Casts, string Source);

    public sealed class ProvisionalEncounterPlan
    {
        public IReadOnlyList<ProvisionalPhase> Phases { get; }

        public ProvisionalEncounterPlan(IReadOnlyList<ProvisionalPhase> phases)
        {
            ArgumentNullException.ThrowIfNull(phases);

            if (phases.Count == 0)
                throw new ArgumentException("A plan needs at least one phase.", nameof(phases));

            if (phases[0].HealthFraction != null)
                throw new ArgumentException("The first phase must be the opening phase without a health fraction.", nameof(phases));

            float previous = 1f;
            for (int i = 1; i < phases.Count; i++)
            {
                float? fraction = phases[i].HealthFraction;
                if (fraction == null || !(fraction > 0f && fraction < previous))
                    throw new ArgumentException("Later phases need strictly descending health fractions between 0 and 1.", nameof(phases));

                previous = fraction.Value;
            }

            Phases = phases;
        }
    }

    /// <summary>
    /// Runs a <see cref="ProvisionalEncounterPlan"/>: reports which casts are due. Disabled by default;
    /// it neither casts nor advances while disabled or out of combat. Provisional encounter behaviour is
    /// never retail parity.
    /// </summary>
    public sealed class ProvisionalEncounterRunner
    {
        /// <summary>
        /// Off by default; the plan is inert until a caller turns it on.
        /// </summary>
        public bool Enabled { get; set; }

        public int PhaseIndex { get; private set; }

        private readonly ProvisionalEncounterPlan plan;
        private readonly EncounterHealthPhases healthPhases;
        private double phaseElapsed;
        private double[] due = [];

        public ProvisionalEncounterRunner(ProvisionalEncounterPlan plan)
        {
            this.plan = plan;
            healthPhases = new EncounterHealthPhases(plan.Phases.Skip(1).Select(p => p.HealthFraction.Value).ToArray());
            ArmPhase(0);
        }

        /// <summary>
        /// Advance by the supplied delta and return the casts that are due.
        /// </summary>
        public IReadOnlyList<ProvisionalCast> Update(double lastTick, uint health, uint maxHealth, bool inCombat)
        {
            if (!Enabled || !inCombat)
                return [];

            IReadOnlyList<int> reached = healthPhases.Update(health, maxHealth);
            if (reached.Count > 0)
            {
                // Indexes follow descending thresholds, so the highest index is the lowest threshold reached.
                int newPhase = reached.Max() + 1;
                if (newPhase > PhaseIndex)
                    ArmPhase(newPhase);
            }

            phaseElapsed += lastTick;

            IReadOnlyList<ProvisionalCast> casts = plan.Phases[PhaseIndex].Casts;
            List<ProvisionalCast> result = [];
            for (int i = 0; i < casts.Count; i++)
            {
                if (double.IsPositiveInfinity(due[i]) || phaseElapsed < due[i])
                    continue;

                result.Add(casts[i]);

                if (casts[i].IntervalSeconds <= 0d)
                {
                    due[i] = double.PositiveInfinity;
                    continue;
                }

                due[i] += casts[i].IntervalSeconds;

                // A very large tick must not queue a burst: the next cast is one interval after now at the earliest.
                if (due[i] <= phaseElapsed)
                    due[i] = phaseElapsed + casts[i].IntervalSeconds;
            }

            return result;
        }

        /// <summary>
        /// Return to the opening phase, for leaving combat or a wipe.
        /// </summary>
        public void Reset()
        {
            healthPhases.Reset();
            ArmPhase(0);
        }

        private void ArmPhase(int index)
        {
            PhaseIndex   = index;
            phaseElapsed = 0d;
            due          = plan.Phases[index].Casts.Select(c => c.FirstDelaySeconds).ToArray();
        }
    }
}
