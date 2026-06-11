using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Achievement;

namespace NexusForever.Game.Achievement
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameAchievement(this IServiceCollection sc)
        {
            sc.AddSingleton<IGlobalAchievementManager, GlobalAchievementManager>();
        }
    }
}
