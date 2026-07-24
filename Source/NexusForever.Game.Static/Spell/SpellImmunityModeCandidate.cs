namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Spell-immunity modes whose runtime matching behavior is backed by current table evidence.
    /// </summary>
    public static class SpellImmunityModeCandidate
    {
        /// <summary>
        /// Match a concrete <c>Spell4</c> id carried by <c>SpellImmunity.DataBits01</c>.
        /// </summary>
        public const uint ConcreteSpell = 0u;

        public static bool IsConservativelySupported(uint mode)
        {
            return mode == ConcreteSpell;
        }
    }
}
