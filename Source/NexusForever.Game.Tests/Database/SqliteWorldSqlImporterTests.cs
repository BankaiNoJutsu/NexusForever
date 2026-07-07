using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Aspire.Database.Migrations.Service;
using NexusForever.Database.World;

namespace NexusForever.Game.Tests.Database;

public sealed class SqliteWorldSqlImporterTests
{
    [Fact]
    public async Task ImportFileAsync_WithDeleteJoinDeletingSourceAlias_DeletesMatchingRows()
    {
        await using SqliteConnection connection = await CreateOpenConnection();
        await using WorldContext context = CreateContext(connection);
        await CreateLootTables(context);
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO loot_group (id, comment) VALUES
              (100, 'DataMapping creature loot'),
              (200, 'Manual loot');
            INSERT INTO loot_item (id, comment) VALUES
              (100, 'mapped item'),
              (200, 'manual item');
            """);

        await ImportSql(
            context,
            """
            DELETE li
            FROM loot_item li
            JOIN loot_group lg ON lg.id = li.id
            WHERE lg.comment LIKE 'DataMapping %';
            """);

        List<long> remainingIds = await QueryIds(context, "loot_item");
        Assert.Equal([200L], remainingIds);
    }

    [Fact]
    public async Task ImportFileAsync_WithDeleteJoinDeletingJoinedAlias_Throws()
    {
        await using SqliteConnection connection = await CreateOpenConnection();
        await using WorldContext context = CreateContext(connection);
        await CreateLootTables(context);

        await Assert.ThrowsAsync<NotSupportedException>(() => ImportSql(
            context,
            """
            DELETE lg
            FROM loot_item li
            JOIN loot_group lg ON lg.id = li.id
            WHERE lg.comment LIKE 'DataMapping %';
            """));
    }

    private static async Task<SqliteConnection> CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static WorldContext CreateContext(SqliteConnection connection)
    {
        DbContextOptions<WorldContext> options = new DbContextOptionsBuilder<WorldContext>()
            .UseSqlite(connection)
            .Options;

        return new WorldContext(options);
    }

    private static async Task CreateLootTables(WorldContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE loot_group (
                id INTEGER NOT NULL PRIMARY KEY,
                comment TEXT NULL
            );

            CREATE TABLE loot_item (
                id INTEGER NOT NULL PRIMARY KEY,
                comment TEXT NULL
            );
            """);
    }

    private static async Task ImportSql(WorldContext context, string sql)
    {
        string path = Path.Combine(Path.GetTempPath(), $"NexusForeverSqliteImporter_{Guid.NewGuid():N}.sql");
        await File.WriteAllTextAsync(path, sql);

        try
        {
            var importer = new SqliteWorldSqlImporter(NullLogger.Instance, context);
            await importer.ImportFileAsync(path, CancellationToken.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<List<long>> QueryIds(WorldContext context, string table)
    {
        var ids = new List<long>();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT id FROM {table} ORDER BY id";
        if (context.Database.CurrentTransaction != null)
            command.Transaction = context.Database.CurrentTransaction.GetDbTransaction();

        await using DbDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            ids.Add(reader.GetInt64(0));

        return ids;
    }
}
