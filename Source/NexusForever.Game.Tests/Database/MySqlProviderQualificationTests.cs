using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Game.Tests.Database;

/// <summary>
/// Opt-in provider qualification. Every test class in this assembly that already accepts a
/// <see cref="DbContextOptions{CharacterContext}"/> constructor argument (and the three classes that also accept an Auth database) is
/// driven against fresh disposable MySQL schemas per case, created and dropped here. Enable with NEXUS_FOREVER_TEST_MYSQL_SERVER set
/// to a server connection string without a database, for example "server=127.0.0.1;port=3306;user=...;password=...".
/// NEXUS_FOREVER_TEST_MYSQL_CLASSES optionally restricts the run to class-name substrings. Without the server variable every case is
/// skipped, so default runs are unchanged.
/// </summary>
public sealed class MySqlProviderQualificationTests
{
    private const string ServerVariable = "NEXUS_FOREVER_TEST_MYSQL_SERVER";

    public sealed class RequiresMySqlTheoryAttribute : TheoryAttribute
    {
        public RequiresMySqlTheoryAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ServerVariable)))
                Skip = $"Set {ServerVariable} to run the MySQL provider qualification.";
        }
    }

    public static IEnumerable<object[]> Classes() => DiscoverClasses().Select(type => new object[] { type.FullName! });

    public static IEnumerable<object[]> TwoDatabaseClasses() => DiscoverTwoDatabaseClasses().Select(type => new object[] { type.FullName! });

    [RequiresMySqlTheory]
    [MemberData(nameof(Classes))]
    public async Task ClassHoldsOnMySql(string className)
    {
        Type type = DiscoverClasses().Single(candidate => candidate.FullName == className);
        await RunClass(type, async (server, schema) =>
        {
            await Execute(server, $"CREATE DATABASE `{schema}`");
            CharacterOptions options = CreateCharacterOptions(server, schema, type);
            await using (var schemaContext = new CharacterContext(options.Options)) await schemaContext.Database.EnsureCreatedAsync();
            return [options.Options];
        });
    }

    /// <summary>The Auth-aware constructors: a Character schema plus a separate Auth schema (and a foreign Auth schema where asked for).</summary>
    [RequiresMySqlTheory]
    [MemberData(nameof(TwoDatabaseClasses))]
    public async Task TwoDatabaseClassHoldsOnMySql(string className)
    {
        Type type = DiscoverTwoDatabaseClasses().Single(candidate => candidate.FullName == className);
        await RunClass(type, async (server, schema) =>
        {
            string authSchema = schema + "_auth", foreignAuthSchema = schema + "_fauth";
            await Execute(server, $"CREATE DATABASE `{schema}`");
            await Execute(server, $"CREATE DATABASE `{authSchema}`");
            await Execute(server, $"CREATE DATABASE `{foreignAuthSchema}`");
            CharacterOptions character = CreateCharacterOptions(server, schema, type);
            var authOptions = MySqlBuilder<AuthContext>(server, authSchema).Options;
            await using (var characterContext = new CharacterContext(character.Options)) await characterContext.Database.EnsureCreatedAsync();
            await using (var authContext = new AuthContext(authOptions)) await authContext.Database.EnsureCreatedAsync();
            DatabaseConnectionString Connection(string database) => new()
            {
                Provider = DatabaseProvider.MySql,
                ConnectionString = $"{server};database={database};Pooling=False"
            };
            return type.Name switch
            {
                "CraftingSettlementTests" => [character.Options, Connection(authSchema), Connection(foreignAuthSchema)],
                "ContractClaimTransactionTests" => [character.Options, authOptions],
                "BankDeferredCurrencyRecoveryTests" => [character.Options, Connection(authSchema)],
                _ => throw new InvalidOperationException($"No two-database constructor mapping for {type.Name}.")
            };
        });
    }

    private static async Task RunClass(Type type, Func<string, string, Task<object[]>> buildConstructorArguments)
    {
        string server = Environment.GetEnvironmentVariable(ServerVariable)!;
        var failures = new List<string>();
        int executed = 0;

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            bool theory = method.GetCustomAttribute<TheoryAttribute>() != null;
            bool fact = !theory && method.GetCustomAttribute<FactAttribute>() != null;
            if (!theory && !fact) continue;
            object?[][] cases = fact ? [[]] : method.GetCustomAttributes<InlineDataAttribute>().Select(data => data.GetData(method).First()).ToArray();
            ParameterInfo[] parameters = method.GetParameters();
            foreach (object?[] provided in cases)
            {
                // Theory data may omit trailing optional parameters; fill them with their declared defaults.
                object?[] arguments = provided.Length >= parameters.Length ? provided
                    : provided.Concat(parameters.Skip(provided.Length).Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null)).ToArray();
                executed++;
                // HousingRemodelPublicationTests only creates its secondary "foreign" schema for primary schemas named nf_remodel_*.
                string schema = (type.Name == "HousingRemodelPublicationTests" ? "nf_remodel_qual_" : "nexus_qual_") + Guid.NewGuid().ToString("N")[..12];
                string label = $"{method.Name}({string.Join(", ", arguments)})";
                try
                {
                    object[] constructorArguments = await buildConstructorArguments(server, schema);
                    object instance = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, constructorArguments, null)!;
                    if (method.Invoke(instance, arguments) is Task task) await task;
                }
                catch (Exception error)
                {
                    Exception root = error is TargetInvocationException { InnerException: { } inner } ? inner : error;
                    Exception leaf = root;
                    while (leaf.InnerException != null) leaf = leaf.InnerException;
                    string rootText = root.Message.Split(Environment.NewLine)[0];
                    string leafText = leaf.Message.Split(Environment.NewLine)[0];
                    // First frame inside this test assembly's own files (other than this driver) shows where an assertion failed.
                    string? site = root.StackTrace?.Split('\n')
                        .FirstOrDefault(frame => frame.Contains(".Tests") && !frame.Contains(nameof(MySqlProviderQualificationTests)))?.Trim();
                    failures.Add($"{label}: {root.GetType().Name}: {rootText}"
                        + (ReferenceEquals(leaf, root) ? "" : $" <- {leaf.GetType().Name}: {leafText}")
                        + (site == null ? "" : $" @ {ShortSite(site)}"));
                }
                finally
                {
                    foreach (string suffix in new[] { "", "_foreign", "_auth", "_fauth" })
                        await Execute(server, $"DROP DATABASE IF EXISTS `{schema}{suffix}`");
                }
            }
        }

        Assert.True(executed > 0, $"No cases were discovered in {type.FullName}.");
        Assert.True(failures.Count == 0, $"{failures.Count} of {executed} cases failed on MySQL in {type.FullName}:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    // "at Ns.Class.Method(args) in <path>\File.cs:line N" -> "Class.Method File.cs:line N"
    private static string ShortSite(string frame)
    {
        int inIndex = frame.IndexOf(" in ", StringComparison.Ordinal);
        string method = inIndex < 0 ? frame : frame[..inIndex];
        string location = inIndex < 0 ? "" : frame[(inIndex + 4)..];
        string file = location.Contains('\\') ? location[(location.LastIndexOf('\\') + 1)..] : location;
        int paren = method.IndexOf('(');
        if (paren > 0) method = method[..paren];
        return $"{method.Replace("at NexusForever.Game.Tests.", "").Trim()} {file}";
    }

    private readonly record struct CharacterOptions(DbContextOptions<CharacterContext> Options);

    // NEXUS_FOREVER_TEST_MYSQL_PRODUCTION_OPTIONS=1 builds every context through the repository's own UseConfiguration (retry strategy
    // EnableRetryOnFailure(5, 10s) and the string concurrency SQL generator) instead of plain UseMySql.
    private static bool UseProductionOptions => Environment.GetEnvironmentVariable("NEXUS_FOREVER_TEST_MYSQL_PRODUCTION_OPTIONS") == "1";

    private static DbContextOptionsBuilder<TContext> MySqlBuilder<TContext>(string server, string database) where TContext : DbContext
    {
        string connection = $"{server};database={database};Pooling=False";
        var builder = new DbContextOptionsBuilder<TContext>();
        if (UseProductionOptions)
            builder.UseConfiguration(new DatabaseConnectionString { Provider = DatabaseProvider.MySql, ConnectionString = connection });
        else
            builder.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 0)));
        return builder;
    }

    private static CharacterOptions CreateCharacterOptions(string server, string schema, Type type)
    {
        var builder = MySqlBuilder<CharacterContext>(server, schema);
        // These fixtures are documented as synthetic and run SQLite with foreign keys disabled; mirror that per session.
        if (ForeignKeysDisabledFixtures.Contains(type.Name)) builder.AddInterceptors(new DisableForeignKeyChecks());
        return new CharacterOptions(builder.Options);
    }

    private static bool MatchesClassFilter(Type type)
    {
        string? filter = Environment.GetEnvironmentVariable("NEXUS_FOREVER_TEST_MYSQL_CLASSES");
        return string.IsNullOrWhiteSpace(filter)
            || filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(part => type.FullName!.Contains(part, StringComparison.Ordinal));
    }

    private static IEnumerable<Type> PublicTestClasses() => typeof(MySqlProviderQualificationTests).Assembly.GetTypes()
        .Where(MatchesClassFilter)
        .Where(type => type.IsClass && !type.IsAbstract && type.IsPublic && type != typeof(MySqlProviderQualificationTests)
            && type.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(type => type.FullName, StringComparer.Ordinal);

    private static IEnumerable<Type> DiscoverClasses() => PublicTestClasses().Where(type =>
        type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(DbContextOptions<CharacterContext>)], null) != null);

    private static readonly string[] TwoDatabaseClassNames =
        ["BankDeferredCurrencyRecoveryTests", "ContractClaimTransactionTests", "CraftingSettlementTests"];

    private static IEnumerable<Type> DiscoverTwoDatabaseClasses() => PublicTestClasses().Where(type => TwoDatabaseClassNames.Contains(type.Name));

    private static readonly HashSet<string> ForeignKeysDisabledFixtures = ["CharacterItemDurabilityTests"];

    private sealed class DisableForeignKeyChecks : DbConnectionInterceptor
    {
        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SET SESSION foreign_key_checks = 0";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "SET SESSION foreign_key_checks = 0";
            command.ExecuteNonQuery();
        }
    }

    private static async Task Execute(string server, string sql)
    {
        await using var connection = new MySqlConnection(server + ";Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
