using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Character;

namespace NexusForever.Game.Tests.Database;

public sealed class MigratedCostumeTimestampTests
{
    [Fact]
    public void CostumeTimestampMigrationRestoresTheRuntimeUpdateGenerationContract()
    {
        using var context = new CharacterContext(CharacterMigrationHistoryTests.CreateScriptOptions());
        string previous = context.Database.GetMigrations().Single(id => id.EndsWith("_AlignNullableItemAndLeaderboardColumns", StringComparison.Ordinal));
        string script = context.GetService<IMigrator>().GenerateScript(previous);
        Assert.Contains("ALTER TABLE `character_costume`", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON UPDATE CURRENT_TIMESTAMP", script, StringComparison.OrdinalIgnoreCase);
        string downgrade = context.GetService<IMigrator>().GenerateScript(context.Database.GetMigrations().Last(), previous);
        Assert.Contains("ALTER TABLE `character_costume`", downgrade, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ON UPDATE CURRENT_TIMESTAMP", downgrade, StringComparison.OrdinalIgnoreCase);
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
