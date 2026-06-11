using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Map.Lock;
using NexusForever.Shared;

namespace NexusForever.Game.Map
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameMap(this IServiceCollection sc)
        {
            sc.AddGameMapInstance();
            sc.AddGameMapLock();

            sc.AddTransient<IPublicEventManager, PublicEventManager>();

            sc.AddSingleton<IEntityCacheManager, EntityCacheManager>();
            sc.AddSingleton<IMapIOManager, MapIOManager>();
            sc.AddSingleton<IMapManager, MapManager>();

            sc.AddTransient<IMapFactory, MapFactory>();
            sc.AddSingleton<IFactoryInterface<IMap>, FactoryInterface<IMap>>();
            sc.AddTransient<IBaseMap, BaseMap>();
        }
    }
}
