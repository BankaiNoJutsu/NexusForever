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
    /// Events with only partial evidence (e.g. the high-value unsupported Spellslinger
    /// event 76) are intentionally absent. Local Spell4 name clusters now back the
    /// receive-damage variants 17/18/19 as the client's Melee/Ranged/Magic buckets, so
    /// runtime damage calculation can emit those events from <see cref="Spell4BaseEntry.School"/>.
    /// No distinct Unarmed variant has been identified yet, so unarmed hits remain on the
    /// generic receive-damage event 16.
    /// </para>
    /// </remarks>
    public static class ProcTriggerEventCandidate
    {
        // Evidence: Spell4=7116 Momentum on-kill, Spell4=4876 Readiness enter-combat,
        //           Spell4=4046 Brutal deal-damage, Spell4=1024 Confrontation receive-damage,
        //           Spell4=8886 On Get Damaged Melee..., Spell4=8909 On Get Damaged Ranged...,
        //           Spell4=8857 On Get Damaged Magic..., Spell4=35054 Charge Builder heal-other.
        public const uint KillTarget          = 1u;
        public const uint EnterCombat         = 6u;
        public const uint ActionCastAny       = 10u;
        public const uint DealDamage          = 12u;
        public const uint ReceiveDamage       = 16u;
        public const uint ReceiveDamageMelee  = 17u;
        public const uint ReceiveDamageRanged = 18u;
        public const uint ReceiveDamageMagic  = 19u;
        public const uint HealOther           = 20u;

        // Evidence (names and data, no native consumer): the 31 Proc rows with event 87 belong to spells that react to a class resource level:
        //           Spell4=59159 Warrior Full Defense ("While above 500 Kinetic Energy gain a Defense"), 59045 Kinetic Fury, 59137 Kinetic Buffer,
        //           84001 Engineer Volatile Injection, and the ON auras of the two Warrior toggles (79790 Power Link, 79753 Augmented Blade), whose
        //           proc casts the toggle's "Spell Remove" under the prerequisite "Kinetic Energy < 250". Inferred. No retail parity claim.
        public const uint ResourceChanged     = 87u;

        public static bool TryGetReceiveDamageSchoolTriggerEvent(SpellSchool school, out uint triggerEvent)
        {
            triggerEvent = school switch
            {
                SpellSchool.Melee => ReceiveDamageMelee,
                SpellSchool.Ranged => ReceiveDamageRanged,
                SpellSchool.Spell => ReceiveDamageMagic,
                _ => 0u
            };

            return triggerEvent != 0u;
        }

        public static bool IsConservativelySupported(uint triggerEvent)
        {
            return TryGetConservativeLabel(triggerEvent, out _);
        }

        public static bool TryGetConservativeLabel(uint triggerEvent, out string label)
        {
            label = triggerEvent switch
            {
                KillTarget          => "kill-target",
                EnterCombat         => "enter-combat",
                ActionCastAny       => "action-cast-any",
                DealDamage          => "deal-damage",
                ReceiveDamage       => "receive-damage",
                ReceiveDamageMelee  => "receive-damage-melee",
                ReceiveDamageRanged => "receive-damage-ranged",
                ReceiveDamageMagic  => "receive-damage-magic",
                HealOther           => "heal-other",
                ResourceChanged     => "resource-changed",
                _                   => null
            };

            return label != null;
        }
    }
}
