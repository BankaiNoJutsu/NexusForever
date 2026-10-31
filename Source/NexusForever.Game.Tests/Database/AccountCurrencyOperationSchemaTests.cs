using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.AuthContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class AccountCurrencyOperationSchemaTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProvidersAddOnlyAnEmptyGloballyKeyedWalletJournal(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []);
        Assert.False(context.Database.HasPendingModelChanges()); var assembly = context.GetService<IMigrationsAssembly>();
        var definition = assembly.Migrations.Single(row => row.Key.EndsWith("_AccountCurrencyOperations"));
        var migration = assembly.CreateMigration(definition.Value, context.Database.ProviderName);
        Assert.Equal(2, migration.UpOperations.Count); var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        Assert.Equal("account_currency_operation", table.Name); Assert.Equal(new[] { "operationKey" }, table.PrimaryKey.Columns); Assert.Empty(table.ForeignKeys);
        Assert.All(table.Columns, column => { Assert.False(column.IsNullable); Assert.Null(column.DefaultValue); });
        var index = Assert.Single(migration.UpOperations.OfType<CreateIndexOperation>()); Assert.Equal(new[] { "accountId", "currencyId" }, index.Columns);
        var key = context.Model.FindEntityType(typeof(AccountCurrencyOperationModel)).FindPrimaryKey().Properties.Single();
        Assert.Equal(ValueGenerated.Never, key.ValueGenerated); Assert.Equal(128, key.GetMaxLength());
        Assert.DoesNotContain(migration.UpOperations, operation => operation is SqlOperation or UpdateDataOperation or DeleteDataOperation or AddColumnOperation);
    }

    [Fact]
    public async Task PopulatedSqliteUpgradeRetainsWalletsAndEntitlementDefaultsWithoutCreatingOperationsOrGrants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AuthContext>().UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new AuthContext(options); await context.GetService<IMigrator>().MigrateAsync("20261030165054_AccountWorldSessionEpoch");
        context.Account.Add(new() { Id = 77, Email = "wallet-upgrade@example.local" });
        context.AccountCurrency.Add(new() { Id = 77, CurrencyId = 14, Amount = 1234 });
        context.AccountEntitlement.AddRange(new AccountEntitlementModel { Id = 77, EntitlementId = 15, Amount = 1 }, new AccountEntitlementModel { Id = 77, EntitlementId = 17, Amount = 1 });
        await context.SaveChangesAsync(); await context.Database.MigrateAsync(); await context.Database.MigrateAsync(); context.ChangeTracker.Clear();
        Assert.Equal(1234ul, context.AccountCurrency.Single().Amount); Assert.Empty(context.AccountCurrencyOperation);
        Assert.Equal(new byte[] { 15, 17 }, context.AccountEntitlement.OrderBy(row => row.EntitlementId).Select(row => row.EntitlementId));
        Assert.All(context.AccountEntitlement, row => Assert.Equal(1u, row.Amount)); Assert.Empty(context.AccountInventory); Assert.Empty(context.AccountPendingItem);
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
    }
}
