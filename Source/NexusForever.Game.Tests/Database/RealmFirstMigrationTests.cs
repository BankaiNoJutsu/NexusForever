using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class RealmFirstMigrationTests
{
    private const string PreviousSqlite = "20261102182001_BankReconciliationReceipts";
    private const string LatestSqlite = "20261103170302_RealmFirstClaims";
    private const string LatestMySql = "20261103170256_RealmFirstClaims";

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ClaimMigrationAddsOnlyItsStoreAndKeepsStableCompositeKeys(bool sqlite)
    {
        using var context = new CharacterContext(sqlite ? Sqlite("Data Source=:memory:") : CharacterMigrationHistoryTests.CreateScriptOptions());
        Assert.False(context.Database.HasPendingModelChanges());
        var assembly = context.GetService<IMigrationsAssembly>();
        string latest = sqlite ? LatestSqlite : LatestMySql;
        var migration = assembly.CreateMigration(assembly.Migrations[latest], context.Database.ProviderName!);
        Assert.Equal("realm_first_achievement", Assert.IsType<CreateTableOperation>(Assert.Single(migration.UpOperations)).Name);
        Assert.Equal("realm_first_achievement", Assert.IsType<DropTableOperation>(Assert.Single(migration.DownOperations)).Name);
        var model = context.Model.FindEntityType(typeof(RealmFirstAchievementModel))!;
        Assert.Equal(new[] { nameof(RealmFirstAchievementModel.IsGuild), nameof(RealmFirstAchievementModel.AchievementId) }, model.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Empty(model.GetForeignKeys());
        foreach (var snapshot in new[] { context.Model, assembly.ModelSnapshot!.Model })
        {
            var claim = snapshot.FindEntityType(typeof(RealmFirstAchievementModel).FullName!)!;
            Assert.All(claim.FindPrimaryKey()!.Properties, property => Assert.Equal(ValueGenerated.Never, property.ValueGenerated));
            var rank = snapshot.FindEntityType(typeof(GuildRankModel).FullName!)!;
            Assert.Equal(ValueGenerated.Never, rank.FindProperty(nameof(GuildRankModel.Id))!.ValueGenerated);
            Assert.Equal(ValueGenerated.Never, rank.FindProperty(nameof(GuildRankModel.Index))!.ValueGenerated);
        }
        context.RealmFirstAchievement.AddRange(new RealmFirstAchievementModel { AchievementId = 500, OwnerId = 42, Token = new string('1', 32) },
            new RealmFirstAchievementModel { IsGuild = true, AchievementId = 500, OwnerId = 77, Token = new string('2', 32) });
        Assert.Equal(2, context.ChangeTracker.Entries<RealmFirstAchievementModel>().Count());
        Assert.All(context.ChangeTracker.Entries<RealmFirstAchievementModel>(), row => Assert.False(row.Property(model => model.IsGuild).IsTemporary));
    }

    [Fact]
    public async Task CopiedPreviousSqliteDataSurvivesUpgradeAndClosesHistoryWithoutInventedWinners()
    {
        string directory = Path.Combine(Path.GetTempPath(), "NexusForeverRealmFirstUpgrade_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string original = Path.Combine(directory, "previous.sqlite"), copied = Path.Combine(directory, "copied.sqlite");
        Dictionary<string, string[]> before;
        try
        {
            using (var old = new CharacterContext(Sqlite($"Data Source={original};Pooling=False")))
            {
                await old.GetService<IMigrator>().MigrateAsync(PreviousSqlite);
                old.Character.Add(new CharacterModel { Id = 42, Name = "Legacy", TotalXp = 100, Level = 1,
                    Achievement = [new CharacterAchievementModel { Id = 42, AchievementId = 500, ProgressState = 1, DateCompleted = new(2026, 10, 1) }] });
                old.Guild.Add(new GuildModel { Id = 77, Name = "Legacy guild", LeaderId = 42,
                    Achievement = [new GuildAchievementModel { Id = 77, AchievementId = 501, ProgressState = 1, DateCompleted = new(2026, 10, 1) }] });
                await old.SaveChangesAsync(); before = Snapshot(old);
            }
            string originalHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(original)));
            File.Copy(original, copied);
            var config = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={copied};Pooling=False" };
            using (var upgraded = new CharacterContext(config))
            {
                await upgraded.Database.MigrateAsync(); await upgraded.Database.MigrateAsync();
                Assert.Empty(upgraded.RealmFirstAchievement);
                Assert.Empty(await upgraded.Database.GetPendingMigrationsAsync());
            }
            var database = new CharacterDatabase(); database.Initialise(config);
            var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
            table.SetProperty(nameof(IGameTableManager.Achievement), PlayerBindPointTests.Table(
                new AchievementEntry { Id = 500, AchievementTypeId = (uint)AchievementType.RealmFirst },
                new AchievementEntry { Id = 501, AchievementTypeId = (uint)AchievementType.RealmFirst, Flags = (uint)AchievementFlags.Guild }));
            new GlobalAchievementManager(databases, tables).Initialise(); new GlobalAchievementManager(databases, tables).Initialise();
            using (var read = new CharacterContext(config))
            {
                var claims = read.RealmFirstAchievement.AsNoTracking().ToArray(); Assert.Equal(2, claims.Length);
                Assert.All(claims, row => { Assert.Null(row.OwnerId); Assert.Null(row.Token); Assert.Null(row.Name); });
                Assert.Contains(claims, row => !row.IsGuild && row.AchievementId == 500);
                Assert.Contains(claims, row => row.IsGuild && row.AchievementId == 501);
                var after = Snapshot(read);
                foreach (var tableName in before.Keys) Assert.Equal(before[tableName], after[tableName]);
                Assert.Empty(read.CharacterEntitlement);
            }
            Assert.Equal(originalHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(original))));
        }
        finally
        {
            string full = Path.GetFullPath(directory);
            if (!full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(full).StartsWith("NexusForeverRealmFirstUpgrade_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected upgrade fixture path.");
            Directory.Delete(full, true);
        }
    }

    private static DbContextOptions<CharacterContext> Sqlite(string connection)
    {
        var builder = new DbContextOptionsBuilder<CharacterContext>();
        builder.UseConfiguration(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = connection });
        return builder.Options;
    }

    private static Dictionary<string, string[]> Snapshot(CharacterContext context)
    {
        DbConnection connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) connection.Open();
        var names = new List<string>();
        using (var query = connection.CreateCommand())
        {
            query.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%'";
            using var reader = query.ExecuteReader(); while (reader.Read()) names.Add(reader.GetString(0));
        }
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            using var query = connection.CreateCommand(); query.CommandText = "SELECT * FROM \"" + name.Replace("\"", "\"\"") + "\"";
            using var reader = query.ExecuteReader(); var rows = new List<string>();
            while (reader.Read())
            {
                var values = new object[reader.FieldCount]; reader.GetValues(values);
                rows.Add(JsonSerializer.Serialize(values.Select(value => value is DBNull ? null : value)));
            }
            result.Add(name, rows.Order(StringComparer.Ordinal).ToArray());
        }
        return result;
    }
}
