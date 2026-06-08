using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Database
{
    public static class Extensions
    {
        public const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string SqliteMigrationsAssembly = "NexusForever.Database.Sqlite.Migrations";

        public static DbContextOptionsBuilder UseConfiguration(this DbContextOptionsBuilder optionsBuilder, IConnectionString connectionString)
        {
            switch (connectionString.Provider)
            {
                case DatabaseProvider.MySql:
                    optionsBuilder.UseMySql(connectionString.ConnectionString, ServerVersion.AutoDetect(connectionString.ConnectionString), b =>
                    {
                        b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                    break;
                case DatabaseProvider.Sqlite:
                    optionsBuilder.UseSqlite(connectionString.ConnectionString, b =>
                    {
                        b.MigrationsAssembly(SqliteMigrationsAssembly);
                    });
                    optionsBuilder.AddInterceptors(SqliteConnectionInterceptor.Instance);
                    break;
                default:
                    throw new NotSupportedException($"The requested database provider: {connectionString.Provider:G} is not supported.");
            }
            return optionsBuilder;
        }

        private sealed class SqliteConnectionInterceptor : DbConnectionInterceptor
        {
            public static SqliteConnectionInterceptor Instance { get; } = new();

            public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
            {
                Configure(connection);
            }

            public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
            {
                await ConfigureAsync(connection, cancellationToken);
            }

            private static void Configure(DbConnection connection)
            {
                if (connection is not SqliteConnection sqliteConnection)
                    return;

                using SqliteCommand command = sqliteConnection.CreateCommand();
                command.CommandText = """
                    PRAGMA foreign_keys = ON;
                    PRAGMA busy_timeout = 5000;
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    """;
                command.ExecuteNonQuery();
            }

            private static async Task ConfigureAsync(DbConnection connection, CancellationToken cancellationToken)
            {
                if (connection is not SqliteConnection sqliteConnection)
                    return;

                await using SqliteCommand command = sqliteConnection.CreateCommand();
                command.CommandText = """
                    PRAGMA foreign_keys = ON;
                    PRAGMA busy_timeout = 5000;
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
