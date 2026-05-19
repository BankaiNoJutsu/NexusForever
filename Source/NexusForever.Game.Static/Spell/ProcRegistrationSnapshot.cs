namespace NexusForever.Game.Static.Spell
{
    public readonly record struct ProcRegistrationSnapshot(
        uint EffectId,
        uint Spell4Id,
        uint CastingId,
        uint TriggerEvent,
        uint TriggerSpell4Id,
        float Chance,
        uint TargetData,
        uint CooldownMsOrSentinel,
        double CooldownRemainingSeconds,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);
}
