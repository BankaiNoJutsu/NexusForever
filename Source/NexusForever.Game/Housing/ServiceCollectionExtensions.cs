using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Housing;

namespace NexusForever.Game.Housing
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameHousing(this IServiceCollection sc)
        {
            sc.AddSingleton<IGlobalResidenceManager, GlobalResidenceManager>();
        }
    }
}
