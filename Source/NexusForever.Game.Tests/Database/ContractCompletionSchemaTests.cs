using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class ContractCompletionSchemaTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProviderMigrationsOnlyAddCharacterOwnedReceiptAndProgressionTables(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []);
        Assert.False(context.Database.HasPendingModelChanges());
        var migrations = context.GetService<IMigrationsAssembly>();
        var definition = migrations.Migrations.Single(row => row.Key.EndsWith("_ContractCompletionReceipts"));
        var migration = migrations.CreateMigration(definition.Value, context.Database.ProviderName);
        Assert.All(migration.UpOperations, operation => Assert.IsType<CreateTableOperation>(operation));
        var tables = migration.UpOperations.Cast<CreateTableOperation>().ToArray(); Assert.Equal(2, tables.Length);
        Assert.Equal(new[] { "character_contract_completion", "character_contract_reward_track" }, tables.Select(table => table.Name).Order());
        foreach (var table in tables)
        {
            Assert.All(table.Columns, column => { Assert.False(column.IsNullable); Assert.Null(column.DefaultValue); });
            var foreign = Assert.Single(table.ForeignKeys); Assert.Equal("character", foreign.PrincipalTable); Assert.Equal(new[] { "id" }, foreign.Columns);
        }
        var receipt = context.Model.FindEntityType(typeof(CharacterContractCompletionModel));
        Assert.Equal(new[] { "Id", "AcceptanceId" }, receipt.FindPrimaryKey().Properties.Select(property => property.Name));
        Assert.Equal(32, receipt.FindProperty("AcceptanceId").GetMaxLength()); Assert.False(receipt.FindProperty("PayloadJson").IsNullable);
        var track = context.Model.FindEntityType(typeof(CharacterContractRewardTrackModel)); Assert.True(track.FindProperty("Revision").IsConcurrencyToken);
    }

    [Fact]
    public async Task PopulatedSqliteUpgradeAndReplayKeepAcceptedTokensAndNeverBackfillCredit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection, b => b.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new CharacterContext(options);
        await context.GetService<IMigrator>().MigrateAsync("20261030230524_ContractQuestAcceptance");
        context.Character.Add(new() { Id = 42, Name = "Contract journal upgrade" });
        context.CharacterQuest.Add(new() { Id = 42, QuestId = 9880, State = 2, Flags = 7, Timer = 123, ContractAcceptanceId = "123456781234123412341234567890ab" });
        context.CharacterQuestObjective.Add(new() { Id = 42, QuestId = 9880, Index = 0, Progress = 4, Timer = 250 }); await context.SaveChangesAsync();
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync(); context.ChangeTracker.Clear();
        var quest = Assert.Single(context.CharacterQuest); Assert.Equal((byte)2, quest.State); Assert.Equal((byte)7, quest.Flags); Assert.Equal(123u, quest.Timer); Assert.Equal("123456781234123412341234567890ab", quest.ContractAcceptanceId);
        var objective = Assert.Single(context.CharacterQuestObjective); Assert.Equal(4u, objective.Progress); Assert.Equal(250u, objective.Timer); Assert.Equal((byte)0, objective.Index);
        Assert.Empty(context.CharacterContractCompletion); Assert.Empty(context.CharacterContractRewardTrack);
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
    }
}
