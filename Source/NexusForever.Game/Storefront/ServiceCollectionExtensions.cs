using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Storefront;

namespace NexusForever.Game.Storefront
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameStore(this IServiceCollection sc)
        {
            sc.AddSingleton<IGlobalStorefrontManager, GlobalStorefrontManager>();
        }
    }
}
