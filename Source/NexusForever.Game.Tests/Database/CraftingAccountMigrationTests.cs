using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using AuthFactory = NexusForever.Database.Sqlite.Migrations.Design.AuthContextFactory;
using CharacterFactory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class CraftingAccountMigrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProviderSnapshotsContainOnlyTheIntendedCraftingSchema(bool sqlite)
    {
        using var auth = new AuthFactory().CreateDbContext(sqlite ? [] : ["--mysql"]);
        using var character = new CharacterFactory().CreateDbContext(sqlite ? [] : ["--mysql"]);
        Assert.False(auth.Database.HasPendingModelChanges()); Assert.False(character.Database.HasPendingModelChanges());
        foreach (DbContext context in new DbContext[] { auth, character })
        {
            var assembly = context.GetService<IMigrationsAssembly>();
            var migration = assembly.CreateMigration(Assert.Single(assembly.Migrations, row => row.Key.EndsWith("_CraftingAccountAwards", StringComparison.Ordinal)).Value,
                context.Database.ProviderName!);
            var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
            Assert.Empty(table.ForeignKeys);
            Assert.Equal(context == auth ? "account_crafting_receipt" : "character_crafting_account_outbox", table.Name);
            Assert.Single(migration.UpOperations.OfType<CreateIndexOperation>());
            if (context == auth)
            {
                var column = Assert.Single(migration.UpOperations.OfType<AddColumnOperation>());
                Assert.Equal("nextInventoryId", column.Name); Assert.Equal("account", column.Table);
                Assert.Equal(1ul, column.DefaultValue); Assert.False(column.IsNullable);
                Assert.Equal(3, migration.UpOperations.Count);
            }
            else Assert.Equal(2, migration.UpOperations.Count);
        }
    }

    [Fact]
    public async Task AuthUpgradeBootstrapsPastLiveAndConsumedItemsWithoutChangingEntitlements()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var context = new AuthContext(new DbContextOptionsBuilder<AuthContext>().UseSqlite(connection,
            options => options.MigrationsAssembly(typeof(AuthFactory).Assembly.GetName().Name)).Options);
        await BeforeLast(context);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO account (id,email) VALUES (12,'upgrade12@example.local')");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO account_inventory (id,inventoryId,accountItemId,creationKey) VALUES (12,50,1101,'legacy-50')");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO account_inventory_receipt (accountId,deliveryKey,inventoryId) VALUES (12,'consumed-80',80)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO account_entitlement (id,entitlementId,amount) VALUES (12,15,1),(12,17,1)");
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync();
        Assert.Equal(1ul, (await context.Account.SingleAsync()).NextInventoryId);
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            Assert.Equal(81ul, context.ReserveInventoryIds(12, 1));
            await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
        context.ChangeTracker.Clear();
        Assert.Equal(82ul, (await context.Account.SingleAsync()).NextInventoryId);
        Assert.Equal(50ul, (await context.AccountInventory.SingleAsync()).InventoryId);
        Assert.Equal(80ul, (await context.AccountInventoryReceipt.SingleAsync()).InventoryId);
        var entitlements = await context.AccountEntitlement.OrderBy(row => row.EntitlementId).ToArrayAsync();
        Assert.Equal(new byte[] { 15, 17 }, entitlements.Select(row => row.EntitlementId));
        Assert.All(entitlements, row => Assert.Equal(1u, row.Amount));
        Assert.Empty(context.AccountCraftingReceipt);
    }

    [Fact]
    public async Task CharacterUpgradeKeepsProgressAndItsCommittedOutboxSurvivesCharacterDeletion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var context = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection,
            options => options.MigrationsAssembly(typeof(CharacterFactory).Assembly.GetName().Name)).Options);
        await BeforeLast(context);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character (id,accountId,name,isOnline,totalXp) VALUES (42,12,'CraftSource',0,123)");
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync();
        Assert.Equal(123u, (await context.Character.SingleAsync()).TotalXp);
        Assert.Empty(context.CharacterEntitlement);
        context.CharacterCraftingAccountOutbox.Add(new() { Id = new string('a', 32), CharacterId = 42, AccountId = 12, RealmId = 1, PayloadJson = "{}" });
        await context.SaveChangesAsync(); await context.Character.ExecuteDeleteAsync();
        Assert.Equal(12u, (await context.CharacterCraftingAccountOutbox.SingleAsync()).AccountId);
    }

    private static Task BeforeLast(DbContext context)
    {
        var migrations = context.GetService<IMigrationsAssembly>().Migrations.Keys.ToArray();
        // Locate the crafting migration by name: later migrations may follow it.
        int index = Array.FindIndex(migrations, migration => migration.EndsWith("_CraftingAccountAwards", StringComparison.Ordinal));
        Assert.True(index > 0, "The CraftingAccountAwards migration must exist and have a predecessor.");
        return context.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
    }
}
