using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Spell
{
    // Only the effect handler enrolls handles actually installed by this cast.
    internal sealed class AugmentationSpellParameters : SpellParameters
    {
        internal List<ISpellProcInstance> ProcInstances { get; } = [];
    }
}
