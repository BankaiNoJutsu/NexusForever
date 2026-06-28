using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Tests.Entity;

public class PetCustomisationTests
{
    [Fact]
    public void Save_CreateWhenRowDoesNotExist_InsertsRow()
    {
        using CharacterContext context = CreateContext();

        var customisation = new PetCustomisation(43ul, PetType.ScanBot, 1u);

        customisation.Save(context);
        context.SaveChanges();

        CharacterPetCustomisationModel row = Assert.Single(context.CharacterPetCustomisation.AsNoTracking());
        Assert.Equal(43ul, row.Id);
        Assert.Equal((byte)PetType.ScanBot, row.Type);
        Assert.Equal(1u, row.ObjectId);
        Assert.Equal(string.Empty, row.Name);
        Assert.Equal(0ul, row.FlairIdMask);
    }

    [Fact]
    public void Save_CreateWhenRowAlreadyExists_DoesNotInsertDuplicate()
    {
        using CharacterContext context = CreateContext();
        context.CharacterPetCustomisation.Add(new CharacterPetCustomisationModel
        {
            Id          = 43ul,
            Type        = (byte)PetType.ScanBot,
            ObjectId    = 1u,
            Name        = "Scanner",
            FlairIdMask = 123ul
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var customisation = new PetCustomisation(43ul, PetType.ScanBot, 1u);

        customisation.Save(context);
        context.SaveChanges();

        CharacterPetCustomisationModel row = Assert.Single(context.CharacterPetCustomisation.AsNoTracking());
        Assert.Equal(43ul, row.Id);
        Assert.Equal((byte)PetType.ScanBot, row.Type);
        Assert.Equal(1u, row.ObjectId);
        Assert.Equal("Scanner", row.Name);
        Assert.Equal(123ul, row.FlairIdMask);
    }

    [Fact]
    public void Save_CreateWithNameWhenRowAlreadyExists_UpdatesExistingRow()
    {
        using CharacterContext context = CreateContext();
        context.CharacterPetCustomisation.Add(new CharacterPetCustomisationModel
        {
            Id          = 43ul,
            Type        = (byte)PetType.ScanBot,
            ObjectId    = 1u,
            Name        = "Scanner",
            FlairIdMask = 123ul
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var customisation = new PetCustomisation(43ul, PetType.ScanBot, 1u)
        {
            Name = "Buddy"
        };

        customisation.Save(context);
        context.SaveChanges();

        CharacterPetCustomisationModel row = Assert.Single(context.CharacterPetCustomisation.AsNoTracking());
        Assert.Equal("Buddy", row.Name);
        Assert.Equal(123ul, row.FlairIdMask);
    }

    [Fact]
    public void UpsertTrackedCreates_WithTrackedDuplicateInsert_DetachesPendingInsert()
    {
        using CharacterContext context = CreateContext();
        context.CharacterPetCustomisation.Add(new CharacterPetCustomisationModel
        {
            Id          = 43ul,
            Type        = (byte)PetType.ScanBot,
            ObjectId    = 1u,
            Name        = "Scanner",
            FlairIdMask = 123ul
        });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        context.CharacterPetCustomisation.Add(new CharacterPetCustomisationModel
        {
            Id          = 43ul,
            Type        = (byte)PetType.ScanBot,
            ObjectId    = 1u,
            Name        = string.Empty,
            FlairIdMask = 0ul
        });

        PetCustomisation.UpsertTrackedCreates(context);
        Assert.Empty(context.ChangeTracker.Entries<CharacterPetCustomisationModel>());

        context.SaveChanges();
        context.ChangeTracker.Clear();

        CharacterPetCustomisationModel row = Assert.Single(context.CharacterPetCustomisation.AsNoTracking());
        Assert.Equal("Scanner", row.Name);
        Assert.Equal(123ul, row.FlairIdMask);
    }

    [Fact]
    public void UpsertTrackedCreates_WithTrackedMissingInsert_InsertsRow()
    {
        using CharacterContext context = CreateContext();

        context.CharacterPetCustomisation.Add(new CharacterPetCustomisationModel
        {
            Id          = 43ul,
            Type        = (byte)PetType.ScanBot,
            ObjectId    = 1u,
            Name        = "Scanner",
            FlairIdMask = 123ul
        });

        PetCustomisation.UpsertTrackedCreates(context);
        Assert.Empty(context.ChangeTracker.Entries<CharacterPetCustomisationModel>());

        context.SaveChanges();
        context.ChangeTracker.Clear();

        CharacterPetCustomisationModel row = Assert.Single(context.CharacterPetCustomisation.AsNoTracking());
        Assert.Equal(43ul, row.Id);
        Assert.Equal((byte)PetType.ScanBot, row.Type);
        Assert.Equal(1u, row.ObjectId);
        Assert.Equal("Scanner", row.Name);
        Assert.Equal(123ul, row.FlairIdMask);
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
            CREATE TABLE character_pet_customisation (
                id INTEGER NOT NULL DEFAULT 0,
                type INTEGER NOT NULL DEFAULT 0,
                objectId INTEGER NOT NULL DEFAULT 0,
                name TEXT NOT NULL DEFAULT '',
                flairIdMask INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (id, type, objectId)
            );
            """);

        return context;
    }
}
