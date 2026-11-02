using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;

namespace NexusForever.Game.Tests.Database;

public sealed class MigratedSchemaPersistenceTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"NexusForeverSchemaPersistence_{Guid.NewGuid():N}.sqlite");
    private readonly IConnectionString provider;
    public MigratedSchemaPersistenceTests() { }
    internal MigratedSchemaPersistenceTests(IConnectionString provider) => this.provider = provider;
    private IConnectionString Configuration => provider ?? new DatabaseConnectionString
        { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={path};Pooling=False" };

    [Fact]
    public async Task CreddPublicHistoryWriterAllocatesIdsAfterAZeroPreservingUpgrade()
    {
        using var context = new AuthContext(Configuration);
        string previous = context.Database.GetMigrations().Single(id => id.EndsWith("_AccountCurrencyOperations", StringComparison.Ordinal));
        await context.Database.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync(previous);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO account_credd_history (id,accountId,createdUtc,operation,creditAmount) VALUES (0,77,'2026-10-30 01:02:03',1,40),(9,77,'2026-10-30 02:03:04',2,50)");
        var before = JsonSerializer.Serialize((await context.AccountCREDDHistory.AsNoTracking().ToArrayAsync()).OrderBy(row => row.Id));
        await context.Database.OpenConnectionAsync();
        string mode = await SqlMode(context);
        await context.Database.MigrateAsync(); await context.Database.MigrateAsync();
        Assert.Equal(mode, await SqlMode(context));
        Assert.Equal(before, JsonSerializer.Serialize((await context.AccountCREDDHistory.AsNoTracking().ToArrayAsync()).OrderBy(row => row.Id)));

        var database = new AuthDatabase(); database.Initialise(Configuration);
        database.AddCREDDHistory(new() { AccountId = 77, CreatedUtc = new(2026, 11, 2), Operation = 3, CreditAmount = 60 });
        database.AddCREDDHistory(new() { AccountId = 77, CreatedUtc = new(2026, 11, 3), Operation = 4, CreditAmount = 70 });
        var rows = database.GetCREDDHistory(77, 10);
        Assert.Equal(4, rows.Count); Assert.Equal(4, rows.Select(row => row.Id).Distinct().Count());
        Assert.Contains(rows, row => row.Id == 0 && row.CreditAmount == 40);
        Assert.Contains(rows, row => row.Id == 9 && row.CreditAmount == 50);
        Assert.All(rows.Where(row => row.Operation >= 3), row => Assert.True(row.Id > 9));
        Assert.Empty(context.AccountEntitlement); Assert.Empty(context.AccountInventory); Assert.Empty(context.AccountPendingItem);
    }

    [Fact]
    public async Task MigratedItemAndLeaderboardFieldsRetainTheirNullableModelContract()
    {
        using var context = new CharacterContext(Configuration);
        await context.Database.MigrateAsync();
        context.Character.Add(new() { Id = 42, Name = "Schema owner" });
        context.Item.Add(new() { Id = 700, OwnerId = 42, ItemId = 7001, Location = 1, StackCount = 3, MicrochipIds = "91,92", RuneSlots = "7:9001" });
        context.LeaderboardPveScore.Add(new() { Id = 1, CharacterId = 42, PlayerName = "Preserved PvE", RecordedUtc = new(2026, 11, 2) });
        context.LeaderboardPvpScore.Add(new() { Id = 1, CharacterId = 42, PlayerName = "Preserved PvP", RecordedUtc = new(2026, 11, 2) });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("UPDATE item SET microchipIds=NULL,runeSlots=NULL WHERE id=700; UPDATE leaderboard_pve_score SET playerName=NULL WHERE id=1; UPDATE leaderboard_pvp_score SET playerName=NULL WHERE id=1");
        context.ChangeTracker.Clear();
        var item = await context.Item.SingleAsync();
        Assert.Null(item.MicrochipIds); Assert.Null(item.RuneSlots); Assert.Equal(3u, item.StackCount);
        Assert.Null((await context.LeaderboardPveScore.SingleAsync()).PlayerName);
        Assert.Null((await context.LeaderboardPvpScore.SingleAsync()).PlayerName);
        item.Durability = .5f; await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var retained = await context.Item.SingleAsync();
        Assert.Null(retained.MicrochipIds); Assert.Null(retained.RuneSlots); Assert.Equal(.5f, retained.Durability);
        Assert.Empty(context.CharacterEntitlement);
    }

    private async Task<string> SqlMode(DbContext context)
    {
        if (Configuration.Provider != DatabaseProvider.MySql) return "SQLite";
        await using var command = context.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT @@SESSION.sql_mode";
        return (string)await command.ExecuteScalarAsync();
    }

    public void Dispose()
    {
        if (provider != null) return;
        foreach (string suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }
}
