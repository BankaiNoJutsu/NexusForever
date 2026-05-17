using Microsoft.EntityFrameworkCore;
using NexusForever.Database.World;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Data;

public class RuntimeDataBoundaryTests
{
    [Fact]
    public void WorldRuntimeModelDoesNotMapDevelopmentReferenceTables()
    {
        DbContextOptions<WorldContext> options = new DbContextOptionsBuilder<WorldContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_world;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using WorldContext context = new(options);

        List<string> tableNames = context.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        Assert.DoesNotContain(tableNames, t => t.StartsWith("nf_map_", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tableNames, t => t.Equals("wildstar_client", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tableNames, t => t.Equals("jabbithole", StringComparison.OrdinalIgnoreCase));
    }
}
