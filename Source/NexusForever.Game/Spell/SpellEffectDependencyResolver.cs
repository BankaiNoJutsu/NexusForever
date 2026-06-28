using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
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

        public IAssetManager GetAssetManager()
        {
            return serviceProvider.GetService<IAssetManager>();
        }

        public IGlobalAchievementManager GetGlobalAchievementManager()
        {
            return serviceProvider.GetService<IGlobalAchievementManager>();
        }

        public IGlobalResidenceManager GetGlobalResidenceManager()
        {
            return serviceProvider.GetService<IGlobalResidenceManager>();
        }

        public IGlobalLootManager GetGlobalLootManager()
        {
            return serviceProvider.GetService<IGlobalLootManager>();
        }

        public IMapLockManager GetMapLockManager()
        {
            return serviceProvider.GetService<IMapLockManager>();
        }

        public IGlobalSpellManager GetGlobalSpellManager()
        {
            return serviceProvider.GetService<IGlobalSpellManager>();
        }

        public IGameTableManager GetGameTableManager()
        {
            return serviceProvider.GetService<IGameTableManager>();
        }
    }
}
