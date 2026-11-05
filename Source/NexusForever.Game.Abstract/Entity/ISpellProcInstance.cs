namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// Identifies one installed proc, even when another cast reuses its effect ID.
    /// </summary>
    public interface ISpellProcInstance
    {
        uint EffectId { get; }
    }
}
