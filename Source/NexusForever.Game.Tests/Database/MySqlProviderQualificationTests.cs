using System.Reflection;
using Microsoft.EntityFrameworkCore;
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
            foreach (object?[] arguments in cases)
            {
                executed++;
                string schema = "nexus_qual_" + Guid.NewGuid().ToString("N")[..12];
                string label = $"{method.Name}({string.Join(", ", arguments)})";
                try
                {
                    await Execute(server, $"CREATE DATABASE `{schema}`");
                    var options = new DbContextOptionsBuilder<CharacterContext>()
                        .UseMySql($"{server};database={schema};Pooling=False", new MySqlServerVersion(new Version(8, 0, 0))).Options;
                    object instance = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [options], null)!;
                    if (method.Invoke(instance, arguments) is Task task) await task;
                }
                catch (Exception error)
                {
                    Exception root = error is TargetInvocationException { InnerException: { } inner } ? inner : error;
                    failures.Add($"{label}: {root.GetType().Name}: {root.Message.Split('\n')[0]}");
                }
                finally { await Execute(server, $"DROP DATABASE IF EXISTS `{schema}`"); }
            }
        }

        Assert.True(executed > 0, $"No cases were discovered in {className}.");
        Assert.True(failures.Count == 0, $"{failures.Count} of {executed} cases failed on MySQL in {className}:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static IEnumerable<Type> DiscoverClasses() => typeof(MySqlProviderQualificationTests).Assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && type.IsPublic && type != typeof(MySqlProviderQualificationTests)
            && type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(DbContextOptions<CharacterContext>)], null) != null
            && type.GetConstructor(Type.EmptyTypes) != null)
        .OrderBy(type => type.FullName, StringComparer.Ordinal);

    private static async Task Execute(string server, string sql)
    {
        await using var connection = new MySqlConnection(server + ";Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
