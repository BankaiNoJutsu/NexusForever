namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Candidate labels for <see cref="SpellEffectType.Proc"/> DataBits00 trigger events.
    /// </summary>
    /// <remarks>
    /// These values anchor the observed proc runtime events emitted by NexusForever.
    /// </remarks>
    public static class ProcTriggerEventCandidate
    {
        public const uint KillTarget    = 1u;
        public const uint EnterCombat   = 6u;
        public const uint ActionCastAny = 10u;
        public const uint DealDamage    = 12u;
        public const uint ReceiveDamage = 16u;
        public const uint HealOther     = 20u;
    }
}
