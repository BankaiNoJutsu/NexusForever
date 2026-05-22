namespace NexusForever.Game.Static.Spell
{
    public readonly record struct SummonTrapEvidenceBoundarySnapshot(
        bool CreatureIdPresent,
        bool TriggerSpellResolvable,
        string BlockedReason)
    {
        public bool IsConservativelyCreateSupported => BlockedReason == null;
    }

    /// <summary>
    /// Conservative create boundary for <see cref="SpellEffectType.SummonTrap"/> rows.
    /// </summary>
    /// <remarks>
    /// Witness <c>Spell4=34094</c> (Stalker Proximity Mine) encodes creature <c>26850</c>,
    /// trigger <c>34095</c>, arm <c>2000</c> ms, and radius <c>3.0</c>.
    /// Trigger firing and owner AI remain blocked until a separate witness proves them.
    /// </remarks>
    public static class SummonTrapEvidenceBoundary
    {
        public static SummonTrapEvidenceBoundarySnapshot Describe(
            uint creatureId,
            uint triggerSpell4Id,
            bool creatureExists,
            bool triggerSpellExists)
        {
            if (creatureId == 0u)
            {
                return new SummonTrapEvidenceBoundarySnapshot(
                    false,
                    triggerSpell4Id == 0u || triggerSpellExists,
                    "missing-creature-id");
            }

            if (!creatureExists)
            {
                return new SummonTrapEvidenceBoundarySnapshot(
                    true,
                    triggerSpell4Id == 0u || triggerSpellExists,
                    "unknown-creature-id");
            }

            if (triggerSpell4Id != 0u && !triggerSpellExists)
            {
                return new SummonTrapEvidenceBoundarySnapshot(
                    true,
                    false,
                    "unknown-trigger-spell4");
            }

            return new SummonTrapEvidenceBoundarySnapshot(
                true,
                true,
                null);
        }
    }
}
