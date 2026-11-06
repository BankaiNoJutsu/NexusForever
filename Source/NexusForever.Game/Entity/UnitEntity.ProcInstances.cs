using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public abstract partial class UnitEntity
    {
        public IReadOnlyCollection<ISpellProcInstance> CaptureSpellProcInstances(uint spell4Id)
            => procStates.Values.Where(state => state.Spell4Id == spell4Id).ToArray();
    }
}
