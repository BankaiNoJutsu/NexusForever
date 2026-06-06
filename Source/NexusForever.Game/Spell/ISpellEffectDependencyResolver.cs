using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;

namespace NexusForever.Game.Spell
{
    internal interface ISpellEffectDependencyResolver
    {
        IDamageCalculator CreateDamageCalculator();
        IEntityFactory GetEntityFactory();
        IForcedMovementGenerator GetForcedMovementGenerator();
    }
}
