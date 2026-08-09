using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterDatabaseSqliteTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusForeverCharacterDatabaseTests_{Guid.NewGuid():N}");

    [Fact]
    public async Task GetNextIdentifiers_Sqlite_AggregatesUnsignedIdentifiers()
    {
        Directory.CreateDirectory(directory);
        DatabaseConnectionString connectionString = CreateConnectionString();

        await using (var context = new CharacterContext(connectionString))
        {
            await context.Database.MigrateAsync();
            context.Guild.Add(new GuildModel
            {
                Id           = 42ul,
                Name         = "SQLite Guild",
                OriginalName = "SQLite Guild",
                CreateTime   = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var database = new CharacterDatabase();
        database.Initialise(connectionString);

        Assert.Equal(0ul, database.GetNextCharacterId());
        Assert.Equal(0ul, database.GetNextItemId());
        Assert.Equal(0ul, database.GetNextResidenceId());
        Assert.Equal(0ul, database.GetNextDecorId());
        Assert.Equal(0ul, database.GetNextMailId());
        Assert.Equal(42ul, database.GetNextGuildId());
        Assert.Equal(0ul, database.GetNextMarketplaceAuctionId());
        Assert.Equal(0ul, database.GetNextMarketplaceCommodityOrderId());
        Assert.Equal(0ul, database.GetNextLeaderboardPveScoreId());
        Assert.Equal(0ul, database.GetNextLeaderboardPvpScoreId());
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

    private DatabaseConnectionString CreateConnectionString()
    {
        return new DatabaseConnectionString
        {
            Provider         = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(directory, "character.sqlite")}"
        };
    }
}
