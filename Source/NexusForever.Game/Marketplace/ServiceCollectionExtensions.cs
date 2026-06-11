using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Marketplace;

namespace NexusForever.Game.Marketplace
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameMarketplace(this IServiceCollection sc)
        {
            sc.AddSingleton<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        }
    }
}
