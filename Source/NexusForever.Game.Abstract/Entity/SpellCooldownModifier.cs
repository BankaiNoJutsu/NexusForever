namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// One concrete effect73 instance: scope/key and operation/magnitude are the
    /// four original Spell4Effects data words, independent of source spell IDs.
    /// </summary>
    public sealed record SpellCooldownModifier(uint EffectId, uint Spell4Id, uint CastingId,
        uint Scope, uint Key, uint Mode, float Magnitude);
}
