using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusForever.Aspire.Database.Migrations.Configuration.Model;
using NexusForever.Aspire.Database.Migrations.Service;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Chat;
using NexusForever.Database.Configuration.Model;
using NexusForever.Database.Friendship;
using NexusForever.Database.Group;
using NexusForever.Database.World;
using NLog.Extensions.Logging;

namespace NexusForever.Aspire.Database.Migrations
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var builder = new HostBuilder()
                .ConfigureAppConfiguration(cb =>
                {
                    cb.SetBasePath(basePath)
                        .AddJsonFile("AspireMigrations.json", false)
                        .AddEnvironmentVariables();
                })
                .ConfigureLogging(l =>
                {
                    l.ClearProviders();
                    l.AddNLog();
                })
                .ConfigureServices((hb, sc) =>
                {
                    sc.AddOptions<DatabaseMigrationOptions>()
                        .Bind(hb.Configuration.GetSection("DatabaseMigration"));

                    sc.AddOptions<AccountCreationOptions>()
                        .Bind(hb.Configuration.GetSection("AccountCreation"));

                    sc.AddOptions<WorldDatabaseOptions>()
                        .Bind(hb.Configuration.GetSection("WorldDatabase"));

                    sc.AddHostedService<DatabaseMigrationHostedService>();
                    sc.AddHostedService<AccountCreationHostedService>();
                    sc.AddHostedService<WorldDatabaseHostedService>();
                    sc.AddHostedService<FinishHostedService>();

                    sc.AddConfiguredDbContext<AuthContext>(hb.Configuration, "Auth", "authdb");
                    sc.AddConfiguredDbContext<CharacterContext>(hb.Configuration, "Character", "characterdb");
                    sc.AddConfiguredDbContext<WorldContext>(hb.Configuration, "World", "worlddb");
                    sc.AddConfiguredDbContext<GroupContext>(hb.Configuration, "Group", "groupdb");
                    sc.AddConfiguredDbContext<ChatContext>(hb.Configuration, "Chat", "chatdb");
                    sc.AddConfiguredDbContext<FriendshipContext>(hb.Configuration, "Friendship", "friendshipdb");
                });

            IHost host = builder.Build();
            await host.RunAsync();
        }
    }

    internal static class ServiceCollectionExtensions
    {
        public static void AddConfiguredDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, string databaseName, string connectionStringName)
            where TContext : DbContext
        {
            services.AddDbContext<TContext>(options =>
            {
                IConnectionString connectionString = GetConnectionString(configuration, databaseName, connectionStringName);
                options.UseConfiguration(connectionString);
            });
        }

        private static IConnectionString GetConnectionString(IConfiguration configuration, string databaseName, string connectionStringName)
        {
            DatabaseConnectionString configuredConnectionString = configuration
                .GetSection($"Database:{databaseName}")
                .Get<DatabaseConnectionString>();

            if (configuredConnectionString != null && !string.IsNullOrWhiteSpace(configuredConnectionString.ConnectionString))
                return configuredConnectionString;

            string legacyConnectionString = configuration.GetConnectionString(connectionStringName);
            if (!string.IsNullOrWhiteSpace(legacyConnectionString))
            {
                return new DatabaseConnectionString
                {
                    Provider         = DatabaseProvider.MySql,
                    ConnectionString = legacyConnectionString
                };
            }

            throw new InvalidOperationException($"Database connection string '{databaseName}' is not configured.");
        }
    }
}
