namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Candidate labels for <see cref="SpellEffectType.Proc"/> DataBits00 trigger events.
    /// </summary>
    /// <remarks>
    /// These values are trace-only evidence anchors until retail/sniff behavior confirms dispatch semantics.
    /// </remarks>
    public static class ProcTriggerEventCandidate
    {
        public const uint KillTarget    = 1u;
        public const uint EnterCombat   = 6u;
        public const uint DealDamage    = 12u;
        public const uint ReceiveDamage = 16u;
        public const uint HealOther     = 20u;
    }
}
