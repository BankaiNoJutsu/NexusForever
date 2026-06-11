using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Fortune;

namespace NexusForever.Game.Fortune
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameFortune(this IServiceCollection sc)
        {
            sc.AddSingleton<IFortuneRewardPool, FortuneRewardPool>();
        }
    }
}
