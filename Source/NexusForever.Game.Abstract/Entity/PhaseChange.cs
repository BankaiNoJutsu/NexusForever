namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// One <c>ChangePhase</c> spell effect applied to a player: phase bits added to and cleared from the two words of <c>ServerPhaseVisibilityWorldLocation</c> (0x0171).
    /// </summary>
    /// <remarks>
    /// The effect has no client handler; the field meaning is inferred from the shipped rows (Decomp/Analysis/SPELL_EFFECT_CHANGEPHASE_20261108.md).
    /// </remarks>
    /// <param name="EffectId">Concrete spell effect instance that owns the change.</param>
    /// <param name="AddIPerceive">DataBits00, bits added to <c>PhasesIPerceive</c>.</param>
    /// <param name="AddPerceiveMe">DataBits01, bits added to <c>PhasesThatPerceiveMe</c>.</param>
    /// <param name="ClearIPerceive">DataBits02, bits cleared from <c>PhasesIPerceive</c>.</param>
    /// <param name="ClearPerceiveMe">DataBits03, bits cleared from <c>PhasesThatPerceiveMe</c>.</param>
    public readonly record struct PhaseChange(
        uint EffectId,
        uint Spell4Id,
        uint CastingId,
        uint AddIPerceive,
        uint AddPerceiveMe,
        uint ClearIPerceive,
        uint ClearPerceiveMe);
}
