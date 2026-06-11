using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Server;

namespace NexusForever.Game.Server
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameServer(this IServiceCollection sc)
        {
            sc.AddSingleton<IServerManager, ServerManager>();
        }
    }
}
