namespace NexusForever.Script.Main.AI
{
    /// <summary>
    /// Tracks which descending health thresholds an encounter creature has reached.
    /// Each threshold fires once per fight; healing back above it does not re-arm it, and
    /// <see cref="Reset"/> (call on leaving combat or wipe) re-arms all of them.
    /// The class encodes no boss value: callers supply the fractions from agreed evidence.
    /// </summary>
    public sealed class EncounterHealthPhases
    {
        private readonly float[] thresholds;
        private readonly bool[] crossed;

        /// <summary>
        /// Create a tracker for the supplied health fractions, each strictly between 0 and 1.
        /// Indexes returned by <see cref="Update"/> follow descending threshold order.
        /// </summary>
        public EncounterHealthPhases(params float[] fractions)
        {
            ArgumentNullException.ThrowIfNull(fractions);

            foreach (float fraction in fractions)
            {
                if (!(fraction > 0f && fraction < 1f))
                    throw new ArgumentOutOfRangeException(nameof(fractions), fraction, "Thresholds must be strictly between 0 and 1.");
            }

            if (fractions.Distinct().Count() != fractions.Length)
                throw new ArgumentException("Thresholds must be unique.", nameof(fractions));

            thresholds = fractions.OrderByDescending(f => f).ToArray();
            crossed    = new bool[thresholds.Length];
        }

        /// <summary>
        /// Report the thresholds newly reached by the supplied health, highest threshold first.
        /// A threshold is reached when health divided by max health is less than or equal to it.
        /// </summary>
        public IReadOnlyList<int> Update(uint health, uint maxHealth)
        {
            if (maxHealth == 0u)
                return [];

            float fraction = (float)health / maxHealth;

            List<int> reached = [];
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (crossed[i] || fraction > thresholds[i])
                    continue;

                crossed[i] = true;
                reached.Add(i);
            }

            return reached;
        }

        /// <summary>
        /// Return whether the threshold at the supplied descending-order index has fired.
        /// </summary>
        public bool HasCrossed(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, crossed.Length);
            return crossed[index];
        }

        /// <summary>
        /// Re-arm every threshold.
        /// </summary>
        public void Reset()
        {
            Array.Clear(crossed);
        }
    }
}
