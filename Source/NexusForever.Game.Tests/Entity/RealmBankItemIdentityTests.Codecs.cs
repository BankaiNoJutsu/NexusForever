using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task DepositedCodecsSurviveBankRelogAndWithdrawalIntoAnotherCharacter()
    {
        var original = CharacterRow(); await Seed(original);
        var (manager, player, items) = Access();
        var item = new GameItem(original, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        await manager.SaveItemAsync(item, player);
        var (fresh, next, _) = Access();
        ((RecordingDispatchProxy<IPlayer>)(object)next).SetProperty(nameof(IPlayer.CharacterId), 101ul);
        ((RecordingDispatchProxy<IPlayer>)(object)next).SetProperty(nameof(IPlayer.Identity), new Identity { Id = 101, RealmId = 2 });
        using (var seed = new CharacterContext(Configuration))
        {
            seed.Character.Add(new() { Id = 101, AccountId = 12, Name = "BankReceiver", OriginalName = "BankReceiver", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
            await seed.SaveChangesAsync();
        }
        var loaded = Load(fresh, next);
        Assert.Equal(original.MicrochipIds, ItemMicrochipIdsCodec.Serialize(loaded.MicrochipIds));
        Assert.Equal(original.RuneSlots, ItemRuneSlotsCodec.Serialize(loaded.RuneSlots));
        loaded.Location = InventoryLocation.Inventory; loaded.BagIndex = 4;
        await fresh.SaveCharacterItemAsync(loaded, next);
        using var read = new CharacterContext(Configuration); var row = Assert.Single(read.Item);
        Assert.Equal(101ul, row.OwnerId); Assert.Equal(original.MicrochipIds, row.MicrochipIds); Assert.Equal(original.RuneSlots, row.RuneSlots);
        Assert.Empty(read.RealmBankItem);
    }

    [Fact]
    public async Task SingletonBankLoaderLoadsEachInventoryInsteadOfRememberingOnlyTheAccountAndRealm()
    {
        await Seed(bank: BankRow(401)); var (manager, player, _) = Access();
        var first = Load(manager, player); var second = Load(manager, player);
        Assert.NotSame(first, second); Assert.Equal(first.Guid, second.Guid);
    }

    [Fact]
    public async Task FailedBankReadCanRetryOnTheSameInventoryAfterTheDatabaseRecovers()
    {
        await Seed(bank: BankRow(401)); var (manager, player, _) = Access();
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var proxy);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), inventory);
        string ddl;
        using (var context = new CharacterContext(Configuration))
        {
            var connection = context.Database.GetDbConnection(); await connection.OpenAsync();
            using var command = connection.CreateCommand(); command.CommandText = "SELECT sql FROM sqlite_master WHERE name='realm_bank_item'"; ddl = (string)await command.ExecuteScalarAsync();
            await context.Database.ExecuteSqlRawAsync("DROP TABLE realm_bank_item");
        }
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => manager.EnsureLoaded(player));
        using (var restore = new CharacterContext(Configuration)) { await restore.Database.ExecuteSqlRawAsync(ddl); restore.RealmBankItem.Add(BankRow(401)); await restore.SaveChangesAsync(); }
        manager.EnsureLoaded(player); Assert.Single(proxy.GetInvocations(nameof(IInventory.LoadItem)));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("0,91,91,4294967295", "10:9001,7:0,10:4294967295")]
    public async Task BankRelogPreservesEmptyAndOrderedCodecPayloadsExactly(string chips, string runes)
    {
        var original = CharacterRow(); original.MicrochipIds = chips; original.RuneSlots = runes; await Seed(original);
        var (manager, player, items) = Access(); var item = new GameItem(original, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        await manager.SaveItemAsync(item, player);
        using (var read = new CharacterContext(Configuration))
        { var bank = Assert.Single(read.RealmBankItem); Assert.Equal(chips, bank.MicrochipIds); Assert.Equal(runes, bank.RuneSlots); }
        var (fresh, next, _) = Access(); var loaded = Load(fresh, next);
        Assert.Equal(chips, ItemMicrochipIdsCodec.Serialize(loaded.MicrochipIds)); Assert.Equal(runes, ItemRuneSlotsCodec.Serialize(loaded.RuneSlots));
    }

    [Theory]
    [InlineData(false, "microchipIds")] [InlineData(false, "runeSlots")]
    [InlineData(true, "microchipIds")] [InlineData(true, "runeSlots")]
    public async Task RewrittenTransferCodecsRejectTheWholeUnitAndFreshRetryPreservesBothDomains(bool withdrawal, string column)
    {
        var original = CharacterRow(); var bank = BankRow(401); bank.MicrochipIds = original.MicrochipIds; bank.RuneSlots = original.RuneSlots;
        await Seed(withdrawal ? null : original, withdrawal ? bank : null);
        var (manager, player, items) = Access(); var item = withdrawal ? Load(manager, player) : new GameItem(original, items);
        item.Location = withdrawal ? InventoryLocation.Inventory : InventoryLocation.RealmBank; item.BagIndex = 4;
        using (var context = new CharacterContext(Configuration))
        {
            string table = withdrawal ? "item" : "realm_bank_item";
            string change = column == "microchipIds" ? "98" : "7:98";
            string sql = context.Database.IsSqlite()
                ? $"CREATE TRIGGER rewrite_codec AFTER INSERT ON {table} BEGIN UPDATE {table} SET {column}='{change}' WHERE id=NEW.id; END;"
                : $"CREATE TRIGGER rewrite_codec BEFORE INSERT ON `{table}` FOR EACH ROW SET NEW.`{column}`='{change}'";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => withdrawal ? manager.SaveCharacterItemAsync(item, player) : manager.SaveItemAsync(item, player));
        Assert.False(item.HasPendingSave);
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Equal(withdrawal ? 0 : 1, read.Item.Count()); Assert.Equal(withdrawal ? 1 : 0, read.RealmBankItem.Count());
            if (withdrawal) { var retained = read.RealmBankItem.Single(); Assert.Equal(original.MicrochipIds, retained.MicrochipIds); Assert.Equal(original.RuneSlots, retained.RuneSlots); }
            else { var retained = read.Item.Single(); Assert.Equal(original.MicrochipIds, retained.MicrochipIds); Assert.Equal(original.RuneSlots, retained.RuneSlots); }
            await read.Database.ExecuteSqlRawAsync("DROP TRIGGER rewrite_codec");
        }
        await (withdrawal ? manager.SaveCharacterItemAsync(item, player) : manager.SaveItemAsync(item, player));
        using var final = new CharacterContext(Configuration);
        Assert.Equal(withdrawal ? 1 : 0, final.Item.Count()); Assert.Equal(withdrawal ? 0 : 1, final.RealmBankItem.Count());
        Assert.Equal(original.MicrochipIds, withdrawal ? final.Item.Single().MicrochipIds : final.RealmBankItem.Single().MicrochipIds);
        Assert.Equal(original.RuneSlots, withdrawal ? final.Item.Single().RuneSlots : final.RealmBankItem.Single().RuneSlots);
    }

    [Fact]
    public async Task PartialInventoryPublicationIsQuarantinedAndAFreshInventoryCanLoadBothRows()
    {
        await Seed(bank: BankRow(401)); using (var seed = new CharacterContext(Configuration)) { var second = BankRow(402); second.BagIndex = 4; seed.RealmBankItem.Add(second); await seed.SaveChangesAsync(); }
        var (manager, player, _) = Access(); var inventory = RecordingDispatchProxy<IInventory>.Create(out var proxy);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), inventory);
        int calls = 0; proxy.SetMethodHandler(nameof(IInventory.LoadItem), _ => { if (++calls == 2) throw new IOException("Owned partial bank publication."); return null; });
        Assert.Throws<IOException>(() => manager.EnsureLoaded(player)); Assert.Throws<InvalidOperationException>(() => manager.EnsureLoaded(player)); Assert.Equal(2, calls);
        var fresh = RecordingDispatchProxy<IInventory>.Create(out var freshProxy); ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), fresh);
        manager.EnsureLoaded(player); manager.EnsureLoaded(player); Assert.Equal(2, freshProxy.GetInvocations(nameof(IInventory.LoadItem)).Count);
    }

    [Theory]
    [InlineData("account")] [InlineData("realm")] [InlineData("character")]
    public async Task LoadedInventoryCannotBeReusedUnderAnotherBankOwner(string owner)
    {
        await Seed(bank: BankRow(401)); var (manager, player, _) = Access(); _ = Load(manager, player);
        if (owner == "account") ((RecordingDispatchProxy<IAccount>)(object)player.Account).SetProperty(nameof(IAccount.Id), 13u);
        if (owner == "realm") ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Identity), new Identity { Id = 100, RealmId = 3 });
        if (owner == "character") ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.CharacterId), 101ul);
        Assert.Throws<InvalidOperationException>(() => manager.EnsureLoaded(player));
    }

    [Fact]
    public async Task AStaleUnchangedCharacterObjectCannotDeleteAnAlreadyDepositedBankRow()
    {
        var original = CharacterRow(); await Seed(original); var (manager, player, items) = Access();
        var current = new GameItem(original, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        var stale = new GameItem(original, items) { Location = InventoryLocation.Inventory };
        using (var clean = new CharacterContext(Configuration)) { stale.Save(clean); await clean.SaveChangesAsync(); }
        await manager.SaveItemAsync(current, player);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveCharacterItemAsync(stale, player));
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item);
        var bank = Assert.Single(read.RealmBankItem); Assert.Equal(original.MicrochipIds, bank.MicrochipIds); Assert.Equal(original.RuneSlots, bank.RuneSlots);
    }

    [Theory]
    [InlineData("microchipIds")] [InlineData("runeSlots")]
    public async Task RewrittenSingleRowBankUpdateRollsBackBeforeAcknowledgingItsCodec(string column)
    {
        var original = CharacterRow(); var bank = BankRow(401); bank.MicrochipIds = original.MicrochipIds; bank.RuneSlots = original.RuneSlots;
        await Seed(bank: bank); var (manager, player, _) = Access(); var item = Load(manager, player);
        if (column == "microchipIds") item.MicrochipIds.Add(99);
        else { item.RuneSlots.Clear(); item.RuneSlots.Add(new ItemRuneSlot((NexusForever.Game.Static.Crafting.RuneType)7, 9002)); item.TouchRuneSlots(); }
        using (var context = new CharacterContext(Configuration))
        {
            string sql = context.Database.IsSqlite()
                ? $"CREATE TRIGGER rewrite_codec AFTER UPDATE ON realm_bank_item BEGIN UPDATE realm_bank_item SET {column}='98' WHERE id=NEW.id; END;"
                : $"CREATE TRIGGER rewrite_codec BEFORE UPDATE ON realm_bank_item FOR EACH ROW SET NEW.`{column}`='98'";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveItemAsync(item, player));
        using (var read = new CharacterContext(Configuration))
        {
            var row = read.RealmBankItem.Single(); Assert.Equal(original.MicrochipIds, row.MicrochipIds); Assert.Equal(original.RuneSlots, row.RuneSlots);
            Assert.Empty(read.Item); await read.Database.ExecuteSqlRawAsync("DROP TRIGGER rewrite_codec");
        }
        await manager.SaveItemAsync(item, player);
        using var final = new CharacterContext(Configuration); var saved = final.RealmBankItem.Single();
        Assert.Equal(column == "microchipIds" ? "91,92,99" : original.MicrochipIds, saved.MicrochipIds);
        Assert.Equal(column == "runeSlots" ? "7:9002" : original.RuneSlots, saved.RuneSlots);
    }

    [Fact]
    public async Task PopulatedBankCodecMigrationPreservesOldColumnsAndUsesEmptyLegacyCodecsWithoutGrants()
    {
        using var context = new CharacterContext(Configuration);
        string previous = context.Database.GetMigrations().Where(value => !value.EndsWith("_RealmBankItemCodecs", StringComparison.Ordinal)).Last();
        await context.GetService<IMigrator>().MigrateAsync(previous);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO realm_bank_item (id,accountId,realmId,itemId,bagIndex,stackCount,charges,durability,expirationTimeLeft,soulbound) VALUES (401,12,2,7001,3,17,5,0.75,1234,1)");
        async Task<string> Snapshot()
        {
            var connection = context.Database.GetDbConnection(); if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
            using var command = connection.CreateCommand(); command.CommandText = "SELECT id,accountId,realmId,itemId,bagIndex,stackCount,charges,durability,expirationTimeLeft,soulbound FROM realm_bank_item ORDER BY id";
            using var reader = await command.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync());
            return System.Text.Json.JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(index => Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture)).ToArray());
        }
        string before = await Snapshot(); await context.Database.MigrateAsync(); await context.Database.MigrateAsync();
        Assert.Equal(before, await Snapshot()); var bank = context.RealmBankItem.Single();
        Assert.Equal(string.Empty, bank.MicrochipIds); Assert.Equal(string.Empty, bank.RuneSlots);
        Assert.Empty(context.Item); Assert.Empty(context.Character); Assert.False(context.Database.HasPendingModelChanges());
    }
}
