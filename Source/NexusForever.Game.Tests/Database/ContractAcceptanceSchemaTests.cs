using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Quest;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class ContractAcceptanceSchemaTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProviderSnapshotsAndMigrationsRetainNullableExplicitConcurrentUniqueAcceptanceIdentity(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []);
        var entity = context.Model.FindEntityType(typeof(CharacterQuestModel));
        var property = entity.FindProperty(nameof(CharacterQuestModel.ContractAcceptanceId));
        Assert.True(property.IsNullable); Assert.True(property.IsConcurrencyToken);
        Assert.Equal(32, property.GetMaxLength()); Assert.Equal("char(32)", property.GetColumnType());
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, property.ValueGenerated);
        var index = Assert.Single(entity.GetIndexes(), i => i.GetDatabaseName() == "IX_character_quest_contractAcceptance");
        Assert.True(index.IsUnique); Assert.Equal(new[] { nameof(CharacterQuestModel.Id), nameof(CharacterQuestModel.ContractAcceptanceId) }, index.Properties.Select(p => p.Name));
        Assert.False(context.Database.HasPendingModelChanges());
        var migrations = context.GetService<IMigrationsAssembly>(); var last = migrations.Migrations.Single(row => row.Key.EndsWith("_ContractQuestAcceptance"));
        Assert.EndsWith("_ContractQuestAcceptance", last.Key);
        var migration = migrations.CreateMigration(last.Value, context.Database.ProviderName);
        Assert.Collection(migration.UpOperations,
            operation => { var column = Assert.IsType<AddColumnOperation>(operation); Assert.Equal("character_quest", column.Table); Assert.Equal("contractAcceptanceId", column.Name); Assert.True(column.IsNullable); Assert.Null(column.DefaultValue); },
            operation => { var added = Assert.IsType<CreateIndexOperation>(operation); Assert.Equal("character_quest", added.Table); Assert.True(added.IsUnique); Assert.Equal(new[] { "id", "contractAcceptanceId" }, added.Columns); });
    }

    [Fact]
    public async Task PopulatedSqliteUpgradeAndReplayPreserveEveryOldQuestValueAndLeaveAllHistoricalTokensNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection,
            builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new CharacterContext(options);
        var migrations = context.Database.GetMigrations().ToArray();
        int acceptance = Array.FindIndex(migrations, id => id.EndsWith("_ContractQuestAcceptance")); string previous = migrations[acceptance - 1];
        await context.GetService<IMigrator>().MigrateAsync(previous);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character (id,name,isOnline) VALUES (42,'Legacy Contract acceptance',0)");
        DateTime legacyReset = new(2026, 10, 31, 10, 30, 0, DateTimeKind.Utc);
        foreach (var pair in new[] { (9001, QuestState.Accepted), (9002, QuestState.Achieved), (9003, QuestState.Completed) })
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO character_quest (id,questId,state,flags,timer,reset) VALUES (42,{pair.Item1},{(byte)pair.Item2},7,1200,{legacyReset})");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character_quest_objective (id,questId,`index`,progress,timer) VALUES (42,9001,0,2,250)");
        async Task<string[]> OldRows(string table, string fields)
        {
            await using var command = connection.CreateCommand(); command.CommandText = $"SELECT {fields} FROM {table} ORDER BY questId";
            await using var reader = await command.ExecuteReaderAsync(); var result = new List<string>();
            while (await reader.ReadAsync()) result.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
            return result.ToArray();
        }
        var quests = await OldRows("character_quest", "id,questId,state,flags,timer,reset");
        var objectives = await OldRows("character_quest_objective", "id,questId,`index`,progress,timer");
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync();
        Assert.Equal(quests, await OldRows("character_quest", "id,questId,state,flags,timer,reset"));
        Assert.Equal(objectives, await OldRows("character_quest_objective", "id,questId,`index`,progress,timer"));
        context.ChangeTracker.Clear(); Assert.Equal(3, await context.CharacterQuest.CountAsync());
        Assert.All(await context.CharacterQuest.AsNoTracking().ToArrayAsync(), q => Assert.Null(q.ContractAcceptanceId));
        Assert.Equal(migrations, await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.CharacterChallengeRewardTrack.ToArrayAsync()); Assert.Empty(await context.CharacterChallengeMedalReceipt.ToArrayAsync());
    }
}
