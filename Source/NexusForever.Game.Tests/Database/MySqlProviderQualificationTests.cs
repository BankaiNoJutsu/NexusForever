using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using NexusForever.Database.Character;

namespace NexusForever.Game.Tests.Database;

/// <summary>
/// Opt-in provider qualification. Every test class in this assembly that already accepts a single
/// <see cref="DbContextOptions{CharacterContext}"/> constructor argument is driven against a fresh disposable MySQL schema per case
/// (created and dropped here). Enable with NEXUS_FOREVER_TEST_MYSQL_SERVER set to a server connection string without a database,
/// for example "server=127.0.0.1;port=3306;user=...;password=...". Without it every case is skipped, so default runs are unchanged.
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

    [RequiresMySqlTheory]
    [MemberData(nameof(Classes))]
    public async Task ClassHoldsOnMySql(string className)
    {
        string server = Environment.GetEnvironmentVariable(ServerVariable)!;
        Type type = DiscoverClasses().Single(candidate => candidate.FullName == className);
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
                    await Execute(server, $"CREATE DATABASE `{schema}`");
                    var builder = new DbContextOptionsBuilder<CharacterContext>()
                        .UseMySql($"{server};database={schema};Pooling=False", new MySqlServerVersion(new Version(8, 0, 0)));
                    // These fixtures are documented as synthetic and run SQLite with foreign keys disabled; mirror that per session.
                    if (ForeignKeysDisabledFixtures.Contains(type.Name)) builder.AddInterceptors(new DisableForeignKeyChecks());
                    var options = builder.Options;
                    // Some fixtures expect the caller to supply the schema when given provider options; EnsureCreated is a no-op
                    // for fixtures that create it themselves afterwards.
                    await using (var schemaContext = new CharacterContext(options)) await schemaContext.Database.EnsureCreatedAsync();
                    object instance = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [options], null)!;
                    if (method.Invoke(instance, arguments) is Task task) await task;
                }
                catch (Exception error)
                {
                    Exception root = error is TargetInvocationException { InnerException: { } inner } ? inner : error;
                    Exception leaf = root;
                    while (leaf.InnerException != null) leaf = leaf.InnerException;
                    string rootText = root.Message.Split(Environment.NewLine)[0];
                    string leafText = leaf.Message.Split(Environment.NewLine)[0];
                    failures.Add($"{label}: {root.GetType().Name}: {rootText}"
                        + (ReferenceEquals(leaf, root) ? "" : $" <- {leaf.GetType().Name}: {leafText}"));
                }
                finally
                {
                    await Execute(server, $"DROP DATABASE IF EXISTS `{schema}`");
                    await Execute(server, $"DROP DATABASE IF EXISTS `{schema}_foreign`");
                }
            }
        }

        Assert.True(executed > 0, $"No cases were discovered in {className}.");
        Assert.True(failures.Count == 0, $"{failures.Count} of {executed} cases failed on MySQL in {className}:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    // Optional comma-separated class-name substrings (NEXUS_FOREVER_TEST_MYSQL_CLASSES) to rerun a subset.
    private static bool MatchesClassFilter(Type type)
    {
        string? filter = Environment.GetEnvironmentVariable("NEXUS_FOREVER_TEST_MYSQL_CLASSES");
        return string.IsNullOrWhiteSpace(filter)
            || filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(part => type.FullName!.Contains(part, StringComparison.Ordinal));
    }

    private static IEnumerable<Type> DiscoverClasses() => typeof(MySqlProviderQualificationTests).Assembly.GetTypes()
        .Where(MatchesClassFilter)
        .Where(type => type.IsClass && !type.IsAbstract && type.IsPublic && type != typeof(MySqlProviderQualificationTests)
            && type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(DbContextOptions<CharacterContext>)], null) != null
            && type.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(type => type.FullName, StringComparer.Ordinal);

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
