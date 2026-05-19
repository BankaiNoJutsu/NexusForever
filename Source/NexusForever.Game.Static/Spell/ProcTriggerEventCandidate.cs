namespace NexusForever.Game.Static.Spell
{
    /// <summary>
    /// Dispatched trigger event constants for <see cref="SpellEffectType.Proc"/> DataBits00.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each constant is a DataBits00 value that has been verified against the imported
    /// Spell4Effects dataset and is backed by a live probe hook in UnitEntity.  Any value
    /// present here is eligible for conservative dispatch by
    /// <see cref="ProcDispatchEvidenceBoundary"/>.
    /// </para>
    /// <para>
    /// Events with only partial evidence (e.g. school-specific receive-damage variants
    /// 17/18/19, or the high-value unsupported Spellslinger event 76) are intentionally
    /// absent.  They require either a dedicated probe-hook that emits the specific event
    /// number or a confirmed school-to-event mapping before they can be added here.
    /// </para>
    /// </remarks>
    public static class ProcTriggerEventCandidate
    {
        // Evidence: Spell4=7116 Momentum on-kill, Spell4=4876 Readiness enter-combat,
        //           Spell4=4046 Brutal deal-damage, Spell4=1024 Confrontation receive-damage,
        //           Spell4=35054 Charge Builder heal-other.
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
                KillTarget    => "kill-target",
                EnterCombat   => "enter-combat",
                ActionCastAny => "action-cast-any",
                DealDamage    => "deal-damage",
                ReceiveDamage => "receive-damage",
                HealOther     => "heal-other",
                _             => null
            };

            return label != null;
        }
    }
}
