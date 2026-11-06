using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NexusForever.Database.Character;

namespace NexusForever.Game.Tests.Quest;

/// <summary>
/// Opt-in provider qualification: runs every <see cref="QuestPersistenceDurabilityTests"/> case against a fresh disposable MySQL
/// schema. Set NEXUS_FOREVER_TEST_MYSQL_SERVER to a connection string without a database (for example
/// "server=127.0.0.1;port=3306;user=...;password=...") to enable; each case creates and drops its own schema named
/// nexus_qual_*. Skipped when the variable is absent, so default runs and CI are unchanged.
/// </summary>
public sealed class QuestPersistenceDurabilityMySqlTests
{
    private const string ServerVariable = "NEXUS_FOREVER_TEST_MYSQL_SERVER";

    public sealed class RequiresMySqlFactAttribute : FactAttribute
    {
        public RequiresMySqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ServerVariable)))
                Skip = $"Set {ServerVariable} to run the MySQL provider qualification.";
        }
    }

    [RequiresMySqlFact]
    public async Task EveryDurabilityCaseHoldsOnMySql()
    {
        string server = Environment.GetEnvironmentVariable(ServerVariable)!;
        var failures = new List<string>();
        int executed = 0;

        foreach (MethodInfo method in typeof(QuestPersistenceDurabilityTests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            bool isFact = method.GetCustomAttribute<FactAttribute>() != null && method.GetCustomAttribute<TheoryAttribute>() == null;
            object?[][] cases = isFact ? [[]]
                : method.GetCustomAttributes<InlineDataAttribute>().Select(data => data.GetData(method).First()).ToArray();
            foreach (object?[] arguments in cases)
            {
                if (!isFact && method.GetCustomAttribute<TheoryAttribute>() == null) continue;
                string schema = "nexus_qual_" + Guid.NewGuid().ToString("N")[..12];
                string label = $"{method.Name}({string.Join(", ", arguments)})";
                try
                {
                    await Execute(server, $"CREATE DATABASE `{schema}`");
                    var options = new DbContextOptionsBuilder<CharacterContext>()
                        .UseMySql($"{server};database={schema}", new MySqlServerVersion(new Version(8, 0, 0)))
                        .Options;
                    var instance = Activator.CreateInstance(typeof(QuestPersistenceDurabilityTests),
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [options], null)!;
                    object? result = method.Invoke(instance, arguments);
                    if (result is Task task) await task;
                    executed++;
                }
                catch (Exception error)
                {
                    executed++;
                    failures.Add($"{label}: {(error is TargetInvocationException { InnerException: { } inner } ? inner : error).GetType().Name}: "
                        + (error is TargetInvocationException { InnerException: { } inner2 } ? inner2 : error).Message.Split('\n')[0]);
                }
                finally { await Execute(server, $"DROP DATABASE IF EXISTS `{schema}`"); }
            }
        }

        Assert.True(executed > 0, "No durability cases were discovered.");
        Assert.True(failures.Count == 0, $"{failures.Count} of {executed} cases failed on MySQL:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static async Task Execute(string server, string sql)
    {
        await using var connection = new MySqlConnection(server);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
