using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Friendship;
using NexusForever.Game.Static.Chat;

namespace NexusForever.Game.Tests.Database;

public sealed class PresenceGenerationSchemaTests
{
    [Fact]
    public async Task AuthUpgradePreservesExistingAccountsDefaultsAndWalletWithoutEpochBackfill()
    {
        using var files = new Files();
        var options = new DbContextOptionsBuilder<AuthContext>().UseSqlite($"Data Source={files.File};Pooling=False",
            configure => configure.MigrationsAssembly(Extensions.SqliteMigrationsAssembly)).Options;
        await using (var before = new AuthContext(options))
        {
            await before.GetService<IMigrator>().MigrateAsync("20261029175905_AccountInventorySourceIdentity");
            // Seed the historical schema directly; the current AccountModel also
            // maps columns introduced by migrations that this fixture has not run.
            await before.Database.ExecuteSqlRawAsync("INSERT INTO account (id,email,s,v,sessionKey,gameToken) VALUES (7,'upgrade@example.test','preserve-s','preserve-v','preserve-key','preserve-token')");
            before.AccountEntitlement.AddRange(new AccountEntitlementModel { Id = 7, EntitlementId = 15, Amount = 1 },
                new AccountEntitlementModel { Id = 7, EntitlementId = 17, Amount = 1 });
            before.AccountCurrency.Add(new() { Id = 7, CurrencyId = 8, Amount = 0 }); await before.SaveChangesAsync();
        }
        await using (var after = new AuthContext(options))
        {
            await after.Database.MigrateAsync(); Assert.False(after.Database.HasPendingModelChanges());
            var row = await after.Account.SingleAsync(); Assert.Equal("preserve-s", row.S); Assert.Equal("preserve-v", row.V);
            Assert.Equal("preserve-key", row.SessionKey); Assert.Equal("preserve-token", row.GameToken); Assert.Equal(1ul, row.NextPendingItemId);
            Assert.Equal(1ul, row.NextInventoryId);
            Assert.Empty(await after.AccountWorldSession.ToArrayAsync()); Assert.Equal(0ul, row.WorldSessionEpoch);
            Assert.Equal(new byte[] { 15, 17 }, await after.AccountEntitlement.OrderBy(e => e.EntitlementId).Select(e => e.EntitlementId).ToArrayAsync());
            Assert.All(await after.AccountEntitlement.ToArrayAsync(), e => Assert.Equal(1u, e.Amount));
            Assert.Equal(0ul, (await after.AccountCurrency.SingleAsync()).Amount); Assert.Empty(await after.AccountPendingItem.ToArrayAsync());
        }
    }

    [Fact]
    public async Task FriendshipUpgradePreservesLegacyActivePresencePreferencesAndHistory()
    {
        using var files = new Files();
        var options = new DbContextOptionsBuilder<FriendshipContext>().UseSqlite($"Data Source={files.File};Pooling=False",
            configure => configure.MigrationsAssembly(Extensions.SqliteMigrationsAssembly)).Options;
        await using (var before = new FriendshipContext(options))
        {
            await before.GetService<IMigrator>().MigrateAsync("20260607222247_FriendshipSqliteInitial");
            await before.Database.ExecuteSqlRawAsync("INSERT INTO account (accountId,email,nickname,status,presence,blockAccountFriendRequests,invitePrivilegesSuspended,activeCharacterId,activeRealmId,lastOnline) VALUES (7,'upgrade@example.test','Preserved','status',2,1,1,NULL,NULL,NULL)");
            await before.Database.ExecuteSqlRawAsync("INSERT INTO character (characterId,realmId,realmName,name,accountId,race,class,path,faction,worldZoneId,worldId,lastOnline) VALUES (101,1,'Synthetic','PreservedCharacter',7,0,0,0,0,0,0,NULL)");
            await before.Database.ExecuteSqlRawAsync("UPDATE account SET activeCharacterId=101,activeRealmId=1 WHERE accountId=7");
        }
        await using (var after = new FriendshipContext(options))
        {
            await after.Database.MigrateAsync(); Assert.False(after.Database.HasPendingModelChanges());
            var row = await after.Account.SingleAsync(); Assert.Equal("Preserved", row.Nickname); Assert.Equal("status", row.Status);
            Assert.Equal((AccountPresenceState)2, row.Presence); Assert.True(row.BlockAccountFriendRequests); Assert.True(row.InvitePrivilegesSuspended);
            Assert.Equal(101ul, row.ActiveCharacterId); Assert.Equal((ushort)1, row.ActiveRealmId); Assert.Null(row.LastOnline);
            Assert.Equal(0ul, row.PresenceSessionEpoch); Assert.Equal(0ul, row.PresenceAdmission); Assert.True(row.PresenceClosed);
            Assert.Null(row.PresenceCharacterId); Assert.Null(row.PresenceRealmId); Assert.Null((await after.Character.SingleAsync()).LastOnline);
            Assert.Empty(await after.InternalMessage.ToArrayAsync());
        }
    }

    private sealed class Files : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-PresenceUpgrade-" + Guid.NewGuid().ToString("N"));
        internal string File => Path.Combine(directory, "upgrade.sqlite");
        internal Files() => Directory.CreateDirectory(directory);
        public void Dispose()
        {
            string full = Path.GetFullPath(directory); string parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("NexusForever-PresenceUpgrade-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected fixture path.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
    }
}
