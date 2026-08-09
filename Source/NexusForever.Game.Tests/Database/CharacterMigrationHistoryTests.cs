using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using NexusForever.Database.Character;

namespace NexusForever.Game.Tests.Database;

public class CharacterMigrationHistoryTests
{
    [Fact]
    public void CostumeMessageMigrationHistoryIsPreservedWithoutDuplicateSchemaChanges()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using CharacterContext context = new(options);
        string[] migrationIds = context.Database.GetMigrations().ToArray();
        string migrationScript = context.Database.GetService<IMigrator>().GenerateScript();

        Assert.Contains("20260516125523_CostumeMessageChanges", migrationIds);
        Assert.Contains("20260604130000_CostumeMessageSemantics", migrationIds);
        Assert.Contains("COLUMN_NAME = 'itemId'", migrationScript, StringComparison.Ordinal);
        Assert.Contains("COLUMN_NAME = 'item2Id'", migrationScript, StringComparison.Ordinal);
        Assert.Contains("COLUMN_NAME = 'mask'", migrationScript, StringComparison.Ordinal);
        Assert.Contains("COLUMN_NAME = 'visibilityMask'", migrationScript, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(migrationScript, @"RENAME COLUMN `?itemId`? TO `?item2Id`?", RegexOptions.CultureInvariant));
        Assert.Single(Regex.Matches(migrationScript, @"RENAME COLUMN `?mask`? TO `?visibilityMask`?", RegexOptions.CultureInvariant));
    }
}
