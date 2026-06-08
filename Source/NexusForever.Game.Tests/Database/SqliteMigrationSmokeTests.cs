using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Chat;
using NexusForever.Database.Configuration.Model;
using NexusForever.Database.Friendship;
using NexusForever.Database.Group;
using NexusForever.Database.World;

namespace NexusForever.Game.Tests.Database;

public sealed class SqliteMigrationSmokeTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusForeverSqliteMigrationTests_{Guid.NewGuid():N}");

    [Fact]
    public async Task SqliteProvider_MigratesAllRuntimeContexts()
    {
        Directory.CreateDirectory(directory);

        await using AuthContext authContext = CreateContext<AuthContext>("auth", options => new AuthContext(options));
        await using CharacterContext characterContext = CreateContext<CharacterContext>("character", options => new CharacterContext(options));
        await using WorldContext worldContext = CreateContext<WorldContext>("world", options => new WorldContext(options));
        await using GroupContext groupContext = CreateContext<GroupContext>("group", options => new GroupContext(options));
        await using ChatContext chatContext = CreateContext<ChatContext>("chat", options => new ChatContext(options));
        await using FriendshipContext friendshipContext = CreateContext<FriendshipContext>("friendship", options => new FriendshipContext(options));

        await AssertMigrates(authContext);
        await AssertMigrates(characterContext);
        await AssertMigrates(worldContext);
        await AssertMigrates(groupContext);
        await AssertMigrates(chatContext);
        await AssertMigrates(friendshipContext);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private TContext CreateContext<TContext>(string databaseName, Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        string databasePath = Path.Combine(directory, $"{databaseName}.sqlite");
        var builder = new DbContextOptionsBuilder<TContext>();
        builder.UseConfiguration(new DatabaseConnectionString
        {
            Provider         = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={databasePath}"
        });

        return factory(builder.Options);
    }

    private static async Task AssertMigrates(DbContext context)
    {
        await context.Database.MigrateAsync();

        string[] appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.NotEmpty(appliedMigrations);
    }
}
