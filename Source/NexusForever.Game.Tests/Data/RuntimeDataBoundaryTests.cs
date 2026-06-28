using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.World;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Data;

public class RuntimeDataBoundaryTests
{
    [Fact]
    public void WorldRuntimeModelDoesNotMapDevelopmentReferenceSchemasOrTables()
    {
        DbContextOptions<WorldContext> options = new DbContextOptionsBuilder<WorldContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_world;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using WorldContext context = new(options);

        var entityTypes = context.Model.GetEntityTypes().ToList();
        List<string> tableNames = entityTypes
            .Select(e => e.GetTableName())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        List<string> schemaNames = entityTypes
            .SelectMany(e => new[] { e.GetSchema(), e.GetViewSchema() })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        Assert.DoesNotContain(tableNames, t => t.StartsWith("nf_map_", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tableNames, t => t.Equals("wildstar_client", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tableNames, t => t.Equals("jabbithole", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(schemaNames, s => s.Equals("wildstar_client", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(schemaNames, s => s.Equals("jabbithole", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(schemaNames, s => s.Equals("nexus_forever_mapping", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RuntimeSourceDoesNotQueryDevelopmentReferenceTables()
    {
        string sourceRoot = Path.GetDirectoryName(GetRepoPath("Source", "NexusForever.sln"));
        var scannedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".json",
            ".config",
            ".props",
            ".targets",
            ".sql"
        };
        var forbiddenRuntimeQuery = new Regex(
            @"\b(?:FROM|JOIN|UPDATE|INTO|DELETE\s+FROM|TRUNCATE\s+(?:TABLE\s+)?|CREATE\s+TABLE|ALTER\s+TABLE|DROP\s+TABLE)\s+(?:(?:`?nexus_forever_mapping`?\s*\.\s*)?`?nf_map_[A-Za-z0-9_]*`?|`?jabbithole`?\s*\.|`?wildstar_client`?\s*\.)|\b(?:Database|Initial\s+Catalog)\s*=\s*(?:jabbithole|wildstar_client|nexus_forever_mapping)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        List<string> offenders = EnumerateRuntimeSourceFiles(sourceRoot, scannedExtensions)
            .Select(path => new
            {
                Path = path,
                Match = forbiddenRuntimeQuery.Match(File.ReadAllText(path))
            })
            .Where(result => result.Match.Success)
            .Select(result => $"{Path.GetRelativePath(sourceRoot, result.Path)}: {result.Match.Value.Trim()}")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Runtime source must consume promoted runtime tables and must not query jabbithole, wildstar_client, nexus_forever_mapping, or nf_map_* directly:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
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
    public void RuntimeWorldSeedContainsPromotedQuestMapPlacements()
    {
        string seedSql = File.ReadAllText(GetRepoPath("Tools", "DataMapping", "sql", "runtime_world_seed.sql"));

        foreach (int entityId in Enumerable.Range(1099000001, 53))
            Assert.Contains($"({entityId},", seedSql, StringComparison.Ordinal);

        Assert.Contains("(1099000001,0,17189,51", seedSql, StringComparison.Ordinal);
        Assert.Contains("(1099000004,0,12535,426", seedSql, StringComparison.Ordinal);
        Assert.Contains("(1099000048,8,24286,870", seedSql, StringComparison.Ordinal);
        Assert.Contains("(1099000053,0,31895,870", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeWorldSeedRejectsStaleDodgerCoordinate()
    {
        string seedSql = File.ReadAllText(GetRepoPath("Tools", "DataMapping", "sql", "runtime_world_seed.sql"));

        Assert.Contains("Source coordinate 1556666 is superseded by official/current coordinate 4460.", seedSql, StringComparison.Ordinal);
        Assert.Contains("WHERE id = 1001556666;", seedSql, StringComparison.Ordinal);
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
    public void AuthoringImportRejectsStaleDodgerCoordinate()
    {
        string applySql = File.ReadAllText(GetRepoPath("Tools", "DataMapping", "sql", "apply_safe_world_imports_from_staging.sql"));

        Assert.Contains("tmp_nf_world_entity_coordinate_reject", applySql, StringComparison.Ordinal);
        Assert.Contains("(1556666, 1086, 54640, 51, 23, 'Dodger stale Jabbithole coordinate superseded by official/current coordinate 4460')", applySql, StringComparison.Ordinal);
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

    private static IEnumerable<string> EnumerateRuntimeSourceFiles(string directory, HashSet<string> scannedExtensions)
    {
        foreach (string file in Directory.EnumerateFiles(directory))
        {
            if (scannedExtensions.Contains(Path.GetExtension(file)))
                yield return file;
        }

        foreach (string childDirectory in Directory.EnumerateDirectories(directory))
        {
            string name = Path.GetFileName(childDirectory);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("NexusForever.Game.Tests", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string file in EnumerateRuntimeSourceFiles(childDirectory, scannedExtensions))
                yield return file;
        }
    }
}
