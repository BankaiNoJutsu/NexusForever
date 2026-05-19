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

        public static bool IsConservativelySupported(uint triggerEvent)
        {
            return TryGetConservativeLabel(triggerEvent, out _);
        }

        public static bool TryGetConservativeLabel(uint triggerEvent, out string label)
        {
            label = triggerEvent switch
            {
                KillTarget    => "kill-target candidate",
                EnterCombat   => "enter-combat candidate",
                ActionCastAny => "action-cast-any candidate",
                DealDamage    => "deal-damage candidate",
                ReceiveDamage => "receive-damage candidate",
                HealOther     => "heal-other candidate",
                _             => null
            };

            return label != null;
        }
    }
}
