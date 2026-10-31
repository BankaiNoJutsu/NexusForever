using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Static.Entity;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;
using MySqlMigration = NexusForever.Database.Character.Migrations.CharacterChallengeRewardTracks;
using SqliteMigration = NexusForever.Database.Sqlite.Migrations.Character.CharacterChallengeRewardTracks;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterChallengeRewardSchemaTests : IDisposable
{
    private const string PreviousSqlite = "20261029122550_CharacterTaxiNode";
    private const string PreviousMySql = "20261029122531_CharacterTaxiNode";
    private const string NewMySql = "20261029215652_CharacterChallengeRewardTracks";
    private const string AttemptId = "0123456789abcdef0123456789abcdef";
    private static readonly string[] NewTables = ["character_challenge_attempt", "character_challenge_reward_track",
        "character_challenge_reward_slot", "character_challenge_medal_receipt", "character_challenge_reward_claim"];
    private readonly string path = Path.Combine(Path.GetTempPath(), $"NexusForeverChallengeSchema_{Guid.NewGuid():N}.sqlite");

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ProviderSnapshotMatchesConfiguredModelAndPreservesOwnership(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []);
        var snapshotModel = context.GetService<IModelRuntimeInitializer>().Initialize(context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model, designTime: true);
        var difference = context.GetService<IMigrationsModelDiffer>().GetDifferences(snapshotModel.GetRelationalModel(),
            context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel());
        Assert.False(context.Database.HasPendingModelChanges(), string.Join("; ", difference.Select(operation => operation is AlterColumnOperation column
            ? $"{column.Table}.{column.Name}: {column.OldColumn.ColumnType}/{column.OldColumn.DefaultValue} -> {column.ColumnType}/{column.DefaultValue}" : operation.GetType().Name)));
        var model = context.Model;
        Assert.True(model.FindEntityType(typeof(CharacterChallengeModel))!.FindProperty(nameof(CharacterChallengeModel.ActiveAttemptId))!.IsNullable);
        Assert.True(model.FindEntityType(typeof(CharacterChallengeRewardTrackModel))!.FindProperty(nameof(CharacterChallengeRewardTrackModel.Revision))!.IsConcurrencyToken);
        Assert.Equal(new[] { "Id", "TrackId", "CycleId", "Ordinal" }, model.FindEntityType(typeof(CharacterChallengeRewardSlotModel))!
            .FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(new[] { "Id", "TrackId", "CycleId", "Ordinal" }, model.FindEntityType(typeof(CharacterChallengeRewardClaimModel))!
            .FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(new[] { "Id", "AttemptId", "Medal" }, model.FindEntityType(typeof(CharacterChallengeMedalReceiptModel))!
            .FindPrimaryKey()!.Properties.Select(property => property.Name));
        var claims = model.FindEntityType(typeof(CharacterChallengeRewardClaimModel))!;
        Assert.Contains(claims.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([nameof(CharacterChallengeRewardClaimModel.ClaimId)]));
        Assert.Equal("longtext", claims.FindProperty(nameof(CharacterChallengeRewardClaimModel.DeliveryJson))!.GetColumnType());
        foreach (var entity in model.GetEntityTypes().Where(entity => NewTables.Contains(entity.GetTableName())))
            Assert.DoesNotContain(entity.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(ItemModel));
        Assert.DoesNotContain(model.GetEntityTypes(), entity => entity.GetTableName() is "character_reward_track" or "character_reward_track_milestone");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ForwardMigrationOnlyAddsAttemptMarkerFiveTablesAndTwoIndexes(bool mysql)
    {
        Migration migration = mysql ? new MySqlMigration() : new SqliteMigration();
        var operations = migration.UpOperations;
        Assert.Equal(NewTables.Order(), operations.OfType<CreateTableOperation>().Select(operation => operation.Name).Order());
        AddColumnOperation marker = Assert.Single(operations.OfType<AddColumnOperation>());
        Assert.Equal("character_challenge", marker.Table); Assert.Equal("activeAttemptId", marker.Name);
        Assert.True(marker.IsNullable); Assert.Null(marker.DefaultValue); Assert.Null(marker.DefaultValueSql);
        Assert.Equal(2, operations.OfType<CreateIndexOperation>().Count());
        Assert.All(operations, operation => Assert.True(operation is AddColumnOperation or CreateTableOperation or CreateIndexOperation));
        foreach (var table in operations.OfType<CreateTableOperation>())
            Assert.DoesNotContain(table.ForeignKeys, key => key.PrincipalTable == "item");
    }

    [Fact]
    public void MySqlForwardScriptUsesNewNamesAndLeavesLegacyAndItemTablesUntouched()
    {
        using var context = new Factory().CreateDbContext(["--mysql"]);
        string sql = context.GetService<IMigrator>().GenerateScript(PreviousMySql, NewMySql, MigrationsSqlGenerationOptions.Idempotent);
        foreach (string table in NewTables) Assert.Contains($"CREATE TABLE `{table}`", sql, StringComparison.Ordinal);
        Assert.Contains("`activeAttemptId` char(32)", sql, StringComparison.Ordinal);
        foreach (string table in new[] { "item", "character_reward_track", "character_reward_track_milestone" })
        {
            Assert.DoesNotContain($"DROP TABLE `{table}`", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"ALTER TABLE `{table}`", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"UPDATE `{table}`", sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task PopulatedSqliteUpgradePreservesEveryOldValueAndOpaqueGhostTables()
    {
        await using var context = Context();
        await context.GetService<IMigrator>().MigrateAsync(PreviousSqlite);
        context.Character.Add(Owner());
        context.Item.Add(Item(700));
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO character_challenge
            (id,challengeId,activated,onCooldown,leftArea,currentCount,currentTier,lastRewardTier,completionCount,activeTimerSeconds,cooldownTimerSeconds,areaFailTimerSeconds)
            VALUES (1,280,1,0,1,17,2,1,9,123.5,67.25,0.5);
            CREATE TABLE character_reward_track (id INTEGER NOT NULL,rewardTrackId INTEGER NOT NULL,points INTEGER NOT NULL,PRIMARY KEY(id,rewardTrackId));
            CREATE TABLE character_reward_track_milestone (id INTEGER NOT NULL,rewardTrackId INTEGER NOT NULL,milestoneId INTEGER NOT NULL,choice INTEGER NOT NULL,pointsRequired INTEGER NOT NULL,PRIMARY KEY(id,rewardTrackId,milestoneId));
            INSERT INTO character_reward_track VALUES (1,16,999);
            INSERT INTO character_reward_track_milestone VALUES (1,16,321,2,200);
            """);
        string[] oldTables = ["character", "character_challenge", "item", "character_reward_track", "character_reward_track_milestone"];
        var before = new Dictionary<string, (string[] Columns, string Rows)>();
        foreach (string table in oldTables)
        {
            string[] columns = await Columns(context, table);
            before[table] = (columns, await Rows(context, table, columns));
        }
        await context.Database.MigrateAsync();
        foreach (string table in oldTables) Assert.Equal(before[table].Rows, await Rows(context, table, before[table].Columns));
        Assert.Null((await context.CharacterChallenge.AsNoTracking().SingleAsync()).ActiveAttemptId);
        Assert.Empty(await context.CharacterChallengeAttempt.ToListAsync());
        Assert.Empty(await context.CharacterChallengeRewardTrack.ToListAsync());
        Assert.Empty(await context.CharacterChallengeRewardSlot.ToListAsync());
        Assert.Empty(await context.CharacterChallengeMedalReceipt.ToListAsync());
        Assert.Empty(await context.CharacterChallengeRewardClaim.ToListAsync());
        string[] history = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        await context.Database.MigrateAsync();
        Assert.Equal(history, (await context.Database.GetAppliedMigrationsAsync()).ToArray());
        foreach (string table in oldTables) Assert.Equal(before[table].Rows, await Rows(context, table, before[table].Columns));
    }

    [Fact]
    public async Task ReceiptsSurviveItemConsumptionAndRepeatedRowsAcrossCycles()
    {
        await using (var context = Context())
        {
            await context.Database.MigrateAsync();
            context.Character.Add(Owner());
            context.CharacterChallengeAttempt.Add(new CharacterChallengeAttemptModel {
                Id = 1, AttemptId = AttemptId, ChallengeId = 280, TrackId = 16, RewardMode = 1, EarnedMedalMask = 1 });
            context.CharacterChallengeRewardTrack.Add(new CharacterChallengeRewardTrackModel {
                Id = 1, TrackId = 16, CycleId = 1, Points = 100, Revision = 2, Active = true });
            context.CharacterChallengeRewardTrack.Add(new CharacterChallengeRewardTrackModel { Id = 1, TrackId = 17 });
            context.CharacterChallengeRewardTrack.Add(new CharacterChallengeRewardTrackModel { Id = 1, TrackId = 18 });
            context.CharacterChallengeRewardSlot.AddRange(Slot(0, 0, 320), Slot(0, 1, 321), Slot(1, 0, 320));
            context.CharacterChallengeMedalReceipt.Add(new CharacterChallengeMedalReceiptModel {
                Id = 1, AttemptId = AttemptId, Medal = 0, TrackId = 16, Points = 125,
                Multiplier = 1.25f, ModifierRevision = -7, CreatedUtc = DateTime.UtcNow });
            context.CharacterChallengeRewardClaim.AddRange(Claim(0, 'a', 700), Claim(1, 'b', 701));
            context.Item.AddRange(Item(700), Item(701));
            await context.SaveChangesAsync();
            context.Item.RemoveRange(await context.Item.ToListAsync());
            await context.SaveChangesAsync();
        }
        await using var reload = Context();
        Assert.Empty(await reload.Item.ToListAsync());
        var claims = (await reload.CharacterChallengeRewardClaim.ToListAsync()).OrderBy(row => row.CycleId).ToList();
        Assert.Equal(new ulong[] { 0, 1 }, claims.Select(row => row.CycleId));
        Assert.All(claims, row => { Assert.Equal(320u, row.RewardRowId); Assert.Equal(1u, row.DeliveryVersion); Assert.Contains("7001", row.DeliveryJson); });
        Assert.Equal(3, await reload.CharacterChallengeRewardSlot.CountAsync());
        var equalCostSlots = await reload.CharacterChallengeRewardSlot.Where(row => row.CycleId == 0).OrderBy(row => row.Ordinal).ToListAsync();
        Assert.Equal(new byte[] { 0, 1 }, equalCostSlots.Select(row => row.Ordinal));
        Assert.All(equalCostSlots, row => Assert.Equal(100u, row.RequiredPoints));
        var medal = Assert.Single(await reload.CharacterChallengeMedalReceipt.ToListAsync());
        Assert.Equal(125u, medal.Points); Assert.Equal(1.25f, medal.Multiplier); Assert.Equal(-7, medal.ModifierRevision);
    }

    [Fact]
    public async Task DuplicateCycleOrdinalCannotCreateAnotherClaimReceipt()
    {
        await using (var context = Context())
        {
            await context.Database.MigrateAsync();
            context.Character.Add(Owner());
            context.CharacterChallengeRewardTrack.Add(new CharacterChallengeRewardTrackModel { Id = 1, TrackId = 16 });
            context.CharacterChallengeRewardSlot.Add(Slot(0, 0, 320));
            context.CharacterChallengeRewardClaim.Add(Claim(0, 'a', 700));
            await context.SaveChangesAsync();
        }
        await using (var competing = Context())
        {
            competing.CharacterChallengeRewardClaim.Add(Claim(0, 'b', 701));
            await Assert.ThrowsAsync<DbUpdateException>(() => competing.SaveChangesAsync());
        }
        await using var reload = Context();
        Assert.Equal(new string('a', 32), Assert.Single(await reload.CharacterChallengeRewardClaim.ToListAsync()).ClaimId);
    }

    private CharacterContext Context() => new(new DatabaseConnectionString {
        Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={path};Pooling=False" });

    private static CharacterModel Owner() => new() { Id = 1, AccountId = 12, Name = "ChallengeSchema",
        OriginalName = "ChallengeSchema", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow };
    private static ItemModel Item(ulong id) => new() { Id = id, OwnerId = 1, ItemId = 7001,
        Location = (ushort)InventoryLocation.Inventory, BagIndex = 4, StackCount = 3, Charges = 0,
        Durability = 0.75f, ExpirationTimeLeft = 1234, Soulbound = true, MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" };
    private static CharacterChallengeRewardSlotModel Slot(ulong cycle, byte ordinal, uint row) => new() {
        Id = 1, TrackId = 16, CycleId = cycle, Ordinal = ordinal, RewardRowId = row, RequiredPoints = 100, Claimed = ordinal == 0 };
    private static CharacterChallengeRewardClaimModel Claim(ulong cycle, char id, ulong item) => new() {
        Id = 1, TrackId = 16, CycleId = cycle, Ordinal = 0, RewardRowId = 320, Choice = 0,
        ClaimId = new string(id, 32), DeliveryVersion = 1, DeliveryJson = JsonSerializer.Serialize(new[] { Item(item) }), CreatedUtc = DateTime.UtcNow };

    private static async Task<string[]> Columns(CharacterContext context, string table)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand(); command.CommandText = $"PRAGMA table_info({Quote(table)})";
        await using var reader = await command.ExecuteReaderAsync(); var columns = new List<string>();
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
        return columns.ToArray();
    }

    private static async Task<string> Rows(CharacterContext context, string table, string[] columns)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {string.Join(',', columns.Select(Quote))} FROM {Quote(table)}";
        await using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount]; reader.GetValues(values);
            rows.Add(JsonSerializer.Serialize(values.Select(value => value is DBNull ? null : value)));
        }
        return string.Join('\n', rows.Order(StringComparer.Ordinal));
    }

    private static string Quote(string name) => '"' + name.Replace("\"", "\"\"") + '"';
    public void Dispose()
    {
        foreach (string suffix in new[] { "", "-wal", "-shm" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }
}
