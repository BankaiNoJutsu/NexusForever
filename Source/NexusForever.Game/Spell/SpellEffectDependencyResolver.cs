using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Shared;

namespace NexusForever.Game.Spell
{
    internal sealed class SpellEffectDependencyResolver : ISpellEffectDependencyResolver
    {
        private readonly IServiceProvider serviceProvider;

        public SpellEffectDependencyResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IDamageCalculator CreateDamageCalculator()
        {
            IFactory<IDamageCalculator> factory = serviceProvider.GetRequiredService<IFactory<IDamageCalculator>>();
            return factory.Resolve();
        }

        public IEntityFactory GetEntityFactory()
        {
            return serviceProvider.GetService<IEntityFactory>();
        }

        public IForcedMovementGenerator GetForcedMovementGenerator()
        {
            return serviceProvider.GetService<IForcedMovementGenerator>();
        }
    }
}
