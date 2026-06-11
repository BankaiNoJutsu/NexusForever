using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Text;

namespace NexusForever.GameTable
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameTable(this IServiceCollection sc, IConfigurationSection configuration)
        {
            sc.AddGameTableText();

            sc.AddOptions<GameTableConfig>()
                .Bind(configuration)
                .ValidateOnStart();

            sc.AddSingleton<GameTableManager>();
            sc.AddSingleton<IGameTableManager>(sp => sp.GetRequiredService<GameTableManager>());
        }
    }
}
