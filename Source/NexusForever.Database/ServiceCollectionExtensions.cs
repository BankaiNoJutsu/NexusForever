using Microsoft.Extensions.DependencyInjection;

namespace NexusForever.Database
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDatabase(this IServiceCollection sc)
        {
            sc.AddSingleton<IDatabaseManager, DatabaseManager>();
        }
    }
}
