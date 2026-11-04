using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// Identifies one installed property effect, even if its numeric key is reused.
    /// </summary>
    public interface ISpellPropertyInstance
    {
        Property Property { get; }
        uint EffectId { get; }
    }
}
