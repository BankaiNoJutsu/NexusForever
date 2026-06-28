using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public class PathEntryTests
{
    [Fact]
    public void Save_WithCreateMaskAndExistingDatabaseRow_UpdatesExistingPathRow()
    {
        using CharacterContext context = CreateContext();

        context.CharacterPath.Add(new CharacterPathModel
        {
            Id = 42ul,
            Path = (byte)Path.Soldier,
            Unlocked = 0,
            TotalXp = 0u,
            LevelRewarded = 0
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var entry = new PathEntry(42ul, Path.Soldier, true)
        {
            TotalXp = 75u,
            LevelRewarded = 2
        };

        entry.Save(context);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        CharacterPathModel row = Assert.Single(context.CharacterPath.AsNoTracking());
        Assert.Equal(42ul, row.Id);
        Assert.Equal((byte)Path.Soldier, row.Path);
        Assert.Equal(1, row.Unlocked);
        Assert.Equal(75u, row.TotalXp);
        Assert.Equal(2, row.LevelRewarded);
    }

    [Fact]
    public void Save_WithCreateMaskAndTrackedDuplicateInsert_DetachesPendingInsert()
    {
        using CharacterContext context = CreateContext();

        context.CharacterPath.Add(new CharacterPathModel
        {
            Id = 42ul,
            Path = (byte)Path.Soldier,
            Unlocked = 0,
            TotalXp = 0u,
            LevelRewarded = 0
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        context.CharacterPath.Add(new CharacterPathModel
        {
            Id = 42ul,
            Path = (byte)Path.Soldier,
            Unlocked = 0,
            TotalXp = 0u,
            LevelRewarded = 0
        });

        var entry = new PathEntry(42ul, Path.Soldier, true)
        {
            TotalXp = 75u,
            LevelRewarded = 2
        };

        entry.Save(context);
        Assert.Empty(context.ChangeTracker.Entries<CharacterPathModel>());

        context.SaveChanges();
        context.ChangeTracker.Clear();

        CharacterPathModel row = Assert.Single(context.CharacterPath.AsNoTracking());
        Assert.Equal(1, row.Unlocked);
        Assert.Equal(75u, row.TotalXp);
        Assert.Equal(2, row.LevelRewarded);
    }

    [Fact]
    public void UpsertTrackedCreates_WithTrackedDuplicateInsert_DetachesPendingInsert()
    {
        using CharacterContext context = CreateContext();

        context.CharacterPath.Add(new CharacterPathModel
        {
            Id = 42ul,
            Path = (byte)Path.Soldier,
            Unlocked = 0,
            TotalXp = 0u,
            LevelRewarded = 0
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        context.CharacterPath.Add(new CharacterPathModel
        {
            Id = 42ul,
            Path = (byte)Path.Soldier,
            Unlocked = 1,
            TotalXp = 75u,
            LevelRewarded = 2
        });

        PathEntry.UpsertTrackedCreates(context);
        Assert.Empty(context.ChangeTracker.Entries<CharacterPathModel>());

        context.SaveChanges();
        context.ChangeTracker.Clear();

        CharacterPathModel row = Assert.Single(context.CharacterPath.AsNoTracking());
        Assert.Equal(1, row.Unlocked);
        Assert.Equal(75u, row.TotalXp);
        Assert.Equal(2, row.LevelRewarded);
    }

    [Fact]
    public void Save_WithCreateMaskAndMissingDatabaseRow_InsertsPathRow()
    {
        using CharacterContext context = CreateContext();

        var entry = new PathEntry(42ul, Path.Soldier, true)
        {
            TotalXp = 75u,
            LevelRewarded = 2
        };

        entry.Save(context);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        CharacterPathModel row = Assert.Single(context.CharacterPath.AsNoTracking());
        Assert.Equal(42ul, row.Id);
        Assert.Equal((byte)Path.Soldier, row.Path);
        Assert.Equal(1, row.Unlocked);
        Assert.Equal(75u, row.TotalXp);
        Assert.Equal(2, row.LevelRewarded);
    }

    private static CharacterContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CharacterContext(options);
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE character_path (
                id INTEGER NOT NULL,
                path INTEGER NOT NULL DEFAULT 0,
                unlocked INTEGER NOT NULL DEFAULT 0,
                totalXp INTEGER NOT NULL DEFAULT 0,
                levelRewarded INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (id, path)
            );
            """);

        return context;
    }
}
