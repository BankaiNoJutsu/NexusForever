using System.Text.RegularExpressions;
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

    [Fact]
    public void RuntimeWorldSeedDoesNotCreateRuntimeSchemaOrReadDevelopmentTables()
    {
        string seedSql = File.ReadAllText(GetRepoPath("Tools", "DataMapping", "sql", "runtime_world_seed.sql"));

        Assert.DoesNotMatch(new Regex(@"(?im)^\s*CREATE\s+TABLE\s+(?!tmp_)", RegexOptions.CultureInvariant), seedSql);
        Assert.DoesNotMatch(new Regex(@"(?im)\b(FROM|JOIN)\s+(nf_map_|jabbithole\.|wildstar_client\.)", RegexOptions.CultureInvariant), seedSql);
        Assert.DoesNotMatch(new Regex(@"(?im)\b(INTO|UPDATE|DELETE\s+FROM)\s+nf_map_", RegexOptions.CultureInvariant), seedSql);
    }

    [Fact]
    public void RuntimeVerifierDoesNotQueryDevelopmentTables()
    {
        string verifierSql = File.ReadAllText(GetRepoPath("Tools", "DataMapping", "sql", "verify_safe_world_imports.sql"));

        Assert.DoesNotMatch(new Regex(@"(?im)\b(FROM|JOIN|LEFT\s+JOIN)\s+(nf_map_|jabbithole\.|wildstar_client\.)", RegexOptions.CultureInvariant), verifierSql);
    }

    [Fact]
    public void AuthoringImportReadsStagingFromMappingDatabase()
    {
        string applySql = File.ReadAllText(GetRepoPath("Tools", "DataMapping", "sql", "apply_safe_world_imports_from_staging.sql"));

        Assert.DoesNotMatch(new Regex(@"(?im)^\s*(FROM|JOIN|LEFT\s+JOIN)\s+nf_map_", RegexOptions.CultureInvariant), applySql);
        Assert.Contains("nexus_forever_mapping.nf_map_creature", applySql, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupReferenceImportsRequireDataMappingAuthoringOptIn()
    {
        string setupScript = File.ReadAllText(GetRepoPath("Tools", "Setup", "Initialize-NexusForever.ps1"));

        Assert.Contains("[switch] $EnableDataMappingAuthoring", setupScript, StringComparison.Ordinal);
        Assert.Contains("if ($EnableDataMappingAuthoring -and !$SkipLargeDumpImports)", setupScript, StringComparison.Ordinal);
    }

    private static string GetRepoPath(params string[] segments)
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find repository path: {Path.Combine(segments)}");
    }
}
