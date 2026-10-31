using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Character;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class ContractClaimDeliverySchemaTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProvidersAddOnlyANullCreditWitnessAndIndependentAccountOutbox(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []); Assert.False(context.Database.HasPendingModelChanges());
        var assembly = context.GetService<IMigrationsAssembly>(); var definition = assembly.Migrations.Single(row => row.Key.EndsWith("_ContractClaimDelivery"));
        var migration = assembly.CreateMigration(definition.Value, context.Database.ProviderName); Assert.Equal(3, migration.UpOperations.Count);
        var column = Assert.Single(migration.UpOperations.OfType<AddColumnOperation>()); Assert.Equal("character_contract_reward_claim", column.Table);
        Assert.Equal("characterCurrencyJson", column.Name); Assert.True(column.IsNullable); Assert.Null(column.DefaultValue);
        var outbox = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>()); Assert.Equal("character_contract_account_outbox", outbox.Name);
        Assert.Empty(outbox.ForeignKeys); Assert.Equal(new[] { "claimId" }, outbox.PrimaryKey.Columns);
        Assert.Single(migration.UpOperations.OfType<CreateIndexOperation>()); Assert.DoesNotContain(migration.UpOperations, operation => operation is SqlOperation or UpdateDataOperation or DeleteDataOperation);
    }

    [Fact]
    public async Task PopulatedSqliteUpgradeRetainsPriorItemReceiptsWithoutInventingCreditsOrAccountIntents()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new CharacterContext(options); await context.GetService<IMigrator>().MigrateAsync("20261031043709_ContractRewardClaims");
        context.Character.Add(new() { Id = 42, AccountId = 27, Name = "Prior Contract receipt" }); await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character_contract_reward_claim (id,trackId,cycleId,ordinal,rewardRowId,choice,claimId,accountId,quoteVersion,quoteJson,deliveryVersion,deliveryJson,createdUtc) VALUES (42,5,7,0,17,0,'123456781234123412341234567890ab',27,1,'retained quote',1,'retained Item plan','2026-10-31 01:02:03')");
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync(); context.ChangeTracker.Clear();
        var receipt = context.CharacterContractRewardClaim.Single(); Assert.Null(receipt.CharacterCurrencyJson); Assert.Equal("retained quote", receipt.QuoteJson);
        Assert.Equal("retained Item plan", receipt.DeliveryJson); Assert.Empty(context.CharacterContractAccountOutbox); Assert.Empty(context.CharacterCurrency); Assert.Empty(context.Item);
    }
}
