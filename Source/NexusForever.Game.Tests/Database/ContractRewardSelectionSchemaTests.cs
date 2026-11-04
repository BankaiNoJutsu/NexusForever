using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class ContractRewardSelectionSchemaTests
{
    [Fact]
    public void MySqlForwardRepairIsDiscoverableConditionalAndDoesNotDropExistingOptionsOnRollback()
    {
        using var context = new Factory().CreateDbContext(["--mysql"]); var assembly = context.GetService<IMigrationsAssembly>();
        var definition = assembly.Migrations.Single(row => row.Key=="20261031033000_RepairLegacyCharacterColumns");
        var migration = assembly.CreateMigration(definition.Value, context.Database.ProviderName);
        Assert.Equal(8, migration.UpOperations.Count); Assert.Empty(migration.DownOperations);
        string[] columns=["castingOptions","combatLogDisableFlags","disableOtherPlayersCombatLogs","sharedChallengeEnabled","activePropUnitId","decorData","hookBagIndex","hookIndex"];
        foreach (string column in columns)
        {
            var operation = Assert.Single(migration.UpOperations.Cast<SqlOperation>(), row => row.Sql.Contains("COLUMN_NAME = '"+column+"'"));
            Assert.Contains("TABLE_SCHEMA = DATABASE()", operation.Sql); Assert.Contains("'SELECT 1'", operation.Sql);
            Assert.Contains("NOT NULL DEFAULT 0", operation.Sql); Assert.DoesNotContain("DROP", operation.Sql); Assert.DoesNotContain("UPDATE", operation.Sql);
        }
    }

    [Fact]
    public async Task SqliteHistoricalInitialMigrationAlreadyContainsTheEightLegacyColumns()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new CharacterContext(options); await context.Database.MigrateAsync();
        foreach (var pair in new[] { ("character",new[]{"castingOptions","combatLogDisableFlags","disableOtherPlayersCombatLogs","sharedChallengeEnabled"}),
            ("residence_decor",new[]{"activePropUnitId","decorData","hookBagIndex","hookIndex"}) })
        {
            using var command = connection.CreateCommand(); command.CommandText="PRAGMA table_info('"+pair.Item1+"')";
            var columns = new List<string>(); await using var reader = await command.ExecuteReaderAsync(); while(await reader.ReadAsync()) columns.Add(reader.GetString(1));
            Assert.All(pair.Item2,column=>Assert.Contains(column,columns));
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProvidersAddOnlyTheUnselectedMarkerAndCharacterOwnedSlots(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []);
        Assert.False(context.Database.HasPendingModelChanges()); var migrations = context.GetService<IMigrationsAssembly>();
        var definition = migrations.Migrations.Single(row => row.Key.EndsWith("_ContractRewardSelections"));
        var migration = migrations.CreateMigration(definition.Value, context.Database.ProviderName);
        Assert.Equal(2, migration.UpOperations.Count);
        var marker = Assert.Single(migration.UpOperations.OfType<AddColumnOperation>());
        Assert.Equal("character_contract_reward_track", marker.Table); Assert.Equal("selectionInitialized", marker.Name);
        Assert.Equal(false, marker.DefaultValue); Assert.False(marker.IsNullable);
        var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        Assert.Equal("character_contract_reward_slot", table.Name);
        Assert.Equal(new[] { "id", "trackId", "cycleId", "ordinal" }, table.PrimaryKey.Columns);
        Assert.All(table.Columns, column => { Assert.False(column.IsNullable); Assert.Null(column.DefaultValue); });
        var foreign = Assert.Single(table.ForeignKeys); Assert.Equal("character_contract_reward_track", foreign.PrincipalTable);
        Assert.Equal(new[] { "id", "trackId" }, foreign.Columns); Assert.Equal(ReferentialAction.Cascade, foreign.OnDelete);
        var slot = context.Model.FindEntityType(typeof(CharacterContractRewardSlotModel));
        Assert.All(slot.FindPrimaryKey().Properties, property => Assert.Equal(ValueGenerated.Never, property.ValueGenerated));
    }

    [Fact]
    public async Task PopulatedUpgradeRetainsPointsHistoryAndAcceptanceWithoutSamplingOrGrants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new CharacterContext(options);
        await context.GetService<IMigrator>().MigrateAsync("20261030234818_ContractCompletionReceipts");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character (id,name,isOnline) VALUES (42,'Contract selection upgrade',0)");
        context.CharacterQuest.Add(new() { Id = 42, QuestId = 9880, State = 2, ContractAcceptanceId = "123456781234123412341234567890ab" });
        context.CharacterContractCompletion.Add(new() { Id = 42, AcceptanceId = "123456781234123412341234567890ab", QuestId = 9880,
            TrackId = 5, Points = 50, BasePoints = 50, PayloadVersion = 1, PayloadJson = "unchanged audit", CreatedUtc = DateTime.UtcNow });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character_contract_reward_track (id,trackId,cycleId,points,revision,active) VALUES (42,5,7,899,3,1)");
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync(); context.ChangeTracker.Clear();
        var track = Assert.Single(context.CharacterContractRewardTrack);
        Assert.Equal(899u, track.Points); Assert.Equal(7ul, track.CycleId); Assert.Equal(3ul, track.Revision); Assert.True(track.Active); Assert.False(track.SelectionInitialized);
        Assert.Empty(context.CharacterContractRewardSlot); Assert.Empty(context.Item);
        Assert.Equal("unchanged audit", context.CharacterContractCompletion.Single().PayloadJson);
        Assert.Equal("123456781234123412341234567890ab", context.CharacterQuest.Single().ContractAcceptanceId);
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task SlotKeysRetainZeroCycleAndOrdinalAndCascadeOnlyWithTheirCharacter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options;
        await using var context = new CharacterContext(options); await context.Database.EnsureCreatedAsync();
        context.Character.Add(new() { Id = 42, Name = "Selection owner" }); context.Character.Add(new() { Id = 43, Name = "Retained owner" });
        context.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = 5, Revision = 1, SelectionInitialized = true });
        context.CharacterContractRewardSlot.Add(new() { Id = 42, TrackId = 5, CycleId = 0, Ordinal = 0, RewardRowId = 17, RequiredPoints = 1000, SelectionJson = "schema fixture" });
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var slot = Assert.Single(context.CharacterContractRewardSlot); Assert.Equal(0ul, slot.CycleId); Assert.Equal((byte)0, slot.Ordinal);
        context.Character.Remove(await context.Character.SingleAsync(row => row.Id == 42)); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        Assert.Empty(context.CharacterContractRewardSlot); Assert.Empty(context.CharacterContractRewardTrack); Assert.Equal(43ul, context.Character.Single().Id);
    }
}
