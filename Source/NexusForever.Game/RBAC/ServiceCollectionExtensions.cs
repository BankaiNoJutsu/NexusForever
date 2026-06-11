using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.RBAC;

namespace NexusForever.Game.RBAC
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameRbac(this IServiceCollection sc)
        {
            sc.AddSingleton<IRBACManager, RBACManager>();
        }
    }
}
