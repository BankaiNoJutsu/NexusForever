using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Shared;

namespace NexusForever.Game.Marketplace
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameMarketplace(this IServiceCollection sc)
        {
            sc.AddSingletonLegacy<IGlobalMarketplaceManager, GlobalMarketplaceManager>();
        }
    }
}
