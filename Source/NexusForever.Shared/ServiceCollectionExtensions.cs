using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Shared.Configuration;

namespace NexusForever.Shared
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSharedConfiguration<TConfiguration>(this IServiceCollection sc, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            sc.AddSingleton<ISharedConfiguration>(_ =>
            {
                var sharedConfiguration = new SharedConfiguration(configuration);
                sharedConfiguration.Initialise<TConfiguration>();
                return sharedConfiguration;
            });
        }

        public static void AddTransientFactory<TInterface, TImplementation>(this IServiceCollection sc)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            sc.AddTransient<TInterface, TImplementation>();
            sc.AddSingleton<IFactory<TInterface>, Factory<TInterface>>();
        }

        public static void AddShared(this IServiceCollection sc)
        {
            sc.AddSingleton<IWorldManager, WorldManager>();
        }
    }
}
