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

public sealed class ContractRewardClaimSchemaTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AClaimIdCannotBeReusedAndASlotCannotReceiveTwoDifferentClaims(bool sameClaimId)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options;
        await using var context = new CharacterContext(options); await context.Database.EnsureCreatedAsync();
        context.Character.AddRange(new CharacterModel { Id = 42, Name = "First owner" }, new CharacterModel { Id = 43, Name = "Second owner" });
        string identity = Guid.NewGuid().ToString("N");
        CharacterContractRewardClaimModel Claim(ulong owner, string id) => new() { Id = owner, TrackId = 5, RewardRowId = 17, AccountId = 27,
            ClaimId = id, QuoteVersion = 1, QuoteJson = "schema quote", DeliveryVersion = 1, DeliveryJson = "schema delivery", CreatedUtc = DateTime.UtcNow };
        context.CharacterContractRewardClaim.Add(Claim(42, identity)); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        context.CharacterContractRewardClaim.Add(Claim(sameClaimId ? 43ul : 42ul, sameClaimId ? identity : Guid.NewGuid().ToString("N")));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        await using var read = new CharacterContext(options); Assert.Equal(identity, Assert.Single(read.CharacterContractRewardClaim).ClaimId);
        Assert.Equal(2, read.Character.Count());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BothProvidersAddOnlyAnUnclaimedDefaultAndCharacterOwnedClaimHistory(bool mysql)
    {
        using var context = new Factory().CreateDbContext(mysql ? ["--mysql"] : []);
        Assert.False(context.Database.HasPendingModelChanges()); var assembly = context.GetService<IMigrationsAssembly>();
        var definition = assembly.Migrations.Single(row => row.Key.EndsWith("_ContractRewardClaims"));
        var migration = assembly.CreateMigration(definition.Value, context.Database.ProviderName);
        Assert.Equal(3, migration.UpOperations.Count); var bit = Assert.Single(migration.UpOperations.OfType<AddColumnOperation>());
        Assert.Equal("character_contract_reward_slot", bit.Table); Assert.Equal("claimed", bit.Name); Assert.Equal(false, bit.DefaultValue);
        var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        Assert.Equal("character_contract_reward_claim", table.Name); Assert.Equal(new[] { "id", "trackId", "cycleId", "ordinal" }, table.PrimaryKey.Columns);
        Assert.All(table.Columns, column => { Assert.False(column.IsNullable); Assert.Null(column.DefaultValue); });
        var foreign = Assert.Single(table.ForeignKeys); Assert.Equal("character", foreign.PrincipalTable); Assert.Equal(ReferentialAction.Cascade, foreign.OnDelete);
        var index = Assert.Single(migration.UpOperations.OfType<CreateIndexOperation>()); Assert.True(index.IsUnique); Assert.Equal(new[] { "claimId" }, index.Columns);
        Assert.All(context.Model.FindEntityType(typeof(CharacterContractRewardClaimModel)).FindPrimaryKey().Properties,
            property => Assert.Equal(ValueGenerated.Never, property.ValueGenerated));
        Assert.DoesNotContain(migration.UpOperations, operation => operation is SqlOperation or UpdateDataOperation or DeleteDataOperation);
    }

    [Fact]
    public async Task PopulatedSqliteUpgradePreservesExistingSelectionsPointsAndHistoryWithoutInventingClaims()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
        await using var context = new CharacterContext(options); await context.GetService<IMigrator>().MigrateAsync("20261031030210_ContractRewardSelections");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character (id,accountId,name,isOnline) VALUES (42,27,'Contract claim upgrade',0)");
        context.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = 5, CycleId = 7, Points = 1234, Revision = 9, Active = true, SelectionInitialized = true });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("INSERT INTO character_contract_reward_slot (id,trackId,cycleId,ordinal,rewardRowId,requiredPoints,selectionJson) VALUES (42,5,7,0,17,1000,'retained selection')");
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync(); context.ChangeTracker.Clear();
        Assert.Empty(context.CharacterContractRewardClaim); Assert.Empty(context.Item); Assert.Empty(context.CharacterCurrency);
        var slot = Assert.Single(context.CharacterContractRewardSlot); Assert.False(slot.Claimed); Assert.Equal("retained selection", slot.SelectionJson);
        var track = Assert.Single(context.CharacterContractRewardTrack); Assert.Equal(1234u, track.Points); Assert.Equal(9ul, track.Revision); Assert.Equal(7ul, track.CycleId);
        Assert.Equal(27u, context.Character.Single().AccountId); Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task ClaimHistoryRetainsZeroKeysAndSurvivesTrackDeletionButCascadesOnlyWithItsCharacter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options;
        await using var context = new CharacterContext(options); await context.Database.EnsureCreatedAsync();
        context.Character.AddRange(new CharacterModel { Id = 42, Name = "Claim owner" }, new CharacterModel { Id = 43, Name = "Retained owner" });
        context.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = 5, Revision = 1 });
        context.CharacterContractRewardClaim.Add(new() { Id = 42, TrackId = 5, CycleId = 0, Ordinal = 0, AccountId = 27, RewardRowId = 17,
            ClaimId = Guid.NewGuid().ToString("N"), QuoteVersion = 1, QuoteJson = "schema witness", DeliveryVersion = 1, DeliveryJson = "schema delivery", CreatedUtc = DateTime.UtcNow });
        await context.SaveChangesAsync(); context.CharacterContractRewardTrack.Remove(context.CharacterContractRewardTrack.Single()); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var claim = Assert.Single(context.CharacterContractRewardClaim); Assert.Equal(0ul, claim.CycleId); Assert.Equal((byte)0, claim.Ordinal);
        context.Character.Remove(context.Character.Single(row => row.Id == 42)); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        Assert.Empty(context.CharacterContractRewardClaim); Assert.Equal(43ul, context.Character.Single().Id);
    }
}
