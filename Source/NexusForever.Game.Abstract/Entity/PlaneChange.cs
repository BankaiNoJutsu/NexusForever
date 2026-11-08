namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// One <c>ChangePlane</c> spell effect applied to a player: plane bits added to the two words of <c>ServerPlaneVisibilitySpellSlingerVoid</c> (0x0172).
    /// </summary>
    /// <remarks>
    /// The effect has no client handler; the field meaning is inferred from the shipped rows (Decomp/Analysis/SPELL_EFFECT_CHANGEPLANE_20261108.md).
    /// </remarks>
    /// <param name="EffectId">Concrete spell effect instance that owns the change.</param>
    /// <param name="AddCanSee">DataBits00, bits added to <c>PlanesCanSee</c>, the word prerequisite 232 tests.</param>
    /// <param name="AddCanBeSeen">DataBits01, bits added to <c>PlanesCanBeSeen</c>, which the client stores but never reads.</param>
    public readonly record struct PlaneChange(
        uint EffectId,
        uint Spell4Id,
        uint CastingId,
        uint AddCanSee,
        uint AddCanBeSeen);
}
