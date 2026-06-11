using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Reputation;

namespace NexusForever.Game.Reputation
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameReputation(this IServiceCollection sc)
        {
            sc.AddSingleton<IFactionManager, FactionManager>();
        }
    }
}
