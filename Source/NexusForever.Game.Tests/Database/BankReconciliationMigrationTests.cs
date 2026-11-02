using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class BankReconciliationMigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReceiptMigrationMatchesItsProviderModel(bool sqlite)
    {
        var options = sqlite ? SqliteOptions("Data Source=:memory:") : CharacterMigrationHistoryTests.CreateScriptOptions();
        using var context = new CharacterContext(options);
        Assert.False(context.Database.HasPendingModelChanges());
        string previous = sqlite ? "20261101100740_MarketplaceBidReceipts" : "20261102073206_RepairCostumeTimestampGeneration";
        string latest = sqlite ? "20261102182001_BankReconciliationReceipts" : "20261102182000_BankReconciliationReceipts";
        string sql = context.GetService<IMigrator>().GenerateScript(previous, latest);
        Assert.Contains("bank_reconciliation_receipt", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("realm_bank_item", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CopiedPreviousSqliteDatabaseRetainsBankRowsAndAddsDurableReceipts()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"NexusForeverBankMigration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string original = Path.Combine(directory, "previous.sqlite"), copied = Path.Combine(directory, "copied.sqlite");
        try
        {
            await using (var context = new CharacterContext(SqliteOptions($"Data Source={original};Pooling=False")))
            {
                await context.GetService<IMigrator>().MigrateAsync("20261101100740_MarketplaceBidReceipts");
                context.RealmBankItem.Add(new RealmBankItemModel { Id = 900, AccountId = 12, RealmId = 1, ItemId = 7001,
                    BagIndex = 3, StackCount = 17, Charges = 2, Durability = .75f, ExpirationTimeLeft = 1200,
                    MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" });
                await context.SaveChangesAsync();
            }
            File.Copy(original, copied);
            var options = SqliteOptions($"Data Source={copied};Pooling=False");
            await using (var context = new CharacterContext(options))
            {
                await context.Database.MigrateAsync();
                var row = Assert.Single(context.RealmBankItem);
                Assert.Equal(17u, row.StackCount); Assert.Equal(1200u, row.ExpirationTimeLeft);
                Assert.Equal("91,92", row.MicrochipIds); Assert.Equal("10:9001,7:0", row.RuneSlots);
                context.BankReconciliationReceipt.Add(new() { Id = "11111111111111111111111111111111", Payload = "retained outcome", CreatedUtc = new(2026, 11, 2, 18, 20, 0) });
                await context.SaveChangesAsync();
            }
            await using var read = new CharacterContext(options);
            Assert.Equal("retained outcome", Assert.Single(read.BankReconciliationReceipt).Payload);
            Assert.Empty(await read.Database.GetPendingMigrationsAsync());
        }
        finally { Directory.Delete(directory, true); }
    }

    private static DbContextOptions<CharacterContext> SqliteOptions(string connection)
    {
        var builder = new DbContextOptionsBuilder<CharacterContext>();
        builder.UseConfiguration(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = connection });
        return builder.Options;
    }
}
